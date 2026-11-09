using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Peakingu.EditorTools
{
    public static class ProgressionSetup
    {
        private const string LEVELS_DIR = "Assets/Resources/Levels";
        private const string SPRITE_PATH = "Assets/Sprites/LevelGoal_Flag.png";
        private const string PREFAB_PATH = "Assets/Prefabs/LevelGoal.prefab";

        [MenuItem("Tools/Peakingu/18. Configurar Progresion 5 Niveles (LevelData, LevelGoal, Build Settings)")]
        public static void SetupProgression()
        {
            try
            {
                // 1. Configurar Sprite del banderín/meta
                SetupGoalSprite();

                // 2. Crear los 5 ScriptableObjects de LevelData
                var levelDataList = CreateLevelDataAssets();

                // 3. Crear Prefab de LevelGoal
                var goalPrefab = CreateGoalPrefab();

                // 4. Asegurar escenas Nivel1, Nivel2, Cueva, Nivel4, Nivel5
                SetupAllLevelScenes(levelDataList, goalPrefab);

                // 5. Configurar EditorBuildSettings
                SetupBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[ProgressionSetup] SUCCESS: Progresión de 5 niveles, ScriptableObjects LevelData y metas LevelGoal configurados exitosamente.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ProgressionSetup] Error al configurar progresión: {ex}");
            }
        }

        #region 1. Sprite
        private static void SetupGoalSprite()
        {
            if (!File.Exists(SPRITE_PATH)) return;

            var importer = AssetImporter.GetAtPath(SPRITE_PATH) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.spritePivot = new Vector2(0.3f, 0.05f); // Base del mástil
                importer.SaveAndReimport();
            }
        }
        #endregion

        #region 2. LevelData ScriptableObjects
        public static List<LevelData> CreateLevelDataAssets()
        {
            if (!Directory.Exists(LEVELS_DIR))
            {
                Directory.CreateDirectory(LEVELS_DIR);
            }

            var configs = new (int num, string name, string scene, float time, float speed, LevelDifficulty diff, int reward, string desc, bool isFinal)[]
            {
                (1, "Laderas Bajas",    "Nivel1", 240f, 1.00f, LevelDifficulty.Facil,   15, "Aprende el movimiento básico, salto y recolección de monedas en la ladera nevada.", false),
                (2, "Ruta Helada",      "Nivel2", 210f, 1.10f, LevelDifficulty.Normal,  20, "El suelo es más resbaladizo. Plataformas flotantes y desniveles mayores hacia la cueva.", false),
                (3, "Cueva Glaciar",    "Cueva",  180f, 1.25f, LevelDifficulty.Dificil, 25, "Oscuridad subterránea, hielo pulido y estalactitas que se desprenden al pasar.", false),
                (4, "Pico Escarpado",   "Nivel4", 150f, 1.40f, LevelDifficulty.Experto, 35, "Grietas profundas y ascenso vertical donde el Gancho Cyborg es clave para avanzar.", false),
                (5, "Cima Peakingu",    "Nivel5", 120f, 1.60f, LevelDifficulty.Extremo, 50, "¡La cumbre final! Carrera contrarreloj para salvar el pescado antes de que se queme en el horno.", true),
            };

            List<LevelData> list = new List<LevelData>();

            for (int i = 0; i < configs.Length; i++)
            {
                var c = configs[i];
                string assetPath = $"{LEVELS_DIR}/Level_{c.num}.asset";

                LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<LevelData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                data.levelNumber = c.num;
                data.levelName = c.name;
                data.sceneName = c.scene;
                data.timeLimit = c.time;
                data.timeSpeed = c.speed;
                data.difficulty = c.diff;
                data.coinReward = c.reward;
                data.description = c.desc;
                data.isFinalLevel = c.isFinal;
                data.nextSceneName = (i < configs.Length - 1) ? configs[i + 1].scene : "";

                EditorUtility.SetDirty(data);
                list.Add(data);
            }

            // Enlazar referencias de nextLevelData
            for (int i = 0; i < list.Count - 1; i++)
            {
                list[i].nextLevelData = list[i + 1];
                EditorUtility.SetDirty(list[i]);
            }
            list[list.Count - 1].nextLevelData = null;
            EditorUtility.SetDirty(list[list.Count - 1]);

            AssetDatabase.SaveAssets();
            Debug.Log($"[ProgressionSetup] 5 ScriptableObjects de LevelData creados y enlazados en {LEVELS_DIR}.");
            return list;
        }
        #endregion

        #region 3. Prefab de LevelGoal
        private static GameObject CreateGoalPrefab()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);

            GameObject obj = new GameObject("LevelGoal");
            obj.tag = "Untagged";

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Environment";
            sr.sortingOrder = 5;

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.5f, 2.5f);
            col.offset = new Vector2(0.5f, 1.25f);

            obj.AddComponent<LevelGoal>();

            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(obj, PREFAB_PATH);
            Object.DestroyImmediate(obj);

            Debug.Log($"[ProgressionSetup] Prefab LevelGoal guardado en {PREFAB_PATH}.");
            return prefab;
        }
        #endregion

        #region 4. Configurar Escenas y Metas
        private static void SetupAllLevelScenes(List<LevelData> levelDataList, GameObject goalPrefab)
        {
            string scenesDir = "Assets/Scenes";

            // 1. Nivel1 (Nivel 1)
            SetupGoalInScene($"{scenesDir}/Nivel1.unity", levelDataList[0], goalPrefab, new Vector3(38f, 2f, 0f));

            // 2. Nivel2 (Nivel 2) - Si no existe, duplicar Nivel1
            EnsureSceneExists($"{scenesDir}/Nivel1.unity", $"{scenesDir}/Nivel2.unity");
            SetupGoalInScene($"{scenesDir}/Nivel2.unity", levelDataList[1], goalPrefab, new Vector3(45f, 3f, 0f));

            // 3. Cueva (Nivel 3)
            SetupGoalInScene($"{scenesDir}/Cueva.unity", levelDataList[2], goalPrefab, new Vector3(44f, -0.5f, 0f));

            // 4. Nivel4 (Nivel 4) - Si no existe, duplicar Nivel1
            EnsureSceneExists($"{scenesDir}/Nivel1.unity", $"{scenesDir}/Nivel4.unity");
            SetupGoalInScene($"{scenesDir}/Nivel4.unity", levelDataList[3], goalPrefab, new Vector3(48f, 5f, 0f));

            // 5. Nivel5 (Nivel 5) - Si no existe, duplicar Nivel1
            EnsureSceneExists($"{scenesDir}/Nivel1.unity", $"{scenesDir}/Nivel5.unity");
            SetupGoalInScene($"{scenesDir}/Nivel5.unity", levelDataList[4], goalPrefab, new Vector3(50f, 6f, 0f));
        }

        private static void EnsureSceneExists(string sourcePath, string destPath)
        {
            if (File.Exists(destPath)) return;

            if (File.Exists(sourcePath))
            {
                AssetDatabase.CopyAsset(sourcePath, destPath);
                Debug.Log($"[ProgressionSetup] Escena creada duplicando base: {destPath}");
            }
        }

        private static void SetupGoalInScene(string scenePath, LevelData data, GameObject goalPrefab, Vector3 defaultPos)
        {
            if (!File.Exists(scenePath)) return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Eliminar VictoryTrigger antiguo si existe
            var oldVictory = Object.FindAnyObjectByType<VictoryTrigger>();
            if (oldVictory != null)
            {
                defaultPos = oldVictory.transform.position;
                Object.DestroyImmediate(oldVictory.gameObject);
            }

            var existingGoal = Object.FindAnyObjectByType<LevelGoal>();
            if (existingGoal == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab);
                instance.name = $"LevelGoal_{data.levelName.Replace(" ", "")}";
                instance.transform.position = defaultPos;
                var lg = instance.GetComponent<LevelGoal>();
                if (lg != null)
                {
                    lg.Configure(data);
                }
            }
            else
            {
                existingGoal.Configure(data);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ProgressionSetup] LevelGoal configurado para Nivel {data.levelNumber} en '{scenePath}'.");
        }
        #endregion

        #region 5. Build Settings
        public static void SetupBuildSettings()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/Menu.unity",
                "Assets/Scenes/Nivel1.unity",
                "Assets/Scenes/Nivel2.unity",
                "Assets/Scenes/Cueva.unity",
                "Assets/Scenes/Nivel4.unity",
                "Assets/Scenes/Nivel5.unity"
            };

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            foreach (var path in scenePaths)
            {
                if (File.Exists(path))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[ProgressionSetup] EditorBuildSettings actualizado con {buildScenes.Count} escenas en orden de progresión.");
        }
        #endregion
    }
}
