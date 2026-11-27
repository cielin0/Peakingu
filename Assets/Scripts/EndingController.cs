using System.Collections;
using UnityEngine;
using TMPro;

namespace Peakingu
{
    /// <summary>
    /// Controla la secuencia cinemática de la escena Final (interior de la casa en la cima).
    /// Muestra al pescado a salvo o quemado según GameManager.Instance.FishBurnt
    /// (o forzarVariante para pruebas en Editor) y dispara el panel de Victoria al concluir.
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        public enum ForzarVariante
        {
            Auto,
            PescadoSalvo,
            PescadoQuemado
        }

        public static bool IsSequenceRunning { get; private set; }

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");

        [Header("── Modo de Prueba ──")]
        [SerializeField] private ForzarVariante forzarVariante = ForzarVariante.Auto;

        [Header("── Referencias de Escena ──")]
        [SerializeField] private GameObject pinguObject;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private TextMeshProUGUI dialogueText;

        [Header("── Posiciones y Movimiento de Pingu ──")]
        [SerializeField] private Vector3 startPosition = new Vector3(-7.5f, -2.5f, 0f);
        [SerializeField] private Vector3 ovenPosition = new Vector3(-1.5f, -2.5f, 0f);
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private bool faceRightAtOven = true;

        [Header("── Tiempos de la Secuencia ──")]
        [SerializeField] private float fadeInDuration = 0.8f;
        [SerializeField] private float pauseBeforeFish = 0.7f;
        [SerializeField] private float displayDuration = 3.2f;

        [Header("── Pescado en la Parrilla y Efectos ──")]
        [SerializeField] private SpriteRenderer fishRenderer;
        [SerializeField] private Sprite goodFishSprite;
        [SerializeField] private Sprite burntFishSprite;
        [SerializeField] private FrameAnimator effectAnimator;
        [SerializeField] private Sprite[] steamFrames;
        [SerializeField] private Sprite[] smokeFrames;
        [SerializeField] private float effectFps = 6f;

        [Header("── Textos Editables ──")]
        [TextArea(2, 3)]
        [SerializeField] private string goodFishText = "¡Llegaste justo a tiempo! El pescado quedó perfecto.";
        [TextArea(2, 3)]
        [SerializeField] private string burntFishText = "Llegaste a casa... y el pescado ya es carbón.";

        [Header("── Guardado al Finalizar ──")]
        [SerializeField] private bool deleteSaveOnComplete = true;

        private Animator pinguAnimator;
        private SpriteRenderer pinguSpriteRenderer;
        private Rigidbody2D pinguRb;

        private void Awake()
        {
            IsSequenceRunning = true;
        }

        private void OnDestroy()
        {
            IsSequenceRunning = false;
        }

        private void Start()
        {
            SetupPinguForCutscene();

            if (fishRenderer != null) fishRenderer.enabled = false;
            if (effectAnimator != null) effectAnimator.Stop();
            if (dialogueText != null) dialogueText.gameObject.SetActive(false);

            bool isBurnt = ResolveIsFishBurnt();
            StartCoroutine(RunEndingSequence(isBurnt));
        }

        private bool ResolveIsFishBurnt()
        {
            switch (forzarVariante)
            {
                case ForzarVariante.PescadoSalvo:
                    return false;
                case ForzarVariante.PescadoQuemado:
                    return true;
                case ForzarVariante.Auto:
                default:
                    if (GameManager.Instance == null)
                    {
                        Debug.LogWarning("[EndingController] GameManager.Instance no existe. Usando variante PescadoSalvo por defecto.");
                        return false;
                    }
                    return GameManager.Instance.FishBurnt;
            }
        }

        private void SetupPinguForCutscene()
        {
            if (pinguObject == null)
            {
                pinguObject = GameObject.FindWithTag("Player");
            }
            if (pinguObject == null) return;

            var pc = pinguObject.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            var animCtrl = pinguObject.GetComponent<PinguAnimationController>();
            if (animCtrl != null) animCtrl.enabled = false;

            var grapple = pinguObject.GetComponent<GrappleController>();
            if (grapple != null) grapple.enabled = false;

            pinguRb = pinguObject.GetComponent<Rigidbody2D>();
            if (pinguRb != null)
            {
                pinguRb.linearVelocity = Vector2.zero;
                pinguRb.gravityScale = 0f;
                pinguRb.bodyType = RigidbodyType2D.Kinematic;
            }

            pinguAnimator = pinguObject.GetComponent<Animator>();
            pinguSpriteRenderer = pinguObject.GetComponentInChildren<SpriteRenderer>();
            pinguObject.transform.position = startPosition;

            SetPinguAnimParams(0f);
        }

        private void SetPinguAnimParams(float speed)
        {
            if (pinguAnimator == null) return;
            pinguAnimator.SetFloat(SpeedHash, speed);
            pinguAnimator.SetBool(IsGroundedHash, true);
            pinguAnimator.SetFloat(VerticalVelocityHash, 0f);
            pinguAnimator.SetBool(IsDashingHash, false);
        }

        private IEnumerator RunEndingSequence(bool isBurnt)
        {
            // 1. Fundido desde negro + caminata automática hacia el horno
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 1f;
                fadeOverlay.blocksRaycasts = true;
            }

            Coroutine fadeRoutine = null;
            if (fadeOverlay != null && fadeInDuration > 0f)
            {
                fadeRoutine = StartCoroutine(FadeInRoutine());
            }

            if (pinguObject != null)
            {
                float dirX = ovenPosition.x - startPosition.x;
                if (pinguSpriteRenderer != null && Mathf.Abs(dirX) > 0.01f)
                {
                    pinguSpriteRenderer.flipX = dirX < 0f;
                }

                SetPinguAnimParams(1f);
                while (Vector3.Distance(pinguObject.transform.position, ovenPosition) > 0.05f)
                {
                    pinguObject.transform.position = Vector3.MoveTowards(
                        pinguObject.transform.position,
                        ovenPosition,
                        walkSpeed * Time.deltaTime
                    );
                    yield return null;
                }
                pinguObject.transform.position = ovenPosition;
                SetPinguAnimParams(0f);
            }

            if (fadeRoutine != null) yield return fadeRoutine;

            // 2. Pausa breve mirando al horno
            if (pinguSpriteRenderer != null)
            {
                pinguSpriteRenderer.flipX = !faceRightAtOven;
            }
            yield return new WaitForSeconds(pauseBeforeFish);

            // 3. Mostrar pescado y animación (vapor blanco o humo negro) + texto
            if (fishRenderer != null)
            {
                fishRenderer.sprite = isBurnt ? burntFishSprite : goodFishSprite;
                fishRenderer.enabled = fishRenderer.sprite != null;
            }

            if (effectAnimator != null)
            {
                Sprite[] frames = isBurnt ? smokeFrames : steamFrames;
                if (frames != null && frames.Length > 0)
                {
                    effectAnimator.SetFramesAndPlay(frames, effectFps, true);
                }
            }

            if (dialogueText != null)
            {
                dialogueText.text = isBurnt ? burntFishText : goodFishText;
                dialogueText.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(displayDuration);

            // 4. Cierre común: borrar guardado (opcional) y mostrar panel de victoria
            IsSequenceRunning = false;

            if (deleteSaveOnComplete)
            {
                SaveSystem.DeleteSave();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerVictory();
            }
            else if (PauseManager.Instance != null)
            {
                PauseManager.Instance.ShowVictory();
            }
        }

        private IEnumerator FadeInRoutine()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.alpha = Mathf.Clamp01(1f - (elapsed / fadeInDuration));
                yield return null;
            }
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
        }

#if UNITY_EDITOR
        public void Configure(
            GameObject pingu,
            CanvasGroup fade,
            TextMeshProUGUI dialogue,
            SpriteRenderer fishSr,
            Sprite goodFish,
            Sprite burntFish,
            FrameAnimator fxAnim,
            Sprite[] steam,
            Sprite[] smoke)
        {
            pinguObject = pingu;
            fadeOverlay = fade;
            dialogueText = dialogue;
            fishRenderer = fishSr;
            goodFishSprite = goodFish;
            burntFishSprite = burntFish;
            effectAnimator = fxAnim;
            steamFrames = steam;
            smokeFrames = smoke;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
