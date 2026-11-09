using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Peakingu
{
    /// <summary>
    /// AudioManager singleton — gestiona música por escena y todos los SFX del juego.
    ///
    /// ARQUITECTURA:
    ///  - DontDestroyOnLoad: persiste entre escenas.
    ///  - Dos AudioSource para música (con cross-fade suave).
    ///  - Un AudioSource para SFX (voz global de efectos).
    ///  - Canal dedicado a pasos (con eco espacial en la cueva).
    ///  - Volumen maestro sincronizado con PlayerPrefs ("MasterVolume").
    ///  - Se suscribe a eventos de GameManager y PlayerStats para reproducir SFX reactivamente.
    ///
    /// CONEXIÓN CON SCRIPTS EXTERNOS:
    ///  Coin          → AudioManager.Instance.PlayCoin()
    ///  PlayerController → llama PlayJump / PlayLand / PlayFootstep / PlayDash
    ///  GrappleController → llama PlayGrappleLaunch / PlayGrappleRelease
    ///  ShopManager   → llama PlayPurchaseSuccess / PlayPurchaseFail
    ///  MenuManager / PauseManager → llama PlayButtonClick
    ///  GameManager   → reacciona a OnGameOver / OnVictory / OnSceneLoaded para música
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<AudioManager>();
                    if (instance == null)
                    {
                        var prefab = Resources.Load<GameObject>("AudioManager");
                        if (prefab == null) prefab = Resources.Load<GameObject>("Prefabs/AudioManager");
                        if (prefab != null)
                        {
                            var go = Instantiate(prefab);
                            instance = go.GetComponent<AudioManager>();
                        }
                        else
                        {
                            var go = new GameObject("AudioManager");
                            instance = go.AddComponent<AudioManager>();
                        }
                    }
                }
                return instance;
            }
        }

        // ─────────────────────────── Inspector ───────────────────────────

        [Header("── Música por Escena ──")]
        [Tooltip("Clip de música para el Menú Principal.")]
        [SerializeField] private AudioClip musicMenu;
        [Tooltip("Clip de música para Nivel1 (montaña).")]
        [SerializeField] private AudioClip musicNivel1;
        [Tooltip("Clip de música para la Cueva.")]
        [SerializeField] private AudioClip musicCueva;
        [Tooltip("Clip de música en la pantalla de Game Over.")]
        [SerializeField] private AudioClip musicGameOver;
        [Tooltip("Clip de música en la pantalla de Victoria.")]
        [SerializeField] private AudioClip musicVictory;
        [Tooltip("Duración en segundos del cross-fade entre pistas.")]
        [SerializeField] private float crossFadeDuration = 1.0f;
        [Tooltip("Volumen base de la música (0–1).")]
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.5f;

        [Header("── SFX de Movimiento ──")]
        [Tooltip("Sonido al saltar.")]
        [SerializeField] private AudioClip sfxJump;
        [Tooltip("Sonido al aterrizar (caída/impacto suave).")]
        [SerializeField] private AudioClip sfxLand;
        [Tooltip("Sonidos de pasos (se elige uno aleatoriamente). En la cueva se añade eco.")]
        [SerializeField] private AudioClip[] sfxFootsteps;
        [Tooltip("Sonido del dash.")]
        [SerializeField] private AudioClip sfxDash;

        [Header("── SFX de Gancho ──")]
        [Tooltip("Sonido al lanzar el gancho cyborg.")]
        [SerializeField] private AudioClip sfxGrappleLaunch;
        [Tooltip("Sonido al soltar el gancho.")]
        [SerializeField] private AudioClip sfxGrappleRelease;

        [Header("── SFX de Moneda y Tienda ──")]
        [Tooltip("Sonido al recolectar una moneda.")]
        [SerializeField] private AudioClip sfxCoin;
        [Tooltip("Sonido al completar una compra en la tienda.")]
        [SerializeField] private AudioClip sfxPurchaseSuccess;
        [Tooltip("Sonido cuando la compra falla (fondos insuficientes).")]
        [SerializeField] private AudioClip sfxPurchaseFail;

        [Header("── SFX de UI / Botones ──")]
        [Tooltip("Sonido al hacer click en cualquier botón de menú.")]
        [SerializeField] private AudioClip sfxButtonClick;
        [Tooltip("Sonido de Game Over.")]
        [SerializeField] private AudioClip sfxGameOver;
        [Tooltip("Sonido de Victoria.")]
        [SerializeField] private AudioClip sfxVictory;

        [Header("── SFX de Pasos con Eco (Cueva) ──")]
        [Tooltip("Fracción del volumen del eco de paso (0 = sin eco).")]
        [Range(0f, 0.6f)]
        [SerializeField] private float echoVolume = 0.35f;
        [Tooltip("Retraso en segundos del eco de los pasos en la cueva.")]
        [SerializeField] private float echoDelay = 0.16f;

        [Header("── Volúmenes SFX ──")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.45f;

        // ─────────────────────────── Estado interno ───────────────────────────

        private AudioSource musicSourceA;   // Canal A de música
        private AudioSource musicSourceB;   // Canal B de música (cross-fade)
        private AudioSource sfxSource;      // Canal principal de SFX
        private AudioSource footstepSource; // Canal dedicado a pasos (para control de intervalo)

        private bool isMusicAActive = true;
        private Coroutine crossFadeCoroutine;

        private string lastSceneName = "";

        // Throttle de pasos
        private float footstepInterval = 0.32f;
        private float footstepTimer = 0f;
        private bool inCaveScene = false;

        // ─────────────────────────── Unity Lifecycle ───────────────────────────

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            BuildAudioSources();
            ApplySavedVolume();
        }

        private void Start()
        {
            // Reproducir inmediatamente la música de la escena de arranque si no está sonando ya
            string currentScene = SceneManager.GetActiveScene().name;
            PlaySceneMusic(currentScene);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameManager.OnGameOver   += OnGameOver;
            GameManager.OnVictory    += OnVictory;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameManager.OnGameOver   -= OnGameOver;
            GameManager.OnVictory    -= OnVictory;
        }

        // ─────────────────────────── Música ───────────────────────────

        /// <summary>
        /// Inicia la reproducción de un clip de música con cross-fade.
        /// Si el clip ya está sonando, no hace nada.
        /// </summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;

            var activeSource = isMusicAActive ? musicSourceA : musicSourceB;
            if (activeSource.clip == clip && activeSource.isPlaying) return;

            if (crossFadeCoroutine != null) StopCoroutine(crossFadeCoroutine);
            crossFadeCoroutine = StartCoroutine(CrossFadeMusic(clip));
        }

        public void StopMusic()
        {
            if (crossFadeCoroutine != null) StopCoroutine(crossFadeCoroutine);
            if (musicSourceA != null) musicSourceA.Stop();
            if (musicSourceB != null) musicSourceB.Stop();
        }

        public void SetMusicVolume(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            var active = isMusicAActive ? musicSourceA : musicSourceB;
            if (active != null) active.volume = musicVolume;
        }

        // ─────────────────────────── SFX Públicos ───────────────────────────

        /// <summary>Sonido de salto de Pingu.</summary>
        public void PlayJump()   => PlaySfx(sfxJump, sfxVolume);

        /// <summary>Sonido al aterrizar (caída / impacto contra el suelo).</summary>
        public void PlayLand()   => PlaySfx(sfxLand, sfxVolume * 0.85f);

        /// <summary>Sonido del dash.</summary>
        public void PlayDash()   => PlaySfx(sfxDash, sfxVolume);

        /// <summary>Sonido al recolectar moneda.</summary>
        public void PlayCoin()   => PlaySfx(sfxCoin, sfxVolume);

        /// <summary>Sonido de compra exitosa en tienda.</summary>
        public void PlayPurchaseSuccess() => PlaySfx(sfxPurchaseSuccess, sfxVolume);

        /// <summary>Sonido de compra fallida en tienda.</summary>
        public void PlayPurchaseFail()    => PlaySfx(sfxPurchaseFail, sfxVolume * 0.8f);

        /// <summary>Sonido de click de botón en UI.</summary>
        public void PlayButtonClick()     => PlaySfx(sfxButtonClick, sfxVolume * 0.9f);

        /// <summary>Sonido al lanzar el gancho.</summary>
        public void PlayGrappleLaunch()   => PlaySfx(sfxGrappleLaunch, sfxVolume);

        /// <summary>Sonido al soltar el gancho.</summary>
        public void PlayGrappleRelease()  => PlaySfx(sfxGrappleRelease, sfxVolume * 0.75f);

        /// <summary>
        /// Reproduce un paso de Pingu con throttle automático.
        /// En escenas de cueva añade un eco diferido.
        /// </summary>
        /// <param name="isMoving">True si el personaje se está moviendo horizontalmente.</param>
        public void PlayFootstep(bool isMoving)
        {
            if (!isMoving || sfxFootsteps == null || sfxFootsteps.Length == 0) return;

            footstepTimer -= Time.deltaTime;
            if (footstepTimer > 0f) return;

            footstepTimer = footstepInterval;

            var clip = sfxFootsteps[Random.Range(0, sfxFootsteps.Length)];
            if (clip != null && footstepSource != null)
            {
                footstepSource.PlayOneShot(clip, footstepVolume);

                if (inCaveScene && echoVolume > 0f)
                {
                    StartCoroutine(PlayEcho(clip));
                }
            }
        }

        // ─────────────────────────── Manejo de Escena ───────────────────────────

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlaySceneMusic(scene.name);
        }

        public void PlaySceneMusic(string sceneName)
        {
            if (sceneName == lastSceneName && (musicSourceA.isPlaying || musicSourceB.isPlaying)) return;
            lastSceneName = sceneName;

            inCaveScene = sceneName.ToLower().Contains("cueva");

            switch (sceneName)
            {
                case "Menu":
                    PlayMusic(musicMenu);
                    break;
                case "Nivel1":
                    PlayMusic(musicNivel1);
                    break;
                case "Cueva":
                    PlayMusic(musicCueva);
                    break;
                default:
                    // Si es otra escena, por defecto si hay música de nivel usarla
                    if (musicNivel1 != null) PlayMusic(musicNivel1);
                    break;
            }
        }

        private void OnGameOver()
        {
            PlaySfx(sfxGameOver, sfxVolume);
            if (musicGameOver != null)
                PlayMusic(musicGameOver);
            else
                StartCoroutine(FadeOutMusic(1.0f));
        }

        private void OnVictory()
        {
            PlaySfx(sfxVictory, sfxVolume);
            if (musicVictory != null)
                PlayMusic(musicVictory);
        }

        // ─────────────────────────── Volumen Global ───────────────────────────

        /// <summary>
        /// Ajusta el volumen maestro (AudioListener) y lo persiste en PlayerPrefs.
        /// Llamado por los sliders de volumen de MenuManager y PauseManager.
        /// </summary>
        public void SetMasterVolume(float v)
        {
            v = Mathf.Clamp01(v);
            AudioListener.volume = v;
            PlayerPrefs.SetFloat("MasterVolume", v);
        }

        public float GetMasterVolume() => AudioListener.volume;

        // ─────────────────────────── Helpers internos ───────────────────────────

        private void BuildAudioSources()
        {
            if (musicSourceA == null) musicSourceA = CreateSource("MusicA", loop: true,  volume: musicVolume);
            if (musicSourceB == null) musicSourceB = CreateSource("MusicB", loop: true,  volume: 0f);
            if (sfxSource == null)    sfxSource    = CreateSource("SFX",    loop: false, volume: sfxVolume);
            if (footstepSource == null) footstepSource = CreateSource("Footsteps", loop: false, volume: footstepVolume);
        }

        private AudioSource CreateSource(string sourceName, bool loop, float volume)
        {
            var go = new GameObject($"AudioSource_{sourceName}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop          = loop;
            src.volume        = volume;
            src.playOnAwake   = false;
            src.spatialBlend  = 0f; // 2D puro
            return src;
        }

        private void ApplySavedVolume()
        {
            float saved = PlayerPrefs.GetFloat("MasterVolume", 1f);
            AudioListener.volume = saved;
        }

        private void PlaySfx(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }

        private IEnumerator CrossFadeMusic(AudioClip newClip)
        {
            var outSource = isMusicAActive ? musicSourceA : musicSourceB;
            var inSource  = isMusicAActive ? musicSourceB : musicSourceA;

            inSource.clip   = newClip;
            inSource.volume = 0f;
            inSource.Play();

            float elapsed = 0f;
            float startVolume = outSource.volume;

            while (elapsed < crossFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / crossFadeDuration);
                inSource.volume  = Mathf.Lerp(0f, musicVolume, t);
                outSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            outSource.Stop();
            outSource.clip = null;
            isMusicAActive = !isMusicAActive;
        }

        private IEnumerator FadeOutMusic(float duration)
        {
            var active = isMusicAActive ? musicSourceA : musicSourceB;
            float startVol = active.volume;
            float elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                active.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }

            active.Stop();
        }

        private IEnumerator PlayEcho(AudioClip clip)
        {
            yield return new WaitForSeconds(echoDelay);
            if (footstepSource != null && clip != null)
            {
                footstepSource.PlayOneShot(clip, footstepVolume * echoVolume);
            }
        }

#if UNITY_EDITOR
        public void ConfigureClips(
            AudioClip menu, AudioClip nivel1, AudioClip cueva, AudioClip gOver, AudioClip vict,
            AudioClip jump, AudioClip land, AudioClip[] steps, AudioClip dash,
            AudioClip gLaunch, AudioClip gRelease, AudioClip coin,
            AudioClip buySuccess, AudioClip buyFail, AudioClip btnClick)
        {
            musicMenu = menu;
            musicNivel1 = nivel1;
            musicCueva = cueva;
            musicGameOver = gOver;
            musicVictory = vict;
            sfxJump = jump;
            sfxLand = land;
            sfxFootsteps = steps;
            sfxDash = dash;
            sfxGrappleLaunch = gLaunch;
            sfxGrappleRelease = gRelease;
            sfxCoin = coin;
            sfxPurchaseSuccess = buySuccess;
            sfxPurchaseFail = buyFail;
            sfxButtonClick = btnClick;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
