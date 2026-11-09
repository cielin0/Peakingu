using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Peakingu.EditorTools
{
    public static class AudioSetup
    {
        [MenuItem("Tools/Peakingu/16. Configurar AudioManager y Asignar Clips")]
        public static void SetupAudio()
        {
            try
            {
                AssetDatabase.Refresh();

                // 1. Cargar todos los clips generados
                var musicMenu = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music_Menu.wav");
                var musicNivel1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music_Nivel1.wav");
                var musicCueva = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music_Cueva.wav");
                var musicGameOver = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_GameOver.wav");
                var musicVictory = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Victory.wav");

                var sfxJump = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Jump.wav");
                var sfxLand = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Land.wav");
                var sfxCoin = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Coin.wav");
                var sfxButtonClick = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_ButtonClick.wav");
                var sfxPurchaseSuccess = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_PurchaseSuccess.wav");
                var sfxPurchaseFail = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_PurchaseFail.wav");
                var sfxDash = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Dash.wav");
                var sfxGrappleLaunch = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_GrappleLaunch.wav");
                var sfxGrappleRelease = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_GrappleRelease.wav");

                var step1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Step_1.wav");
                var step2 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Step_2.wav");
                AudioClip[] footsteps = new AudioClip[] { step1, step2 };

                // 2. Crear GameObject temporal y configurarlo
                GameObject tempGo = new GameObject("AudioManager");
                var audioMgr = tempGo.AddComponent<AudioManager>();
                audioMgr.ConfigureClips(
                    musicMenu, musicNivel1, musicCueva, musicGameOver, musicVictory,
                    sfxJump, sfxLand, footsteps, sfxDash,
                    sfxGrappleLaunch, sfxGrappleRelease, sfxCoin,
                    sfxPurchaseSuccess, sfxPurchaseFail, sfxButtonClick
                );

                // 3. Guardar como Prefab en Assets/Prefabs y Assets/Resources (para lazy-loading)
                if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
                if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");

                string prefabPath = "Assets/Prefabs/AudioManager.prefab";
                string resPrefabPath = "Assets/Resources/AudioManager.prefab";

                var prefab = PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
                PrefabUtility.SaveAsPrefabAsset(tempGo, resPrefabPath);
                Object.DestroyImmediate(tempGo);

                Debug.Log($"[AudioSetup] Prefab AudioManager creado en {prefabPath} y {resPrefabPath}");

                // 4. Asegurar presencia de AudioManager en las escenas principales
                EnsureAudioManagerInScene("Assets/Scenes/Menu.unity", prefab);
                EnsureAudioManagerInScene("Assets/Scenes/Nivel1.unity", prefab);
                EnsureAudioManagerInScene("Assets/Scenes/Cueva.unity", prefab);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[AudioSetup] SUCCESS: AudioManager configurado con música y todos los SFX conectados.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AudioSetup] Error al configurar AudioManager: {ex}");
            }
        }

        private static void EnsureAudioManagerInScene(string scenePath, GameObject prefab)
        {
            if (!File.Exists(scenePath)) return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var existing = Object.FindAnyObjectByType<AudioManager>();
            if (existing == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "AudioManager";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[AudioSetup] AudioManager instanciado en escena: {scenePath}");
            }
            else
            {
                // Si existe pero es antiguo, sincronizar prefab si aplica
                Debug.Log($"[AudioSetup] AudioManager ya presente en escena: {scenePath}");
            }
        }
    }
}
