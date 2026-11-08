using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Peakingu
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Vidas")]
        [Tooltip("Cantidad máxima e inicial de vidas.")]
        [SerializeField] private int maxLives = 5;
        [Tooltip("Cantidad actual de vidas.")]
        [SerializeField] private int currentLives = 5;

        [Header("Checkpoint y Respawn")]
        [Tooltip("Punto inicial de aparición por defecto.")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(-8f, -1f, 0f);
        private Vector3 currentCheckpoint;
        private bool hasCheckpoint;

        // Eventos públicos
        public static event Action<int> OnLivesChanged;
        public static event Action OnGameOver;
        public static event Action<Vector3> OnPlayerRespawned;

        public int CurrentLives => currentLives;
        public int MaxLives => maxLives;
        public Vector3 CurrentCheckpoint => hasCheckpoint ? currentCheckpoint : spawnPosition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            currentLives = maxLives;
            currentCheckpoint = spawnPosition;
        }

        private void Start()
        {
            FindPlayerAndRegisterSpawn();
            OnLivesChanged?.Invoke(currentLives);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindPlayerAndRegisterSpawn();
        }

        private void FindPlayerAndRegisterSpawn()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null && !hasCheckpoint)
            {
                spawnPosition = player.transform.position;
                currentCheckpoint = spawnPosition;
            }
        }

        public void SetCheckpoint(Vector3 newCheckpoint)
        {
            currentCheckpoint = newCheckpoint;
            hasCheckpoint = true;
            Debug.Log($"[GameManager] Checkpoint alcanzado en: {newCheckpoint}");
        }

        /// <summary>
        /// Método público llamado por obstáculos, enemigos y KillZone para infligir daño.
        /// </summary>
        public void TakeDamage(int damage = 1)
        {
            if (currentLives <= 0) return;

            currentLives = Mathf.Max(0, currentLives - damage);
            OnLivesChanged?.Invoke(currentLives);
            Debug.Log($"[GameManager] Pingu recibió daño. Vidas restantes: {currentLives}");

            if (currentLives <= 0)
            {
                TriggerGameOver();
            }
            else
            {
                RespawnPlayer();
            }
        }

        public void RespawnPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Vector3 targetPos = CurrentCheckpoint;
                var controller = player.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.Respawn(targetPos);
                }
                else
                {
                    var rb = player.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                    player.transform.position = targetPos;
                }

                OnPlayerRespawned?.Invoke(targetPos);
            }
        }

        private void TriggerGameOver()
        {
            Debug.Log("[GameManager] GAME OVER: Las vidas llegaron a 0.");
            OnGameOver?.Invoke();
        }

        public void ResetLives()
        {
            currentLives = maxLives;
            OnLivesChanged?.Invoke(currentLives);
        }

        public void ResetGame()
        {
            hasCheckpoint = false;
            ResetLives();
            RespawnPlayer();
        }
    }
}
