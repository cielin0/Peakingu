using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Peakingu
{
    /// <summary>
    /// Sistema de guardado y carga en formato JSON compatible tanto con Windows Standalone como con WebGL (usando PlayerPrefs).
    /// </summary>
    public static class SaveSystem
    {
        [System.Serializable]
        public class SaveData
        {
            public int currentCoins = 0;
            public int currentLives = 5;
            public string currentLevel = "Nivel1";
            public float timeRemaining = 1200f;

            // Mejoras permanentes de PlayerStats
            public bool hasGrapplingHook = false;
            public bool hasJetBoots = false;
            public bool hasAntiSlipGloves = false;
            public bool hasHydrodash = false;

            // Metadatos
            public long saveTimestamp = 0;
        }

        private const string PREFS_KEY = "Peakingu_SaveData_JSON";
        private const string SAVE_FILE_NAME = "peakingu_save.json";

        // Estado pendiente de carga al navegar entre escenas
        private static SaveData pendingLoadData = null;

        public static bool HasPendingLoad => pendingLoadData != null;

        public static void SetPendingLoad(SaveData data)
        {
            pendingLoadData = data;
        }

        public static SaveData ConsumePendingLoad()
        {
            var data = pendingLoadData;
            pendingLoadData = null;
            return data;
        }

        public static void ClearPendingLoad()
        {
            pendingLoadData = null;
        }

        private static string GetSaveFilePath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }

        /// <summary>
        /// Guarda el estado en JSON. En WebGL usa PlayerPrefs, y en Windows escribe a archivo y PlayerPrefs como respaldo.
        /// </summary>
        public static bool SaveGame(SaveData data)
        {
            if (data == null) return false;

            try
            {
                data.saveTimestamp = DateTime.UtcNow.Ticks;
                string json = JsonUtility.ToJson(data, true);

                // 1. Guardar siempre en PlayerPrefs (Garantiza persistencia en WebGL y navegadores)
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();

                // 2. En Windows / Standalone / Editor, guardar también como archivo físico en persistentDataPath
#if !UNITY_WEBGL || UNITY_EDITOR
                try
                {
                    string filePath = GetSaveFilePath();
                    string dir = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    File.WriteAllText(filePath, json);
                    Debug.Log($"[SaveSystem] Partida guardada exitosamente en archivo: {filePath}");
                }
                catch (Exception fileEx)
                {
                    Debug.LogWarning($"[SaveSystem] No se pudo escribir archivo en disco ({fileEx.Message}), respaldo en PlayerPrefs activo.");
                }
#endif

                Debug.Log($"[SaveSystem] Partida guardada (JSON): Nivel={data.currentLevel}, Monedas={data.currentCoins}, Vidas={data.currentLives}, Mejoras(Gancho={data.hasGrapplingHook}, Boots={data.hasJetBoots}, Guantes={data.hasAntiSlipGloves}, Dash={data.hasHydrodash})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Error crítico al serializar y guardar partida: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Carga los datos de partida desde el archivo físico o PlayerPrefs (WebGL).
        /// </summary>
        public static SaveData LoadGame()
        {
            string json = null;

            // 1. Intentar leer desde archivo físico en plataformas de escritorio
#if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                string filePath = GetSaveFilePath();
                if (File.Exists(filePath))
                {
                    json = File.ReadAllText(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Lectura de archivo físico falló: {ex.Message}. Intentando desde PlayerPrefs...");
            }
#endif

            // 2. Si no se obtuvo del archivo o estamos en WebGL, leer de PlayerPrefs
            if (string.IsNullOrEmpty(json) && PlayerPrefs.HasKey(PREFS_KEY))
            {
                json = PlayerPrefs.GetString(PREFS_KEY, "");
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SaveSystem] No se encontraron datos de guardado válidos.");
                return null;
            }

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveSystem] Partida cargada exitosamente: Nivel={data.currentLevel}, Monedas={data.currentCoins}, Vidas={data.currentLives}");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Error al deserializar JSON de guardado: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Comprueba si existe alguna partida guardada en disco o en PlayerPrefs.
        /// </summary>
        public static bool HasSaveData()
        {
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                string json = PlayerPrefs.GetString(PREFS_KEY, "");
                if (!string.IsNullOrEmpty(json)) return true;
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                string filePath = GetSaveFilePath();
                if (File.Exists(filePath)) return true;
            }
            catch
            {
                // Ignorar error de acceso a disco en verificación
            }
#endif

            return false;
        }

        /// <summary>
        /// Elimina la partida guardada actual.
        /// </summary>
        public static void DeleteSave()
        {
            PlayerPrefs.DeleteKey(PREFS_KEY);
            PlayerPrefs.Save();

#if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                string filePath = GetSaveFilePath();
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Error borrando archivo de guardado: {ex.Message}");
            }
#endif
            ClearPendingLoad();
            Debug.Log("[SaveSystem] Partida guardada eliminada.");
        }

        /// <summary>
        /// Captura automáticamente el estado actual del juego (GameManager + PlayerStats + Nivel) y lo guarda en JSON.
        /// </summary>
        public static SaveData AutoSaveCurrentState(string nextLevel = null)
        {
            var data = new SaveData();

            if (GameManager.Instance != null)
            {
                data.currentCoins = GameManager.Instance.CurrentCoins;
                data.currentLives = GameManager.Instance.CurrentLives;
                data.timeRemaining = GameManager.Instance.CurrentTimeRemaining;
            }

            if (PlayerStats.Instance != null)
            {
                data.hasGrapplingHook = PlayerStats.Instance.hasGrapplingHook;
                data.hasJetBoots = PlayerStats.Instance.hasJetBoots;
                data.hasAntiSlipGloves = PlayerStats.Instance.hasAntiSlipGloves;
                data.hasHydrodash = PlayerStats.Instance.hasHydrodash;
            }

            string activeScene = SceneManager.GetActiveScene().name;
            data.currentLevel = !string.IsNullOrEmpty(nextLevel) ? nextLevel : activeScene;

            SaveGame(data);
            Debug.Log($"[SaveSystem] ¡Autoguardado al terminar nivel completado! Nivel guardado: '{data.currentLevel}'");
            return data;
        }
    }
}
