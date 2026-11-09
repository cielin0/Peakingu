using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Peakingu
{
    /// <summary>
    /// Estalactita que cuelga del techo y cae al pasar el jugador.
    /// Tiembla como advertencia, cae aceleradamente con gravedad,
    /// causa daño con GameManager.Instance.TakeDamage(1) al golpear al jugador y se destruye en el impacto.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class FallingStalactite : MonoBehaviour
    {
        public enum StalactiteState
        {
            Hanging,
            Shaking,
            Falling,
            Destroyed
        }

        [Header("--- Detección del Jugador ---")]
        [Tooltip("Ancho horizontal del área de detección hacia abajo.")]
        [SerializeField] private float detectionWidth = 2.0f;
        [Tooltip("Distancia vertical máxima hacia abajo para detectar a Pingu.")]
        [SerializeField] private float detectionDistance = 8.5f;
        [Tooltip("Capa del jugador para el raycast/detección.")]
        [SerializeField] private LayerMask playerLayer;

        [Header("--- Temblor y Caída ---")]
        [Tooltip("Duración en segundos del temblor de advertencia antes de caer.")]
        [SerializeField] private float shakeDuration = 0.55f;
        [Tooltip("Intensidad del desplazamiento horizontal durante el temblor.")]
        [SerializeField] private float shakeMagnitude = 0.1f;
        [Tooltip("Velocidad de oscilación del temblor.")]
        [SerializeField] private float shakeFrequency = 50f;
        [Tooltip("Escala de gravedad una vez que se desprende del techo.")]
        [SerializeField] private float fallGravityScale = 4.0f;
        [Tooltip("Impulso inicial vertical hacia abajo al desprenderse.")]
        [SerializeField] private float initialFallSpeed = -2.5f;

        [Header("--- Daño e Impacto ---")]
        [Tooltip("Cantidad de vidas a restar al golpear al jugador.")]
        [SerializeField] private int damageAmount = 1;
        [Tooltip("Efecto de sonido opcional al temblar.")]
        [SerializeField] private AudioClip shakeSfx;
        [Tooltip("Efecto de sonido opcional al impactar o romperse.")]
        [SerializeField] private AudioClip impactSfx;

        [Header("--- Indicadores Visuales de Peligro ---")]
        [Tooltip("Objeto visual de alerta (ej: signo de exclamación o resplandor).")]
        [SerializeField] private GameObject warningIndicator;
        [Tooltip("Marcador de zona de peligro en el suelo directamente debajo.")]
        [SerializeField] private GameObject groundDangerMarker;
        [Tooltip("Luz 2D de la estalactita (cambia de color a rojo en alerta).")]
        [SerializeField] private Light2D stalactiteLight;

        private Rigidbody2D rb;
        private Collider2D col;
        private SpriteRenderer spriteRenderer;
        private StalactiteState currentState = StalactiteState.Hanging;
        private Vector3 initialPosition;
        private float shakeTimer = 0f;
        private bool hasHit = false;

        public StalactiteState CurrentState => currentState;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            initialPosition = transform.position;

            // Iniciar suspendida y estática en el techo
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Ocultar indicadores al inicio
            if (warningIndicator != null) warningIndicator.SetActive(false);
            if (groundDangerMarker != null) groundDangerMarker.SetActive(false);

            if (playerLayer == 0)
            {
                playerLayer = LayerMask.GetMask("Player", "Default");
            }
        }

        private void Update()
        {
            switch (currentState)
            {
                case StalactiteState.Hanging:
                    CheckPlayerUnderneath();
                    break;

                case StalactiteState.Shaking:
                    HandleShaking();
                    break;

                case StalactiteState.Falling:
                    // Medida de seguridad: Si cae al vacío por más de 4 segundos, autodestruir
                    break;
            }
        }

        /// <summary>
        /// Comprueba si Pingu entra en el cono vertical de detección debajo de la estalactita.
        /// </summary>
        private void CheckPlayerUnderneath()
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) return;

            Vector3 playerPos = playerObj.transform.position;
            float dx = Mathf.Abs(playerPos.x - transform.position.x);
            float dy = transform.position.y - playerPos.y;

            // Detectar si está alineado horizontalmente y debajo dentro de la distancia máxima
            if (dx <= detectionWidth * 0.5f && dy > 0.5f && dy <= detectionDistance)
            {
                StartShakeAndFall();
            }
        }

        /// <summary>
        /// Inicia la fase de advertencia: tiembla y muestra los indicadores de peligro.
        /// </summary>
        public void StartShakeAndFall()
        {
            if (currentState != StalactiteState.Hanging) return;

            currentState = StalactiteState.Shaking;
            shakeTimer = shakeDuration;
            initialPosition = transform.position;

            // Activar indicadores visuales de peligro
            if (warningIndicator != null)
            {
                warningIndicator.SetActive(true);
            }

            if (groundDangerMarker != null)
            {
                groundDangerMarker.SetActive(true);
            }

            // Cambiar luz 2D a tono de advertencia ámbar/rojo
            if (stalactiteLight != null)
            {
                stalactiteLight.color = new Color(1f, 0.35f, 0.15f, 1f);
                stalactiteLight.intensity = 1.4f;
            }

            if (shakeSfx != null)
            {
                AudioSource.PlayClipAtPoint(shakeSfx, transform.position);
            }

            Debug.Log($"[FallingStalactite] ¡Peligro! Estalactita en {transform.position} temblando.");
        }

        private void HandleShaking()
        {
            shakeTimer -= Time.deltaTime;

            // Vibración horizontal rápida
            float offsetX = Mathf.Sin(Time.time * shakeFrequency) * shakeMagnitude;
            transform.position = new Vector3(initialPosition.x + offsetX, initialPosition.y, initialPosition.z);

            // Parpadeo visual del indicador de peligro
            if (warningIndicator != null)
            {
                float pulse = Mathf.PingPong(Time.time * 8f, 1f);
                warningIndicator.transform.localScale = Vector3.one * (0.8f + pulse * 0.4f);
            }

            if (shakeTimer <= 0f)
            {
                ReleaseStalactite();
            }
        }

        /// <summary>
        /// Desprende la estalactita: activa la gravedad y comienza la caída libre acelerada.
        /// </summary>
        private void ReleaseStalactite()
        {
            currentState = StalactiteState.Falling;
            transform.position = initialPosition;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = fallGravityScale;
            rb.linearVelocity = new Vector2(0f, initialFallSpeed);
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (warningIndicator != null)
            {
                warningIndicator.SetActive(false);
            }

            // Destrucción tras 4 segundos por si cae en un pozo sin colisión
            Destroy(gameObject, 4.0f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleImpact(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Si el colisionador es trigger o colisiona con el jugador
            if (other.CompareTag("Player") || other.CompareTag("Hazard") || other.CompareTag("Ground"))
            {
                HandleImpact(other.gameObject);
            }
        }

        private void HandleImpact(GameObject target)
        {
            if (currentState != StalactiteState.Falling || hasHit) return;

            hasHit = true;
            currentState = StalactiteState.Destroyed;

            if (target.CompareTag("Player"))
            {
                // Hacer daño directo al jugador con TakeDamage
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeDamage(damageAmount);
                    Debug.Log($"[FallingStalactite] Estalactita impactó a Pingu. Vidas restantes: {GameManager.Instance.CurrentLives}");
                }
                else
                {
                    var pc = target.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        Debug.Log("[FallingStalactite] Impacto directo a PlayerController.");
                    }
                }
            }
            else
            {
                Debug.Log($"[FallingStalactite] Estalactita impactó contra {target.name} y se rompió.");
            }

            if (impactSfx != null)
            {
                AudioSource.PlayClipAtPoint(impactSfx, transform.position);
            }

            // Ocultar marcador del suelo al destruirse
            if (groundDangerMarker != null)
            {
                groundDangerMarker.SetActive(false);
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + Vector3.down * (detectionDistance * 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(detectionWidth, detectionDistance, 0.1f));
        }

#if UNITY_EDITOR
        public void Configure(
            float width,
            float distance,
            float duration,
            GameObject warnObj,
            GameObject groundMarker,
            Light2D light)
        {
            detectionWidth = width;
            detectionDistance = distance;
            shakeDuration = duration;
            warningIndicator = warnObj;
            groundDangerMarker = groundMarker;
            stalactiteLight = light;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
