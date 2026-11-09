using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Peakingu
{
    /// <summary>
    /// Meta de Nivel (LevelGoal):
    /// Al entrar en contacto con el jugador Pingu:
    ///  1. Otorga una recompensa en monedas con GameManager.Instance.AddCoins().
    ///  2. Si es el Nivel 5 (nivel final), dispara la pantalla de Victoria del juego.
    ///  3. Si no es el nivel final, autoguarda la partida (SaveSystem) y carga el siguiente nivel.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        [Header("── Configuración del Nivel ──")]
        [Tooltip("ScriptableObject con los datos y balance de este nivel.")]
        [SerializeField] private LevelData levelData;

        [Header("── Valores de Respaldo (si LevelData no está asignado) ──")]
        [SerializeField] private int fallbackCoinReward = 15;
        [SerializeField] private string fallbackNextScene = "Nivel2";
        [SerializeField] private bool fallbackIsFinalLevel = false;

        [Header("── Audio y Efectos Visuales ──")]
        [Tooltip("Efecto de sonido al alcanzar la meta.")]
        [SerializeField] private AudioClip goalReachedSfx;
        [Tooltip("Prefab de partículas o fuegos artificiales al cruzar la meta.")]
        [SerializeField] private GameObject goalParticlesPrefab;
        [Tooltip("Tiempo de espera en segundos antes de cargar la siguiente escena (para ver la animación/sonido).")]
        [SerializeField] private float transitionDelay = 0.8f;

        private bool hasTriggered = false;
        private Collider2D goalCollider;

        public LevelData CurrentLevelData => levelData;

        private void Reset()
        {
            goalCollider = GetComponent<Collider2D>();
            if (goalCollider != null)
            {
                goalCollider.isTrigger = true;
            }
        }

        private void Awake()
        {
            goalCollider = GetComponent<Collider2D>();
            if (goalCollider != null)
            {
                goalCollider.isTrigger = true;
            }

            // Si hay LevelData asignado en la escena, sincronizar los parámetros del nivel con GameManager
            if (levelData != null && GameManager.Instance != null)
            {
                GameManager.Instance.ApplyLevelData(levelData);
            }
        }

        private void Start()
        {
            if (levelData != null && GameManager.Instance != null)
            {
                GameManager.Instance.ApplyLevelData(levelData);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered) return;

            // Detectar jugador
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                hasTriggered = true;
                StartCoroutine(ProcessGoalReachedRoutine(other.gameObject));
            }
        }

        private IEnumerator ProcessGoalReachedRoutine(GameObject playerObj)
        {
            // 1. Bloquear input de movimiento de Pingu para evitar que caiga o se mueva durante la transición
            var pc = playerObj.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.SetMovementLocked(true);
            }

            // 2. Determinar recompensa, siguiente nivel y condición final
            int reward = levelData != null ? levelData.coinReward : fallbackCoinReward;
            bool isFinal = levelData != null ? levelData.isFinalLevel : fallbackIsFinalLevel;
            string nextScene = levelData != null ? levelData.TargetNextScene : fallbackNextScene;
            int levelNum = levelData != null ? levelData.levelNumber : 1;

            Debug.Log($"[LevelGoal] ¡Nivel {levelNum} completado! Recompensa: +{reward} monedas. ¿Es final?: {isFinal}");

            // 3. Otorgar recompensa de monedas
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(reward);
            }

            // 4. Efectos audiovisuales
            if (goalReachedSfx != null)
            {
                AudioSource.PlayClipAtPoint(goalReachedSfx, transform.position);
            }
            else if (AudioManager.Instance != null)
            {
                if (isFinal)
                {
                    // Audio de victoria lo maneja GameManager/AudioManager
                }
                else
                {
                    AudioManager.Instance.PlayPurchaseSuccess();
                }
            }

            if (goalParticlesPrefab != null)
            {
                Instantiate(goalParticlesPrefab, transform.position, Quaternion.identity);
            }

            // 5. Caso Nivel 5: ¡Victoria total del juego!
            if (isFinal)
            {
                Debug.Log("[LevelGoal] ¡JUEGO COMPLETADO! Pingu ha conquistado la cumbre del Nivel 5.");

                // Guardar estado final completado
                SaveSystem.AutoSaveCurrentState("Nivel5");

                // Esperar un instante para feedback y desplegar pantalla de victoria
                yield return new WaitForSeconds(0.4f);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerVictory();
                }
                else if (PauseManager.Instance != null)
                {
                    PauseManager.Instance.ShowVictory();
                }
                yield break;
            }

            // 6. Niveles 1 al 4: Guardar y pasar al siguiente nivel
            Debug.Log($"[LevelGoal] Guardando partida y cargando siguiente nivel: '{nextScene}'...");
            SaveSystem.AutoSaveCurrentState(nextScene);

            // Breve pausa para contemplar la llegada a la meta
            yield return new WaitForSeconds(transitionDelay);

            // Cargar siguiente nivel
            SceneManager.LoadScene(nextScene);
        }

#if UNITY_EDITOR
        public void Configure(LevelData data, AudioClip sfx = null)
        {
            levelData = data;
            goalReachedSfx = sfx;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
