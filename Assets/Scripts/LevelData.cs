using UnityEngine;

namespace Peakingu
{
    public enum LevelDifficulty
    {
        Facil,      // Nivel 1: Tutorial y laderas suaves
        Normal,     // Nivel 2: Plataformas de hielo y saltos medidos
        Dificil,    // Nivel 3: Cueva oscura con estalactitas que caen
        Experto,    // Nivel 4: Viento cortante y secciones verticales con gancho
        Extremo     // Nivel 5: Carrera contrarreloj contra el horno en la cumbre
    }

    /// <summary>
    /// ScriptableObject que define los parámetros de configuración, balance y dificultad de cada nivel:
    /// tiempo límite, velocidad de transcurso del tiempo, recompensa en monedas y transición.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData_New", menuName = "Peakingu/Level Data", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("── Identificación del Nivel ──")]
        [Tooltip("Número de nivel (1 a 5).")]
        [Range(1, 5)]
        public int levelNumber = 1;

        [Tooltip("Nombre temático del nivel.")]
        public string levelName = "Laderas Bajas";

        [Tooltip("Nombre exacto de la escena en Unity asociada a este nivel.")]
        public string sceneName = "Nivel1";

        [TextArea(2, 4)]
        [Tooltip("Descripción temática / consejo para el jugador.")]
        public string description = "Aprende las mecánicas básicas de salto y recolección en la base nevada.";

        [Header("── Parámetros de Tiempo y Dificultad ──")]
        [Tooltip("Tiempo límite en segundos para completar el nivel antes de que el pescado se queme.")]
        public float timeLimit = 240f;

        [Tooltip("Multiplicador de velocidad del tiempo. > 1 hace que el temporizador corra más rápido.")]
        [Range(0.5f, 3.0f)]
        public float timeSpeed = 1.0f;

        [Tooltip("Nivel de dificultad estimado para balance.")]
        public LevelDifficulty difficulty = LevelDifficulty.Facil;

        [Header("── Recompensas y Progresión ──")]
        [Tooltip("Monedas otorgadas inmediatamente al alcanzar la meta de este nivel.")]
        public int coinReward = 15;

        [Tooltip("Referencia al siguiente nivel. Dejar nulo si es el nivel final (Nivel 5).")]
        public LevelData nextLevelData;

        [Tooltip("Nombre de la siguiente escena en caso de no enlazar LevelData.")]
        public string nextSceneName = "Nivel2";

        [Tooltip("Indica si este es el nivel final (Nivel 5) que otorga la pantalla de Victoria del juego.")]
        public bool isFinalLevel = false;

        public bool HasNextLevel => nextLevelData != null || (!string.IsNullOrEmpty(nextSceneName) && !isFinalLevel);
        public string TargetNextScene => nextLevelData != null ? nextLevelData.sceneName : nextSceneName;
    }
}
