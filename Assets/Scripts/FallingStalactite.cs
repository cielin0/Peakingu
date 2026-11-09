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
        [SerializeField] private float detectionWidth = 2.4f;
        [Tooltip("Distancia vertical máxima hacia abajo para detectar a Pingu.")]
        [SerializeField] private float detectionDistance = 9.0f;
        [Tooltip("Capa del jugador para el raycast/detección.")]
        [SerializeField] private LayerMask playerLayer;

        [Header("--- Temblor y Caída ---")]
        [Tooltip("Duración en segundos del temblor de advertencia antes de caer.")]
        [SerializeField] private float shakeDuration = 0.5f;
        [Tooltip("Intensidad del desplazamiento horizontal durante el temblor.")]
        [SerializeField] private float shakeMagnitude = 0.08f;
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
        private Transform playerTransform;
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
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Ocultar indicadores al inicio
            if (warningIndicator != null) warningIndicator.SetActive(false);
            if (groundDangerMarker != null) groundDangerMarker.SetActive(false);

            if (playerLayer == 0)
            {
                playerLayer = LayerMask.GetMask("Player");
                if (playerLayer == 0) playerLayer = LayerMask.GetMask("Default");
            }
        }

        private void Start()
        {
            CachePlayer();
        }

        private void CachePlayer()
        {
            if (playerTransform == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
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
                    break;
            }
        }

        /// <summary>
        /// Comprueba si Pingu entra en el área vertical de detección debajo de la estalactita.
        /// </summary>
        private void CheckPlayerUnderneath()
        {
            if (playerTransform == null)
            {
                CachePlayer();
                if (playerTransform == null) return;
            }

            Vector3 playerPos = playerTransform.position;
            float dx = Mathf.Abs(playerPos.x - transform.position.x);
            float dy = transform.position.y - playerPos.y;

            // Detectar si está alineado horizontalmente y dentro de la distancia hacia abajo
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

            // Activar indicador de advertencia sobre la estalactita
            if (warningIndicator != null)
            {
                warningIndicator.SetActive(true);
            }

            // Activar y fijar el marcador de peligro en el suelo (desvinculándolo del padre para que no tiemble ni caiga)
            if (groundDangerMarker != null)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, detectionDistance + 2f, LayerMask.GetMask("Ground"));
                float groundY = (hit.collider != null) ? (hit.point.y + 0.1f) : (transform.position.y - 7.5f);
                groundDangerMarker.transform.position = new Vector3(transform.position.x, groundY, 0f);
                groundDangerMarker.transform.SetParent(null, true);
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
        /// Desprende la estalactita: activa la física dinámica y comienza la caída acelerada.
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

            Debug.Log($"[FallingStalactite] Estalactita desprendida en {transform.position}, cayendo.");

            // Destrucción tras 4 segundos por si cae fuera de límites
            Destroy(gameObject, 4.0f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleImpact(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleImpact(other.gameObject);
        }

        private void HandleImpact(GameObject target)
        {
            if (currentState != StalactiteState.Falling || hasHit) return;

            // Ignorar choques consigo misma, con sus hijos o con otras estalactitas
            if (target == null || target == gameObject || target.transform.IsChildOf(transform) || target.CompareTag("Hazard"))
            {
                return;
            }

            // 1. Impacto con el jugador Pingu
            bool isPlayer = target.CompareTag("Player") || target.GetComponentInParent<PlayerController>() != null;
            if (isPlayer)
            {
                hasHit = true;
                currentState = StalactiteState.Destroyed;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeDamage(damageAmount);
                    Debug.Log($"[FallingStalactite] ¡Estalactita impactó a Pingu! Daño={damageAmount}. Vidas restantes: {GameManager.Instance.CurrentLives}");
                }
                else
                {
                    Debug.Log("[FallingStalactite] Impacto contra el jugador (GameManager no disponible).");
                }

                CleanupAndDestroy();
                return;
            }

            // 2. Impacto con el suelo o plataformas
            bool isGround = target.CompareTag("Ground") || target.layer == LayerMask.NameToLayer("Ground");
            if (isGround)
            {
                // REGLA CRUCIAL: El techo de la cueva también comparte el tilemap y la capa "Ground".
                // Si la estalactita aún no ha descendido al menos 1.2 unidades del techo, ignorar el contacto con el techo.
                if (transform.position.y > initialPosition.y - 1.2f)
                {
                    return;
                }

                hasHit = true;
                currentState = StalactiteState.Destroyed;
                Debug.Log($"[FallingStalactite] Estalactita impactó contra el suelo ({target.name}) en Y={transform.position.y:F2} y se destruyó.");

                CleanupAndDestroy();
                return;
            }

            // Cualquier otro trigger u objeto que no sea Player ni Ground se ignora
        }

        private void CleanupAndDestroy()
        {
            if (impactSfx != null)
            {
                AudioSource.PlayClipAtPoint(impactSfx, transform.position);
            }

            if (groundDangerMarker != null)
            {
                Destroy(groundDangerMarker);
                groundDangerMarker = null;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (groundDangerMarker != null)
            {
                Destroy(groundDangerMarker);
            }
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
