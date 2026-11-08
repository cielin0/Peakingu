using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Peakingu
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("--- Vidas (Corazones) ---")]
        [Tooltip("Contenedor padre que alberga los iconos de corazones.")]
        [SerializeField] private Transform heartsContainer;
        [Tooltip("Lista ordenada de imágenes de corazones en el HUD.")]
        [SerializeField] private List<Image> heartImages = new List<Image>();
        [Tooltip("Sprite del corazón lleno / activo.")]
        [SerializeField] private Sprite heartFullSprite;
        [Tooltip("Sprite del corazón vacío / consumido.")]
        [SerializeField] private Sprite heartEmptySprite;
        [Tooltip("Color de corazón activo (por defecto rojo).")]
        [SerializeField] private Color fullHeartColor = Color.white;
        [Tooltip("Color de corazón consumido (por defecto semitransparente o gris).")]
        [SerializeField] private Color emptyHeartColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("--- Monedas ---")]
        [Tooltip("Texto TextMeshPro para el contador de monedas.")]
        [SerializeField] private TextMeshProUGUI coinsText;
        [Tooltip("Icono visual de la moneda.")]
        [SerializeField] private Image coinIcon;
        [Tooltip("Formato para el texto de monedas (ej: 'x {0}' o '{0}').")]
        [SerializeField] private string coinsFormat = "x {0}";

        [Header("--- Temporizador Regresivo ---")]
        [Tooltip("Texto TextMeshPro para el temporizador.")]
        [SerializeField] private TextMeshProUGUI timerText;
        [Tooltip("Prefijo del texto del temporizador (ej: 'TIME: ').")]
        [SerializeField] private string timerPrefix = "TIME: ";
        [Tooltip("Color normal del temporizador.")]
        [SerializeField] private Color timerNormalColor = Color.white;
        [Tooltip("Color de advertencia cuando queda poco tiempo.")]
        [SerializeField] private Color timerWarningColor = new Color(1f, 0.25f, 0.25f);
        [Tooltip("Segundos restantes para activar la alerta visual de tiempo agotándose.")]
        [SerializeField] private float warningThresholdSeconds = 60f;

        [Header("--- Altura (Metros) ---")]
        [Tooltip("Transform de Pingu para medir la altura Y.")]
        [SerializeField] private Transform playerTransform;
        [Tooltip("Texto TextMeshPro para la altura.")]
        [SerializeField] private TextMeshProUGUI heightText;
        [Tooltip("Prefijo del texto de altura (ej: 'HEIGHT: ').")]
        [SerializeField] private string heightPrefix = "HEIGHT: ";
        [Tooltip("Posición Y de referencia base (nivel del suelo/inicio).")]
        [SerializeField] private float baseHeightY = -1f;
        [Tooltip("Factor de conversión de unidades de Unity a metros.")]
        [SerializeField] private float heightMultiplier = 1f;

        [Header("--- Pantalla de Game Over ---")]
        [Tooltip("Panel emergente cuando ocurre Game Over (opcional).")]
        [SerializeField] private GameObject gameOverPanel;
        [Tooltip("Texto descriptivo de la causa de Game Over.")]
        [SerializeField] private TextMeshProUGUI gameOverReasonText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindPlayerReference();

            // Sincronizar datos iniciales desde el GameManager si ya existe
            if (GameManager.Instance != null)
            {
                UpdateLivesUI(GameManager.Instance.CurrentLives);
                UpdateCoinsUI(GameManager.Instance.CurrentCoins);
                UpdateTimerUI(GameManager.Instance.CurrentTimeRemaining);
            }
            else
            {
                // Valores por defecto de inicio
                UpdateLivesUI(5);
                UpdateCoinsUI(0);
                UpdateTimerUI(1200f);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            GameManager.OnLivesChanged += UpdateLivesUI;
            GameManager.OnCoinsChanged += UpdateCoinsUI;
            GameManager.OnTimerTick += UpdateTimerUI;
            GameManager.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            GameManager.OnLivesChanged -= UpdateLivesUI;
            GameManager.OnCoinsChanged -= UpdateCoinsUI;
            GameManager.OnTimerTick -= UpdateTimerUI;
            GameManager.OnGameOver -= HandleGameOver;
        }

        private void Update()
        {
            UpdateHeightUI();
        }

        private void FindPlayerReference()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
        }

        #region Actualización de UI
        public void UpdateLivesUI(int currentLives)
        {
            if (heartImages == null || heartImages.Count == 0)
            {
                // Si la lista está vacía, intentar buscar imágenes dentro del contenedor
                if (heartsContainer != null)
                {
                    heartImages = new List<Image>(heartsContainer.GetComponentsInChildren<Image>());
                }
            }

            for (int i = 0; i < heartImages.Count; i++)
            {
                if (heartImages[i] == null) continue;

                bool isAlive = i < currentLives;

                if (heartFullSprite != null && heartEmptySprite != null)
                {
                    heartImages[i].sprite = isAlive ? heartFullSprite : heartEmptySprite;
                    heartImages[i].color = fullHeartColor;
                }
                else
                {
                    // Alternativa visual en caso de no tener sprites asignados
                    heartImages[i].color = isAlive ? fullHeartColor : emptyHeartColor;
                }
            }
        }

        public void UpdateCoinsUI(int currentCoins)
        {
            if (coinsText != null)
            {
                coinsText.text = string.Format(coinsFormat, currentCoins);
            }
        }

        public void UpdateTimerUI(float secondsRemaining)
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(secondsRemaining / 60f);
            int seconds = Mathf.FloorToInt(secondsRemaining % 60f);

            timerText.text = $"{timerPrefix}{minutes:00}:{seconds:00}";

            // Alerta visual si queda poco tiempo
            if (secondsRemaining <= warningThresholdSeconds)
            {
                timerText.color = timerWarningColor;
            }
            else
            {
                timerText.color = timerNormalColor;
            }
        }

        private void UpdateHeightUI()
        {
            if (heightText == null) return;

            if (playerTransform == null)
            {
                FindPlayerReference();
            }

            float currentY = playerTransform != null ? playerTransform.position.y : baseHeightY;
            int meters = Mathf.Max(0, Mathf.RoundToInt((currentY - baseHeightY) * heightMultiplier));

            heightText.text = $"{heightPrefix}{meters}m";
        }

        private void HandleGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);

                if (gameOverReasonText != null && GameManager.Instance != null)
                {
                    if (GameManager.Instance.CurrentTimeRemaining <= 0f)
                    {
                        gameOverReasonText.text = "¡EL TIEMPO SE HA AGOTADO!\nEl pescado en el horno se ha quemado por completo.";
                    }
                    else
                    {
                        gameOverReasonText.text = "¡PINGU SE HA QUEDADO SIN VIDAS!\nHas caído de la montaña.";
                    }
                }
            }
        }
        #endregion

        #region Métodos de configuración para Editor
        public void Configure(
            Transform heartsParent,
            List<Image> hearts,
            Sprite fullHeart,
            Sprite emptyHeart,
            TextMeshProUGUI coins,
            TextMeshProUGUI timer,
            TextMeshProUGUI height,
            GameObject goPanel = null,
            TextMeshProUGUI goReason = null,
            float baseHeight = -1f)
        {
            heartsContainer = heartsParent;
            heartImages = hearts;
            heartFullSprite = fullHeart;
            heartEmptySprite = emptyHeart;
            coinsText = coins;
            timerText = timer;
            heightText = height;
            gameOverPanel = goPanel;
            gameOverReasonText = goReason;
            baseHeightY = baseHeight;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        #endregion
    }
}
