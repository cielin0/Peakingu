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

        [Header("Monedas")]
        [Tooltip("Cantidad actual de monedas acumuladas.")]
        [SerializeField] private int currentCoins = 0;

        [Header("Temporizador (Pescado en el horno)")]
        [Tooltip("Tiempo límite inicial en segundos (20 minutos = 1200 s por defecto).")]
        [SerializeField] private float initialTimeInSeconds = 1200f;
        [Tooltip("Indica si el temporizador regresivo se encuentra activo.")]
        [SerializeField] private bool timerIsRunning = true;
        [System.NonSerialized] private float currentTimeRemaining;

        [Header("Checkpoint y Respawn")]
        [Tooltip("Punto inicial de aparición por defecto.")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(-8f, -1f, 0f);
        private Vector3 currentCheckpoint;
        private bool hasCheckpoint;

        // Eventos públicos
        public static event Action<int> OnLivesChanged;
        public static event Action<int> OnCoinsChanged;
        public static event Action<float> OnTimerTick;
        public static event Action OnTimerExpired;
        public static event Action OnGameOver;
        public static event Action<Vector3> OnPlayerRespawned;

        public int CurrentLives => currentLives;
        public int MaxLives => maxLives;
        public int CurrentCoins => currentCoins;
        public float InitialTimeInSeconds => initialTimeInSeconds;
        public float CurrentTimeRemaining => currentTimeRemaining;
        public bool TimerIsRunning => timerIsRunning;
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
            currentTimeRemaining = initialTimeInSeconds;
            currentCheckpoint = spawnPosition;
        }

        private void Start()
        {
            FindPlayerAndRegisterSpawn();
            OnLivesChanged?.Invoke(currentLives);
            OnCoinsChanged?.Invoke(currentCoins);
            OnTimerTick?.Invoke(currentTimeRemaining);
        }

        private void Update()
        {
            UpdateCountdownTimer();
        }

        private void UpdateCountdownTimer()
        {
            if (!timerIsRunning || currentLives <= 0) return;

            currentTimeRemaining -= Time.deltaTime;

            if (currentTimeRemaining <= 0f)
            {
                currentTimeRemaining = 0f;
                timerIsRunning = false;
                OnTimerTick?.Invoke(currentTimeRemaining);
                OnTimerExpired?.Invoke();
                Debug.Log("[GameManager] ¡El tiempo llegó a 00:00! El pescado se ha quemado.");
                TriggerGameOver();
            }
            else
            {
                OnTimerTick?.Invoke(currentTimeRemaining);
            }
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

        #region Sistema de Monedas
        /// <summary>
        /// Agrega monedas a la reserva del jugador y notifica a los observadores (HUD).
        /// </summary>
        public void AddCoins(int amount = 1)
        {
            if (amount <= 0) return;

            currentCoins += amount;
            OnCoinsChanged?.Invoke(currentCoins);
            Debug.Log($"[GameManager] Monedas recolectadas: +{amount}. Total: {currentCoins}");
        }

        /// <summary>
        /// Gasta monedas si el jugador tiene suficiente saldo. Retorna true si tuvo éxito.
        /// </summary>
        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return false;

            if (currentCoins >= amount)
            {
                currentCoins -= amount;
                OnCoinsChanged?.Invoke(currentCoins);
                Debug.Log($"[GameManager] Monedas gastadas: -{amount}. Saldo actual: {currentCoins}");
                return true;
            }

            Debug.LogWarning($"[GameManager] Fondos insuficientes: requiere {amount}, pero tiene {currentCoins}");
            return false;
        }

        public void ResetCoins()
        {
            currentCoins = 0;
            OnCoinsChanged?.Invoke(currentCoins);
        }
        #endregion

        #region Sistema de Temporizador
        public void SetTimerRunning(bool running)
        {
            timerIsRunning = running;
        }

        public void ResetTimer(float? newTimeInSeconds = null)
        {
            currentTimeRemaining = newTimeInSeconds ?? initialTimeInSeconds;
            timerIsRunning = true;
            OnTimerTick?.Invoke(currentTimeRemaining);
        }

        public void AddTime(float seconds)
        {
            currentTimeRemaining = Mathf.Max(0f, currentTimeRemaining + seconds);
            OnTimerTick?.Invoke(currentTimeRemaining);
        }
        #endregion

        #region Vidas y Daño
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
            timerIsRunning = false;
            Debug.Log("[GameManager] GAME OVER: Condición de derrota cumplida.");
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
            ResetCoins();
            ResetTimer();
            RespawnPlayer();
        }
        #endregion
    }
}
