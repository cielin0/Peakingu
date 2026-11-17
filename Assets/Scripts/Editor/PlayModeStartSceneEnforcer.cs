using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Peakingu.EditorTools
{
    /// <summary>
    /// Garantiza que al presionar 'Play' en el Unity Editor, el juego siempre inicie
    /// en la escena del Menú Principal (Assets/Scenes/Menu.unity), sin importar
    /// qué nivel esté abierto en el editor.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartSceneEnforcer
    {
        private const string MENU_SCENE_PATH = "Assets/Scenes/Menu.unity";
        private const string PREF_KEY = "Peakingu_AlwaysPlayFromMenu";

        static PlayModeStartSceneEnforcer()
        {
            ApplyPlayModeStartScene();
        }

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PREF_KEY, true);
            set
            {
                EditorPrefs.SetBool(PREF_KEY, value);
                ApplyPlayModeStartScene();
            }
        }

        public static void ApplyPlayModeStartScene()
        {
            if (IsEnabled)
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MENU_SCENE_PATH);
                if (sceneAsset != null)
                {
                    EditorSceneManager.playModeStartScene = sceneAsset;
                }
                else
                {
                    Debug.LogWarning($"[PlayModeEnforcer] No se encontró la escena en: {MENU_SCENE_PATH}");
                }
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }

        [MenuItem("Tools/Peakingu/Forzar Inicio en Menu Principal al dar Play")]
        public static void ToggleAlwaysPlayFromMenu()
        {
            IsEnabled = !IsEnabled;
            if (IsEnabled)
            {
                Debug.Log("[PlayModeEnforcer] ACTIVADO: Al presionar Play, el juego siempre iniciará en el Menú Principal.");
            }
            else
            {
                Debug.Log("[PlayModeEnforcer] DESACTIVADO: Al presionar Play, se reproducirá la escena actualmente abierta.");
            }
        }

        [MenuItem("Tools/Peakingu/Forzar Inicio en Menu Principal al dar Play", true)]
        public static bool ValidateToggleAlwaysPlayFromMenu()
        {
            Menu.SetChecked("Tools/Peakingu/Forzar Inicio en Menu Principal al dar Play", IsEnabled);
            return true;
        }

        [MenuItem("Tools/Peakingu/Abrir Escena del Menu Principal %#m")] // Shortcut Ctrl+Shift+M
        public static void OpenMenuScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(MENU_SCENE_PATH, OpenSceneMode.Single);
                Debug.Log($"[PlayModeEnforcer] Escena {MENU_SCENE_PATH} abierta en el Editor.");
            }
        }
    }
}
