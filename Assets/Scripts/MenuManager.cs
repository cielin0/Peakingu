using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Peakingu
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Navegación de Escenas")]
        [Tooltip("Nombre de la escena a cargar al presionar Iniciar.")]
        [SerializeField] private string nextSceneName = "Nivel1";

        [Header("Transición de Fundido a Negro")]
        [Tooltip("CanvasGroup que cubre toda la pantalla para el efecto de fade.")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [Tooltip("Duración en segundos de la transición a negro.")]
        [SerializeField] private float fadeDuration = 1.0f;

        [Header("Paneles de la Interfaz")]
        [Tooltip("Panel principal que contiene los botones principales.")]
        [SerializeField] private GameObject mainButtonsPanel;
        [Tooltip("Panel emergente que explica los controles del juego.")]
        [SerializeField] private GameObject controlsPanel;
        [Tooltip("Panel emergente con las opciones de configuración.")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Configuración de Audio")]
        [Tooltip("Slider para regular el volumen maestro del juego.")]
        [SerializeField] private Slider volumeSlider;
        [Tooltip("Texto para mostrar el porcentaje actual de volumen.")]
        [SerializeField] private TextMeshProUGUI volumeValueText;

        [Header("Retroalimentación y Audio")]
        [Tooltip("Clip de sonido al hacer clic en un botón.")]
        [SerializeField] private AudioClip buttonClickSfx;
        [SerializeField] private AudioSource audioSource;

        private bool isStartingGame = false;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // Inicializar volumen desde PlayerPrefs (por defecto 100%)
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            AudioListener.volume = savedVolume;

            if (volumeSlider != null)
            {
                volumeSlider.value = savedVolume;
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }

            UpdateVolumeLabel(savedVolume);
        }

        private void Start()
        {
            // Estado visual inicial
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
                fadeOverlay.interactable = false;
            }

            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        #region Botones Principales
        /// <summary>
        /// Inicia la partida ejecutando un fundido a negro y cargando la escena Nivel1.
        /// </summary>
        public void OnStartClicked()
        {
            if (isStartingGame) return;

            PlayClickSound();
            StartCoroutine(FadeAndLoadRoutine(nextSceneName));
        }

        /// <summary>
        /// Botón Cargar partida (sin función por ahora según los requerimientos).
        /// </summary>
        public void OnLoadGameClicked()
        {
            PlayClickSound();
            Debug.Log("[MenuManager] 'Cargar Partida': Función reservada para futuras versiones.");
        }

        /// <summary>
        /// Abre el panel de Controles.
        /// </summary>
        public void OnControlsClicked()
        {
            PlayClickSound();
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        /// <summary>
        /// Abre el panel de Configuración con el slider de volumen.
        /// </summary>
        public void OnSettingsClicked()
        {
            PlayClickSound();
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);

            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
            }
        }

        /// <summary>
        /// Regresa al panel principal desde cualquier submenú.
        /// </summary>
        public void OnBackToMainMenuClicked()
        {
            PlayClickSound();
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Cierra la aplicación (útil para compilaciones standalone de Windows).
        /// </summary>
        public void OnQuitClicked()
        {
            PlayClickSound();
            Debug.Log("[MenuManager] Saliendo del juego...");
            Application.Quit();
        }
        #endregion

        #region Configuración de Volumen
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

        #region Transición de Fundido
        private IEnumerator FadeAndLoadRoutine(string sceneName)
        {
            isStartingGame = true;

            if (fadeOverlay != null)
            {
                fadeOverlay.blocksRaycasts = true;
                fadeOverlay.interactable = true;

                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    fadeOverlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                    yield return null;
                }

                fadeOverlay.alpha = 1f;
            }

            SceneManager.LoadScene(sceneName);
        }

        private void PlayClickSound()
        {
            if (buttonClickSfx != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(buttonClickSfx);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(buttonClickSfx, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
                }
            }
        }
        #endregion

        #region Setup en Editor
#if UNITY_EDITOR
        public void SetupInEditor(
            CanvasGroup fadeGroup,
            GameObject mainPanel,
            GameObject controls,
            GameObject settings,
            Slider slider,
            TextMeshProUGUI volumeText,
            string nextScene = "Nivel1")
        {
            fadeOverlay = fadeGroup;
            mainButtonsPanel = mainPanel;
            controlsPanel = controls;
            settingsPanel = settings;
            volumeSlider = slider;
            volumeValueText = volumeText;
            nextSceneName = nextScene;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        #endregion
    }
}
