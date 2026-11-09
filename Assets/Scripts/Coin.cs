using UnityEngine;

namespace Peakingu
{
    [RequireComponent(typeof(Collider2D))]
    public class Coin : MonoBehaviour
    {
        [Header("Configuración de Moneda")]
        [Tooltip("Cantidad de monedas que otorga este coleccionable al GameManager.")]
        [SerializeField] private int value = 1;

        [Header("Efectos Opcionales")]
        [Tooltip("Sonido reproducido al recolectar la moneda.")]
        [SerializeField] private AudioClip collectSfx;
        [Tooltip("Prefab de partículas o efecto visual al recolectarse.")]
        [SerializeField] private GameObject collectEffectPrefab;

        [Header("Animación de Flotación")]
        [Tooltip("Habilita un movimiento oscilatorio suave arriba y abajo.")]
        [SerializeField] private bool enableFloatingAnimation = true;
        [Tooltip("Velocidad de la oscilación.")]
        [SerializeField] private float floatSpeed = 3f;
        [Tooltip("Amplitud de la oscilación vertical en unidades.")]
        [SerializeField] private float floatAmplitude = 0.15f;

        private Vector3 initialPosition;
        private bool isCollected = false;
        private Collider2D coinCollider;

        public int Value => value;

        private void Reset()
        {
            coinCollider = GetComponent<Collider2D>();
            if (coinCollider != null)
            {
                coinCollider.isTrigger = true;
            }

            gameObject.tag = "Coin";
            int coinLayer = LayerMask.NameToLayer("Coin");
            if (coinLayer != -1)
            {
                gameObject.layer = coinLayer;
            }
        }

        private void Awake()
        {
            coinCollider = GetComponent<Collider2D>();
            if (coinCollider != null)
            {
                coinCollider.isTrigger = true;
            }
            initialPosition = transform.position;
        }

        private void Update()
        {
            if (enableFloatingAnimation && !isCollected)
            {
                float newY = initialPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected) return;

            // Detección del jugador por Tag, Layer o componente
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                Collect();
            }
        }

        private void Collect()
        {
            isCollected = true;

            // Desactivar collider inmediatamente para evitar recolecciones redundantes
            if (coinCollider != null)
            {
                coinCollider.enabled = false;
            }

            // Notificar y sumar al GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(value);
            }
            else
            {
                Debug.LogWarning("[Coin] GameManager.Instance no encontrado al recolectar la moneda.");
            }

            // Efecto de sonido: AudioManager tiene prioridad; el collectSfx local es fallback
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoin();
            }
            else if (collectSfx != null)
            {
                AudioSource.PlayClipAtPoint(collectSfx, transform.position);
            }

            // Efecto visual si está asignado
            if (collectEffectPrefab != null)
            {
                Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            }

            // Destruir el objeto de la moneda
            Destroy(gameObject);
        }
    }
}
