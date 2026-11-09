using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Peakingu
{
    /// <summary>
    /// Sistema de Gancho Cyborg para Pingu.
    /// Disponible solo si PlayerStats.hasGrapplingHook == true.
    ///
    /// Flujo:
    ///   1. Presionar R → busca el GrapplePoint más cercano en rango.
    ///   2. Crea un DistanceJoint2D entre Pingu y el punto.
    ///   3. LineRenderer dibuja la cuerda.
    ///   4. Presionar R de nuevo o Salto → suelta el gancho conservando impulso.
    ///
    /// Ajuste de parámetros: todos los valores clave están en el Inspector con Tooltips.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(LineRenderer))]
    public class GrappleController : MonoBehaviour
    {
        // ─────────────────────────── Inspector ───────────────────────────

        [Header("── Configuración del Gancho ──")]
        [Tooltip("Radio máximo (en unidades) para encontrar un GrapplePoint.")]
        [SerializeField] private float grappleRange = 10f;

        [Tooltip("Distancia mínima entre Pingu y el punto de agarre para activar el joint.")]
        [SerializeField] private float minGrappleDistance = 0.8f;

        [Tooltip("Fracción de la distancia real que usa el DistanceJoint. < 1 acorta la cuerda.")]
        [Range(0.3f, 1.0f)]
        [SerializeField] private float ropeSlack = 0.95f;

        [Tooltip("¿El joint permite acercarse (distancia máxima solamente) o fuerza distancia fija?")]
        [SerializeField] private bool maxDistanceOnly = true;

        [Tooltip("Capa(s) donde buscar GrapplePoints.")]
        [SerializeField] private LayerMask grappleLayer;

        [Header("── Impulso al Soltar ──")]
        [Tooltip("Fracción de velocidad que conserva Pingu al soltar el gancho (1 = todo, 0 = nada).")]
        [Range(0f, 1f)]
        [SerializeField] private float releaseVelocityRetention = 1f;

        [Tooltip("Impulso adicional vertical al soltar con Salto (para catapultar hacia arriba).")]
        [SerializeField] private float jumpReleaseBoost = 2f;

        [Header("── Visuales de la Cuerda ──")]
        [Tooltip("Color del LineRenderer cuando el gancho está activo.")]
        [SerializeField] private Color ropeColor = new Color(0.6f, 0.85f, 1f, 1f);

        [Tooltip("Ancho de la cuerda.")]
        [SerializeField] private float ropeWidth = 0.06f;

        [Tooltip("Material del LineRenderer (opcional, puede quedar null para usar el por defecto).")]
        [SerializeField] private Material ropeMaterial;

        [Header("── Referencias (auto-detectadas si se dejan vacías) ──")]
        [Tooltip("El Rigidbody2D de Pingu. Se detecta automáticamente en Awake.")]
        [SerializeField] private Rigidbody2D playerRb;

        [Tooltip("El PlayerController de Pingu. Se detecta automáticamente en Awake.")]
        [SerializeField] private PlayerController playerController;

        // ─────────────────────────── Estado ───────────────────────────

        private LineRenderer lr;
        private DistanceJoint2D joint;
        private GrapplePoint activePoint;
        private bool isGrappling = false;

        // ─────────────────────────── Propiedades públicas ───────────────────────────

        /// <summary>¿Hay un gancho activo en este momento?</summary>
        public bool IsGrappling => isGrappling;

        /// <summary>Punto de agarre activo (null si no hay gancho).</summary>
        public GrapplePoint ActiveGrapplePoint => activePoint;

        // ─────────────────────────── Unity Lifecycle ───────────────────────────

        private void Awake()
        {
            if (playerRb == null) playerRb = GetComponent<Rigidbody2D>();
            if (playerController == null) playerController = GetComponent<PlayerController>();

            lr = GetComponent<LineRenderer>();
            ConfigureLineRenderer();

            // Iniciar con el LineRenderer oculto
            lr.enabled = false;

            if (grappleLayer == 0)
            {
                grappleLayer = LayerMask.GetMask("GrapplePoint");
                if (grappleLayer == 0) grappleLayer = ~0; // fallback: todo
            }
        }

        private void Update()
        {
            ReadGrappleInput();
            UpdateRopeVisual();
        }

        private void OnDisable()
        {
            // Limpiar al desactivar (ej: escena de menú)
            ReleaseGrapple(addBoost: false);
        }

        // ─────────────────────────── Input ───────────────────────────

        private void ReadGrappleInput()
        {
            // Solo disponible si el jugador tiene la mejora
            if (!HasGrapplingHookUpgrade()) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            bool grapplePressed = keyboard.rKey.wasPressedThisFrame;
            bool jumpPressed = keyboard.spaceKey.wasPressedThisFrame;

            // R sin gancho → intentar lanzar
            if (grapplePressed && !isGrappling)
            {
                TryGrapple();
                return;
            }

            // R con gancho → soltar sin impulso de salto
            if (grapplePressed && isGrappling)
            {
                ReleaseGrapple(addBoost: false);
                return;
            }

            // Salto con gancho activo → soltar con impulso adicional vertical
            if (jumpPressed && isGrappling)
            {
                ReleaseGrapple(addBoost: true);
            }
        }

        // ─────────────────────────── Lanzar / Soltar ───────────────────────────

        /// <summary>
        /// Busca el GrapplePoint válido más cercano en rango y activa el joint.
        /// </summary>
        private void TryGrapple()
        {
            GrapplePoint nearest = FindNearestGrapplePoint();
            if (nearest == null)
            {
                Debug.Log("[GrappleController] No hay GrapplePoint en rango.");
                return;
            }

            AttachGrapple(nearest);
        }

        /// <summary>
        /// Recorre todos los GrapplePoints dentro de <c>grappleRange</c> y retorna el más cercano.
        /// Usa Physics2D.OverlapCircleAll para eficiencia.
        /// </summary>
        private GrapplePoint FindNearestGrapplePoint()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, grappleRange, grappleLayer);
            if (hits == null || hits.Length == 0) return null;

            GrapplePoint best = null;
            float bestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var gp = hit.GetComponent<GrapplePoint>();
                if (gp == null || !gp.IsActive) continue;

                float d = Vector2.Distance(transform.position, gp.transform.position);
                if (d < bestDist && d >= minGrappleDistance)
                {
                    bestDist = d;
                    best = gp;
                }
            }

            return best;
        }

        /// <summary>
        /// Adjunta el DistanceJoint2D entre Pingu y el GrapplePoint dado.
        /// </summary>
        private void AttachGrapple(GrapplePoint point)
        {
            activePoint = point;
            isGrappling = true;

            // Crear el DistanceJoint2D en Pingu
            joint = gameObject.AddComponent<DistanceJoint2D>();
            joint.connectedBody = point.GetComponent<Rigidbody2D>();
            joint.autoConfigureDistance = false;
            joint.distance = Mathf.Max(
                Vector2.Distance(transform.position, point.transform.position) * ropeSlack,
                minGrappleDistance
            );
            joint.maxDistanceOnly = maxDistanceOnly;
            joint.enableCollision = true;

            // Notificar al GrapplePoint
            activePoint.OnGrappleAttach();

            // Activar LineRenderer
            lr.enabled = true;

            Debug.Log($"[GrappleController] Gancho lanzado a '{point.name}' en {point.transform.position}. Distancia={joint.distance:F2}");
        }

        /// <summary>
        /// Suelta el gancho. Si <paramref name="addBoost"/> es true, añade un impulso vertical
        /// extra (para cuando se suelta con Salto).
        /// </summary>
        /// <param name="addBoost">Si se activa, se añade jumpReleaseBoost a la velocidad vertical.</param>
        public void ReleaseGrapple(bool addBoost)
        {
            if (!isGrappling) return;

            isGrappling = false;

            // Conservar impulso antes de destruir el joint
            Vector2 retainedVelocity = playerRb.linearVelocity * releaseVelocityRetention;
            if (addBoost)
            {
                retainedVelocity += Vector2.up * jumpReleaseBoost;
            }

            // Destruir el joint
            if (joint != null)
            {
                Destroy(joint);
                joint = null;
            }

            // Notificar al GrapplePoint
            if (activePoint != null)
            {
                activePoint.OnGrappleRelease();
                activePoint = null;
            }

            // Ocultar cuerda
            lr.enabled = false;

            // Aplicar velocidad retenida
            playerRb.linearVelocity = retainedVelocity;

            Debug.Log($"[GrappleController] Gancho soltado. Velocidad retenida: {retainedVelocity}, boost={addBoost}");
        }

        // ─────────────────────────── Visual de la Cuerda ───────────────────────────

        private void UpdateRopeVisual()
        {
            if (!isGrappling || activePoint == null)
            {
                lr.enabled = false;
                return;
            }

            lr.enabled = true;
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, activePoint.transform.position);
        }

        // ─────────────────────────── Helpers ───────────────────────────

        /// <summary>
        /// Verifica si el jugador tiene la mejora del gancho desbloqueada.
        /// </summary>
        private bool HasGrapplingHookUpgrade()
        {
            if (PlayerStats.Instance != null)
            {
                return PlayerStats.Instance.hasGrapplingHook;
            }
            return false;
        }

        /// <summary>
        /// Configura el LineRenderer con los parámetros del Inspector.
        /// Usa sharedMaterial para evitar leaks de instancias de material.
        /// </summary>
        private void ConfigureLineRenderer()
        {
            lr.positionCount = 2;
            lr.startWidth = ropeWidth;
            lr.endWidth = ropeWidth;
            lr.startColor = ropeColor;
            lr.endColor = ropeColor;
            lr.useWorldSpace = true;

            if (ropeMaterial != null)
            {
                // Usar el material asignado en el Inspector (sharedMaterial para no crear instancia)
                lr.sharedMaterial = ropeMaterial;
            }
            else if (lr.sharedMaterial == null)
            {
                // Solo crear si no hay ninguno asignado en el prefab
                var mat = new Material(Shader.Find("Sprites/Default")) { color = ropeColor };
#if UNITY_EDITOR
                // En el editor guardamos el material como asset para no hacer leak
                string matPath = "Assets/Materials/GrappleRope.mat";
                if (System.IO.File.Exists(matPath))
                {
                    var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (existing != null)
                    {
                        lr.sharedMaterial = existing;
                        return;
                    }
                }
#endif
                lr.sharedMaterial = mat;
            }
        }

        // ─────────────────────────── Gizmos ───────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrappling ? Color.cyan : new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, grappleRange);

            if (isGrappling && activePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, activePoint.transform.position);
            }
        }
    }
}
