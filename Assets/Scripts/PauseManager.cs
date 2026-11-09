using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

namespace Peakingu
{
    /// <summary>
    /// Gestiona el Menú de Pausa (con ESC), la pantalla de Game Over y la pantalla de Victoria,
    /// garantizando que Time.timeScale se pause y restaure correctamente sin romper el temporizador del juego.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        [Header("--- Paneles Principales ---")]
        [Tooltip("Panel modal de Pausa.")]
        [SerializeField] private GameObject pausePanel;
        [Tooltip("Panel modal de Configuración (Audio / Opciones).")]
        [SerializeField] private GameObject settingsPanel;
        [Tooltip("Panel modal de Game Over.")]
        [SerializeField] private GameObject gameOverPanel;
        [Tooltip("Panel modal de Victoria.")]
        [SerializeField] private GameObject victoryPanel;

        [Header("--- Menú de Pausa (Botones) ---")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button pauseQuitToMenuButton;

        [Header("--- Configuración (Audio) ---")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumeValueText;
        [SerializeField] private Button closeSettingsButton;

        [Header("--- Game Over UI ---")]
        [SerializeField] private TextMeshProUGUI gameOverReasonText;
        [SerializeField] private Button restartGameOverButton;
        [SerializeField] private Button quitMenuGameOverButton;

        [Header("--- Victoria UI ---")]
        [SerializeField] private TextMeshProUGUI victoryTitleText;
        [SerializeField] private TextMeshProUGUI victoryStatsText;
        [SerializeField] private Button restartVictoryButton;
        [SerializeField] private Button quitMenuVictoryButton;

        [Header("--- Audio Opcional ---")]
        [SerializeField] private AudioClip buttonClickSfx;
        [SerializeField] private AudioClip victorySfx;
        [SerializeField] private AudioClip gameOverSfx;
        [SerializeField] private AudioSource audioSource;

        private bool isPaused = false;
        private bool isGameOver = false;
        private bool isVictory = false;
        private bool wasShopOpenLastFrame = false;

        public bool IsPaused => isPaused;
        public bool IsGameOver => isGameOver;
        public bool IsVictory => isVictory;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Garantizar siempre que el tiempo comience normal (timeScale = 1) al instanciarse
            Time.timeScale = 1f;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            InitAudioSettings();
        }

        private void Start()
        {
            // Ocultar todos los paneles modales al inicio
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
        }

        private void OnEnable()
        {
            GameManager.OnGameOver += HandleGameOver;
            GameManager.OnVictory += HandleVictory;
        }

        private void OnDisable()
        {
            GameManager.OnGameOver -= HandleGameOver;
            GameManager.OnVictory -= HandleVictory;

            // Medida de seguridad: Si este objeto se desactiva o destruye, restaurar timeScale a 1f
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void Update()
        {
            // 1. Si la tienda del mercader está abierta, el ESC lo consume ella para cerrarse.
            if (ShopManager.Instance != null && ShopManager.Instance.IsOpen)
            {
                wasShopOpenLastFrame = true;
                return;
            }

            // Evitar pausar en el mismo frame que se acaba de cerrar la tienda
            if (wasShopOpenLastFrame)
            {
                wasShopOpenLastFrame = false;
                return;
            }

            // 2. Si ya ocurrió Game Over o Victoria, no permitir abrir/cerrar pausa
            if (isGameOver || isVictory)
            {
                return;
            }

            // 3. Detección de tecla ESC o botón Start del mando
            bool escPressed = false;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                escPressed = true;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.startButton.wasPressedThisFrame)
            {
                escPressed = true;
            }

            if (escPressed)
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                else if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }

        #region Control de Pausa
        /// <summary>
        /// Pausa la simulación del juego y abre el menú de pausa.
        /// </summary>
        public void PauseGame()
        {
            if (isGameOver || isVictory) return;

            isPaused = true;
            Time.timeScale = 0f; // Pausa la física y el conteo regresivo de Time.deltaTime sin alterar datos

            LockPlayerInput(true);

            if (pausePanel != null) pausePanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            PlaySound(buttonClickSfx);
            Debug.Log("[PauseManager] Juego pausado (Time.timeScale = 0).");
        }

        /// <summary>
        /// Reanuda la simulación del juego y cierra el menú de pausa.
        /// </summary>
        public void ResumeGame()
        {
            isPaused = false;

            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            LockPlayerInput(false);

            Time.timeScale = 1f; // Reanuda la simulación y el timer continúa suavemente

            PlaySound(buttonClickSfx);
            Debug.Log("[PauseManager] Juego reanudado (Time.timeScale = 1).");
        }
        #endregion

        #region Configuración / Volumen
        private void InitAudioSettings()
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            AudioListener.volume = savedVolume;

            if (volumeSlider != null)
            {
                volumeSlider.value = savedVolume;
                volumeSlider.onValueChanged.RemoveAllListeners();
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }

            UpdateVolumeLabel(savedVolume);
        }

        public void OpenSettings()
        {
            PlaySound(buttonClickSfx);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                if (volumeSlider != null)
                {
                    volumeSlider.value = AudioListener.volume;
                }
                UpdateVolumeLabel(AudioListener.volume);
            }
        }

        public void CloseSettings()
        {
            PlaySound(buttonClickSfx);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);

            PlayerPrefs.Save();
        }

        public void SetVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat("MasterVolume", volume);
            UpdateVolumeLabel(volume);
        }

        private void UpdateVolumeLabel(float volume)
        {
            if (volumeValueText != null)
            {
                int percentage = Mathf.RoundToInt(volume * 100f);
                volumeValueText.text = $"{percentage}%";
            }
        }
        #endregion

        #region Game Over
        private void HandleGameOver()
        {
            if (isGameOver) return;

            isGameOver = true;
            isPaused = false;

            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Detener el juego
            Time.timeScale = 0f;
            LockPlayerInput(true);

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);

                if (gameOverReasonText != null && GameManager.Instance != null)
                {
                    if (GameManager.Instance.CurrentTimeRemaining <= 0f)
                    {
                        gameOverReasonText.text = "¡EL TIEMPO SE HA AGOTADO!\nEl pescado en el horno se ha quemado.";
                    }
                    else
                    {
                        gameOverReasonText.text = "¡PINGU SE HA QUEDADO SIN VIDAS!\nHas caído de la montaña.";
                    }
                }
            }

            PlaySound(gameOverSfx);
            Debug.Log("[PauseManager] Pantalla de Game Over desplegada.");
        }
        #endregion

        #region Pantalla de Victoria
        private void HandleVictory()
        {
            ShowVictory();
        }

        /// <summary>
        /// Muestra la pantalla de victoria con estadísticas de tiempo y monedas.
        /// </summary>
        public void ShowVictory()
        {
            if (isVictory) return;

            isVictory = true;
            isPaused = false;

            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Detener la simulación de juego
            Time.timeScale = 0f;
            LockPlayerInput(true);

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);

                if (victoryStatsText != null && GameManager.Instance != null)
                {
                    float timeLeft = GameManager.Instance.CurrentTimeRemaining;
                    int minutes = Mathf.FloorToInt(timeLeft / 60f);
                    int seconds = Mathf.FloorToInt(timeLeft % 60f);
                    int coins = GameManager.Instance.CurrentCoins;

                    victoryStatsText.text = $"TIEMPO RESTANTE: {minutes:00}:{seconds:00}\nMONEDAS RECOLECTADAS: {coins}";
                }
            }

            PlaySound(victorySfx);
            Debug.Log("[PauseManager] ¡Pantalla de Victoria desplegada!");
        }
        #endregion

        #region Navegación y Reinicio
        /// <summary>
        /// Reinicia la partida restaurando timeScale a 1f, reiniciando estadísticas y recargando la escena actual.
        /// </summary>
        public void RestartGame()
        {
            PlaySound(buttonClickSfx);

            // CRUCIAL: Restaurar escala de tiempo ANTES de reiniciar el estado y recargar la escena
            Time.timeScale = 1f;

            isPaused = false;
            isGameOver = false;
            isVictory = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetGame();
            }

            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
            Debug.Log($"[PauseManager] Partida reiniciada. Escena '{currentSceneName}' recargada con Time.timeScale = 1f.");
        }

        /// <summary>
        /// Vuelve al Menú Principal restaurando timeScale a 1f.
        /// </summary>
        public void QuitToMainMenu()
        {
            PlaySound(buttonClickSfx);

            // CRUCIAL: Restaurar escala de tiempo a 1f para que el menú principal funcione correctamente
            Time.timeScale = 1f;

            isPaused = false;
            isGameOver = false;
            isVictory = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetGame();
            }

            SceneManager.LoadScene("Menu");
            Debug.Log("[PauseManager] Regresando al Menú Principal con Time.timeScale = 1f.");
        }
        #endregion

        private void LockPlayerInput(bool locked)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.SetMovementLocked(locked);
                }
            }
        }

        private void PlaySound(AudioClip clip)
        {
            // AudioManager tiene prioridad para button click, game over y victoria
            if (AudioManager.Instance != null)
            {
                if (clip == buttonClickSfx)       { AudioManager.Instance.PlayButtonClick();  return; }
                if (clip == gameOverSfx)          { /* Game Over lo maneja OnGameOver del AudioManager */ return; }
                if (clip == victorySfx)           { /* Victoria la maneja OnVictory del AudioManager */ return; }
            }

            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #region Configuración en Editor
        public void SetupInEditor(
            GameObject pPanel,
            Button btnResume,
            Button btnSettings,
            Button btnQuitPause,
            GameObject sPanel,
            Slider sVolume,
            TextMeshProUGUI txtVolumeVal,
            Button btnCloseSettings,
            GameObject goPanel,
            TextMeshProUGUI txtGOReason,
            Button btnRestartGO,
            Button btnQuitGO,
            GameObject vPanel,
            TextMeshProUGUI txtVTitle,
            TextMeshProUGUI txtVStats,
            Button btnRestartV,
            Button btnQuitV)
        {
            pausePanel = pPanel;
            resumeButton = btnResume;
            settingsButton = btnSettings;
            pauseQuitToMenuButton = btnQuitPause;

            settingsPanel = sPanel;
            volumeSlider = sVolume;
            volumeValueText = txtVolumeVal;
            closeSettingsButton = btnCloseSettings;

            gameOverPanel = goPanel;
            gameOverReasonText = txtGOReason;
            restartGameOverButton = btnRestartGO;
            quitMenuGameOverButton = btnQuitGO;

            victoryPanel = vPanel;
            victoryTitleText = txtVTitle;
            victoryStatsText = txtVStats;
            restartVictoryButton = btnRestartV;
            quitMenuVictoryButton = btnQuitV;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        #endregion
    }
}
