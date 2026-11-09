using UnityEngine;

namespace Peakingu
{
    /// <summary>
    /// Componente que marca un punto de agarre para el Gancho Cyborg de Pingu.
    /// Coloca este componente (con su Rigidbody2D estático) en cada punto del nivel
    /// donde Pingu pueda engancharse.
    ///
    /// Visualmente se muestra como un aro/anillo pixel-art en la escena y en Play Mode
    /// hace una animación de pulso para indicar que está disponible.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class GrapplePoint : MonoBehaviour
    {
        // ─────────────────────────── Inspector ───────────────────────────

        [Header("── Estado ──")]
        [Tooltip("¿Está este punto activo y disponible para el gancho?")]
        [SerializeField] private bool isActive = true;

        [Header("── Visuales ──")]
        [Tooltip("Color del punto cuando está disponible.")]
        [SerializeField] private Color idleColor = new Color(0.4f, 0.9f, 1f, 0.9f);

        [Tooltip("Color del punto cuando Pingu está enganchado a él.")]
        [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("Velocidad de la animación de pulso.")]
        [SerializeField] private float pulseSpeed = 2.5f;

        [Tooltip("Amplitud del pulso de escala.")]
        [SerializeField] private float pulseAmplitude = 0.12f;

        // ─────────────────────────── Estado interno ───────────────────────────

        private SpriteRenderer sr;
        private Vector3 baseScale;
        private bool grappled = false;

        // ─────────────────────────── Propiedades públicas ───────────────────────────

        /// <summary>¿Está este punto activo y disponible para el gancho?</summary>
        public bool IsActive => isActive;

        // ─────────────────────────── Unity Lifecycle ───────────────────────────

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;

            // El Rigidbody2D debe ser estático: el punto no se mueve
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            // El CircleCollider2D solo necesita detectar overlaps (trigger)
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;

            ApplyColor(idleColor);
        }

        private void Update()
        {
            if (!isActive) return;
            AnimatePulse();
        }

        // ─────────────────────────── API Pública ───────────────────────────

        /// <summary>
        /// Llamado por GrappleController cuando Pingu se engancha aquí.
        /// </summary>
        public void OnGrappleAttach()
        {
            grappled = true;
            ApplyColor(activeColor);
        }

        /// <summary>
        /// Llamado por GrappleController cuando Pingu suelta el gancho.
        /// </summary>
        public void OnGrappleRelease()
        {
            grappled = false;
            ApplyColor(idleColor);
        }

        /// <summary>
        /// Activa o desactiva este punto de agarre en tiempo de ejecución.
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            if (sr != null)
            {
                sr.color = active ? idleColor : new Color(idleColor.r, idleColor.g, idleColor.b, 0.25f);
            }
        }

        // ─────────────────────────── Helpers ───────────────────────────

        private void AnimatePulse()
        {
            if (grappled) return; // No pulsa mientras está enganchado
            float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            transform.localScale = baseScale * scale;
        }

        private void ApplyColor(Color color)
        {
            if (sr != null) sr.color = color;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isActive ? new Color(0f, 1f, 1f, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
        }
    }
}
