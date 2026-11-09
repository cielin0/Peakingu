using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Peakingu
{
    [RequireComponent(typeof(Collider2D))]
    public class MerchantNPC : MonoBehaviour
    {
        [Header("Configuración de Interacción")]
        [Tooltip("Texto o cartel flotante que muestra 'Presiona E' o '[E] Comprar Mejoras'.")]
        [SerializeField] private GameObject interactPrompt;
        [Tooltip("Texto del prompt (opcional si se desea cambiar dinámicamente).")]
        [SerializeField] private TextMeshPro promptTextMesh;

        [Header("Audio y Retroalimentación")]
        [Tooltip("Efecto de sonido o voz al interactuar con el mercader cyborg.")]
        [SerializeField] private AudioClip greetingSfx;

        private bool isPlayerInRange = false;
        private PlayerController nearbyPlayer;

        public bool IsPlayerInRange => isPlayerInRange;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            gameObject.tag = "Merchant";
            int merchantLayer = LayerMask.NameToLayer("Merchant");
            if (merchantLayer != -1)
            {
                gameObject.layer = merchantLayer;
            }
        }

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isPlayerInRange) return;

            // Detección de la tecla E con el nuevo Input System
            bool ePressed = false;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                ePressed = true;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame) // Botón Y / Triángulo
            {
                ePressed = true;
            }

            if (ePressed)
            {
                Interact();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                isPlayerInRange = true;
                nearbyPlayer = other.GetComponent<PlayerController>();

                if (interactPrompt != null)
                {
                    interactPrompt.SetActive(true);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                isPlayerInRange = false;
                nearbyPlayer = null;

                if (interactPrompt != null)
                {
                    interactPrompt.SetActive(false);
                }

                // Si el jugador se aleja, cerrar la tienda automáticamente
                if (ShopManager.Instance != null && ShopManager.Instance.IsOpen)
                {
                    ShopManager.Instance.CloseShop();
                }
            }
        }

        public void Interact()
        {
            if (ShopManager.Instance == null)
            {
                Debug.LogWarning("[MerchantNPC] No se encontró una instancia de ShopManager.");
                return;
            }

            if (ShopManager.Instance.IsOpen)
            {
                // Si la tienda ya está abierta, presionar E la cierra
                ShopManager.Instance.CloseShop();
            }
            else
            {
                // Abre la tienda y bloquea el movimiento del jugador
                if (greetingSfx != null)
                {
                    AudioSource.PlayClipAtPoint(greetingSfx, transform.position);
                }

                ShopManager.Instance.OpenShop(this);
            }
        }

        public void SetPromptActive(bool active)
        {
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(active);
            }
        }

#if UNITY_EDITOR
        public void SetupInEditor(GameObject prompt, TextMeshPro textMesh = null)
        {
            interactPrompt = prompt;
            promptTextMesh = textMesh;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
