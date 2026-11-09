using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Peakingu
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("Precios de Mejoras (Informe de Diseño)")]
        public const int PriceGrapplingHook = 30;
        public const int PriceJetBoots = 20;
        public const int PriceAntiSlipGloves = 15;
        public const int PriceHydrodash = 25;

        [Header("Contenedores de UI")]
        [Tooltip("Panel principal de la tienda en el Canvas.")]
        [SerializeField] private GameObject shopPanel;
        [Tooltip("Texto para mostrar las monedas actuales disponibles.")]
        [SerializeField] private TextMeshProUGUI coinsBalanceText;

        [Header("Botones de Mejoras")]
        [SerializeField] private Button buyGrapplingHookButton;
        [SerializeField] private TextMeshProUGUI grapplingHookStatusText;

        [SerializeField] private Button buyJetBootsButton;
        [SerializeField] private TextMeshProUGUI jetBootsStatusText;

        [SerializeField] private Button buyAntiSlipGlovesButton;
        [SerializeField] private TextMeshProUGUI antiSlipGlovesStatusText;

        [SerializeField] private Button buyHydrodashButton;
        [SerializeField] private TextMeshProUGUI hydrodashStatusText;

        [SerializeField] private Button closeShopButton;

        [Header("Audio y Efectos")]
        [SerializeField] private AudioClip purchaseSuccessSfx;
        [SerializeField] private AudioClip purchaseFailedSfx;

        private bool isOpen = false;
        private MerchantNPC currentMerchant;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            // Suscribirse a eventos de GameManager si las monedas cambian
            GameManager.OnCoinsChanged += OnCoinsChangedCallback;

            if (closeShopButton != null)
            {
                closeShopButton.onClick.AddListener(CloseShop);
            }
        }

        private void OnDestroy()
        {
            GameManager.OnCoinsChanged -= OnCoinsChangedCallback;
        }

        private void Update()
        {
            if (!isOpen) return;

            // Teclas para cerrar la tienda: Escape o E
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    CloseShop();
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.buttonEast.wasPressedThisFrame) // Botón B / Círculo para cerrar
                {
                    CloseShop();
                }
            }
        }

        public void OpenShop(MerchantNPC merchant = null)
        {
            isOpen = true;
            currentMerchant = merchant;

            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
            }

            // Bloquear el movimiento de Pingu mientras navega en la tienda
            LockPlayerMovement(true);

            // Refrescar saldo y estado de los botones
            RefreshShopUI();
            Debug.Log("[ShopManager] Tienda del Mercader abierta. Movimiento del jugador bloqueado.");
        }

        public void CloseShop()
        {
            if (!isOpen) return;

            isOpen = false;

            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            // Desbloquear el movimiento de Pingu al salir de la tienda
            LockPlayerMovement(false);

            currentMerchant = null;
            Debug.Log("[ShopManager] Tienda cerrada. Movimiento del jugador restaurado.");
        }

        private void LockPlayerMovement(bool locked)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                var controller = playerObj.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.SetMovementLocked(locked);
                }
            }
        }

        private void OnCoinsChangedCallback(int newCoins)
        {
            if (isOpen)
            {
                RefreshShopUI();
            }
        }

        public void RefreshShopUI()
        {
            int currentCoins = GameManager.Instance != null ? GameManager.Instance.CurrentCoins : 0;

            if (coinsBalanceText != null)
            {
                coinsBalanceText.text = $"Monedas: {currentCoins}";
            }

            // Actualizar botón 1: Mano con Gancho (30)
            UpdateUpgradeButton(
                UpgradeType.GrapplingHook,
                PriceGrapplingHook,
                buyGrapplingHookButton,
                grapplingHookStatusText,
                currentCoins
            );

            // Actualizar botón 2: Jet-Boots (20)
            UpdateUpgradeButton(
                UpgradeType.JetBoots,
                PriceJetBoots,
                buyJetBootsButton,
                jetBootsStatusText,
                currentCoins
            );

            // Actualizar botón 3: Guantes Antideslizantes (15)
            UpdateUpgradeButton(
                UpgradeType.AntiSlipGloves,
                PriceAntiSlipGloves,
                buyAntiSlipGlovesButton,
                antiSlipGlovesStatusText,
                currentCoins
            );

            // Actualizar botón 4: Hidrodash (25)
            UpdateUpgradeButton(
                UpgradeType.Hydrodash,
                PriceHydrodash,
                buyHydrodashButton,
                hydrodashStatusText,
                currentCoins
            );
        }

        private void UpdateUpgradeButton(
            UpgradeType type,
            int cost,
            Button button,
            TextMeshProUGUI statusText,
            int currentCoins)
        {
            if (button == null) return;

            bool isPurchased = PlayerStats.Instance != null && PlayerStats.Instance.HasUpgrade(type);

            if (isPurchased)
            {
                // Si ya fue comprada, se deshabilita el botón y se marca como COMPRADO
                button.interactable = false;
                if (statusText != null)
                {
                    statusText.text = "COMPRADO";
                    statusText.color = new Color(0.4f, 0.9f, 0.4f); // Verde claro
                }
            }
            else
            {
                // Si aún no se compra, verificar si alcanzan las monedas
                bool canAfford = currentCoins >= cost;
                button.interactable = canAfford;

                if (statusText != null)
                {
                    statusText.text = $"{cost} Monedas";
                    statusText.color = canAfford ? Color.white : new Color(0.9f, 0.3f, 0.3f); // Rojo si no alcanza
                }
            }
        }

        #region Métodos de Compra para Botones
        public void BuyGrapplingHook()
        {
            TryBuyUpgrade(UpgradeType.GrapplingHook, PriceGrapplingHook);
        }

        public void BuyJetBoots()
        {
            TryBuyUpgrade(UpgradeType.JetBoots, PriceJetBoots);
        }

        public void BuyAntiSlipGloves()
        {
            TryBuyUpgrade(UpgradeType.AntiSlipGloves, PriceAntiSlipGloves);
        }

        public void BuyHydrodash()
        {
            TryBuyUpgrade(UpgradeType.Hydrodash, PriceHydrodash);
        }

        public bool TryBuyUpgrade(UpgradeType type, int cost)
        {
            if (PlayerStats.Instance == null)
            {
                Debug.LogError("[ShopManager] PlayerStats.Instance no encontrado.");
                return false;
            }

            // 1. Validar si ya está comprada
            if (PlayerStats.Instance.HasUpgrade(type))
            {
                Debug.LogWarning($"[ShopManager] La mejora {type} ya fue comprada anteriormente.");
                return false;
            }

            // 2. Validar si tiene suficientes monedas en GameManager
            if (GameManager.Instance == null || GameManager.Instance.CurrentCoins < cost)
            {
                PlayAudio(purchaseFailedSfx);
                Debug.LogWarning($"[ShopManager] Monedas insuficientes para {type}. Requiere {cost}, tiene {GameManager.Instance?.CurrentCoins ?? 0}.");
                return false;
            }

            // 3. Descontar monedas del GameManager
            bool spentSuccess = GameManager.Instance.SpendCoins(cost);
            if (!spentSuccess)
            {
                PlayAudio(purchaseFailedSfx);
                return false;
            }

            // 4. Marcar mejora como comprada y aplicar modificaciones de habilidad
            PlayerStats.Instance.UnlockUpgrade(type);

            // 5. Audio de éxito
            PlayAudio(purchaseSuccessSfx);

            // 6. Refrescar interfaz y deshabilitar botón
            RefreshShopUI();

            Debug.Log($"[ShopManager] ¡Compra exitosa! {type} adquirida por {cost} monedas.");
            return true;
        }

        private void PlayAudio(AudioClip clip)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : transform.position);
            }
        }
        #endregion

#if UNITY_EDITOR
        public void SetupInEditor(
            GameObject panel,
            TextMeshProUGUI balanceText,
            Button btnHook, TextMeshProUGUI txtHook,
            Button btnBoots, TextMeshProUGUI txtBoots,
            Button btnGloves, TextMeshProUGUI txtGloves,
            Button btnDash, TextMeshProUGUI txtDash,
            Button btnClose)
        {
            shopPanel = panel;
            coinsBalanceText = balanceText;
            buyGrapplingHookButton = btnHook;
            grapplingHookStatusText = txtHook;
            buyJetBootsButton = btnBoots;
            jetBootsStatusText = txtBoots;
            buyAntiSlipGlovesButton = btnGloves;
            antiSlipGlovesStatusText = txtGloves;
            buyHydrodashButton = btnDash;
            hydrodashStatusText = txtDash;
            closeShopButton = btnClose;

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
