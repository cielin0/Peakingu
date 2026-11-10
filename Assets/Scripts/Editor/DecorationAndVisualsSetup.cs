using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Peakingu.EditorTools
{
    public static class DecorationAndVisualsSetup
    {
        private const string DECOR_SPRITES_DIR = "Assets/Sprites/Clean/Decor";
        private const string DECOR_TILES_DIR = "Assets/Tilemaps/Tiles/Decor";
        private const string CHECKPOINT_PREFAB_PATH = "Assets/Prefabs/Checkpoint.prefab";
        private const string STALACTITE_PREFAB_PATH = "Assets/Prefabs/Stalactite.prefab";

        private static readonly string[] AllScenes = new string[]
        {
            "Assets/Scenes/Nivel1.unity",
            "Assets/Scenes/Nivel2.unity",
            "Assets/Scenes/Cueva.unity",
            "Assets/Scenes/Nivel4.unity",
            "Assets/Scenes/Nivel5.unity"
        };

        [MenuItem("Tools/Peakingu/23. Configurar Decoracion, Carambanos y Checkpoint")]
        public static void SetupDecorationAndVisuals()
        {
            try
            {
                // 1. Configurar Checkpoint (bandera_apagada / bandera_encendida)
                SetupCheckpointPrefab();

                // 2. Configurar Estalactitas (carambano_01..04)
                SetupStalactitePrefab();

                // 3. Actualizar escenas
                foreach (var scenePath in AllScenes)
                {
                    if (File.Exists(scenePath))
                    {
                        UpdateSceneVisuals(scenePath);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[DecorationAndVisualsSetup] SUCCESS: Decoración en Environment (detrás de Pingu), carámbanos en estalactitas y banderas en Checkpoint configurados exitosamente.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DecorationAndVisualsSetup] Error: {ex}");
            }
        }

        #region 1. Checkpoint
        public static void SetupCheckpointPrefab()
        {
            var flagOff = AssetDatabase.LoadAssetAtPath<Sprite>($"{DECOR_SPRITES_DIR}/bandera_apagada.png");
            var flagOn = AssetDatabase.LoadAssetAtPath<Sprite>($"{DECOR_SPRITES_DIR}/bandera_encendida.png");

            if (flagOff == null || flagOn == null)
            {
                Debug.LogWarning("[DecorationAndVisualsSetup] No se encontraron sprites de bandera.");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(CHECKPOINT_PREFAB_PATH);

            var cp = prefabRoot.GetComponent<Checkpoint>();
            var sr = prefabRoot.GetComponentInChildren<SpriteRenderer>();
            var col = prefabRoot.GetComponent<BoxCollider2D>();

            // Asignar sprites en Checkpoint
            var so = new SerializedObject(cp);
            so.FindProperty("inactiveSprite").objectReferenceValue = flagOff;
            so.FindProperty("activeSprite").objectReferenceValue = flagOn;
            so.FindProperty("inactiveColor").colorValue = Color.white;
            so.FindProperty("activeColor").colorValue = Color.white;
            so.ApplyModifiedProperties();

            // Configurar SpriteRenderer en Sorting Layer Environment (detrás de Characters)
            if (sr != null)
            {
                sr.sprite = flagOff;
                sr.color = Color.white;
                sr.sortingLayerName = "Environment";
                sr.sortingOrder = 2; // Detrás de Pingu (Characters, order 0)
            }

            // Ajustar BoxCollider2D al tamaño del mástil y bandera
            if (col != null)
            {
                col.isTrigger = true;
                col.size = new Vector2(0.8f, 1.2f);
                col.offset = new Vector2(0f, 0.1f);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CHECKPOINT_PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log("[DecorationAndVisualsSetup] Checkpoint.prefab actualizado con banderas.");
        }
        #endregion

        #region 2. Estalactitas
        public static void SetupStalactitePrefab()
        {
            var carambanos = new List<Sprite>();
            for (int i = 1; i <= 4; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{DECOR_SPRITES_DIR}/carambano_{i:D2}.png");
                if (s != null) carambanos.Add(s);
            }

            if (carambanos.Count == 0)
            {
                Debug.LogWarning("[DecorationAndVisualsSetup] No se encontraron sprites de carámbano.");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(STALACTITE_PREFAB_PATH);

            var stalactite = prefabRoot.GetComponent<FallingStalactite>();
            var sr = prefabRoot.GetComponent<SpriteRenderer>();

            var so = new SerializedObject(stalactite);
            var spritesProp = so.FindProperty("carambanoSprites");
            spritesProp.arraySize = carambanos.Count;
            for (int i = 0; i < carambanos.Count; i++)
            {
                spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = carambanos[i];
            }
            so.ApplyModifiedProperties();

            if (sr != null)
            {
                sr.sprite = carambanos[0];
                sr.color = Color.white;
                sr.sortingLayerName = "Environment";
                sr.sortingOrder = 3; // Detrás del jugador
            }

            // Ajustar Collider al carámbano
            var col = prefabRoot.GetComponent<Collider2D>();
            if (col is PolygonCollider2D poly)
            {
                // Reconstruir puntos del polígono con el nuevo sprite
                poly.points = new Vector2[]
                {
                    new Vector2(-0.18f, 0.3f),
                    new Vector2(0.18f, 0.3f),
                    new Vector2(0.04f, -0.3f),
                    new Vector2(-0.04f, -0.3f)
                };
            }
            else if (col is BoxCollider2D box)
            {
                box.size = new Vector2(0.35f, 0.6f);
                box.offset = Vector2.zero;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, STALACTITE_PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log("[DecorationAndVisualsSetup] Stalactite.prefab actualizado con carambano_01..04.");
        }
        #endregion

        #region 3. Actualización de Escenas
        private static void UpdateSceneVisuals(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var flagOff = AssetDatabase.LoadAssetAtPath<Sprite>($"{DECOR_SPRITES_DIR}/bandera_apagada.png");
            var flagOn = AssetDatabase.LoadAssetAtPath<Sprite>($"{DECOR_SPRITES_DIR}/bandera_encendida.png");

            // 1. Actualizar Checkpoints en la escena
            var checkpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
            foreach (var cp in checkpoints)
            {
                var so = new SerializedObject(cp);
                so.FindProperty("inactiveSprite").objectReferenceValue = flagOff;
                so.FindProperty("activeSprite").objectReferenceValue = flagOn;
                so.FindProperty("inactiveColor").colorValue = Color.white;
                so.FindProperty("activeColor").colorValue = Color.white;
                so.ApplyModifiedProperties();

                var sr = cp.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = flagOff;
                    sr.color = Color.white;
                    sr.sortingLayerName = "Environment";
                    sr.sortingOrder = 2; // Detrás de Pingu (Characters)
                }

                var col = cp.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(0.8f, 1.2f);
                    col.offset = new Vector2(0f, 0.1f);
                }
                EditorUtility.SetDirty(cp);
            }

            // 2. Actualizar Estalactitas en la escena
            var stalactites = Object.FindObjectsByType<FallingStalactite>(FindObjectsSortMode.None);
            var carambanos = new List<Sprite>();
            for (int i = 1; i <= 4; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{DECOR_SPRITES_DIR}/carambano_{i:D2}.png");
                if (s != null) carambanos.Add(s);
            }

            for (int i = 0; i < stalactites.Length; i++)
            {
                var st = stalactites[i];
                var sr = st.GetComponent<SpriteRenderer>();
                var spriteToUse = carambanos[i % carambanos.Count];

                var so = new SerializedObject(st);
                var spritesProp = so.FindProperty("carambanoSprites");
                spritesProp.arraySize = carambanos.Count;
                for (int c = 0; c < carambanos.Count; c++)
                {
                    spritesProp.GetArrayElementAtIndex(c).objectReferenceValue = carambanos[c];
                }
                so.ApplyModifiedProperties();

                if (sr != null)
                {
                    sr.sprite = spriteToUse;
                    sr.color = Color.white;
                    sr.sortingLayerName = "Environment";
                    sr.sortingOrder = 3; // Detrás de Pingu
                }
                EditorUtility.SetDirty(st);
            }

            // 3. Pintar elementos de decoración en el Tilemap Decoration (detrás de Pingu)
            var decorObj = GameObject.Find("Grid/Decoration");
            if (decorObj != null)
            {
                var tm = decorObj.GetComponent<Tilemap>();
                var tmRend = decorObj.GetComponent<TilemapRenderer>();
                if (tmRend != null)
                {
                    tmRend.sortingLayerName = "Environment";
                    tmRend.sortingOrder = 1; // Detrás de Characters (Pingu = Characters, order 0)
                }

                if (tm != null && (scenePath.Contains("Nivel1") || scenePath.Contains("Nivel2")))
                {
                    PopulateDecorTiles(tm);
                    EditorUtility.SetDirty(tm);
                    EditorUtility.SetDirty(decorObj);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DecorationAndVisualsSetup] Escena '{Path.GetFileName(scenePath)}' guardada.");
        }

        private static void PopulateDecorTiles(Tilemap tm)
        {
            // Cargar tiles de decoración
            var cartel = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/cartel.asset");
            var huellas = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/huellas.asset");
            var monticulo1 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/monticulo_01.asset");
            var monticulo2 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/monticulo_02.asset");
            var monticulo3 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/monticulo_03.asset");
            var pino1 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/pino_01.asset");
            var pino2 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/pino_02.asset");
            var pino3 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/pino_03.asset");
            var pino4 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/pino_04.asset");
            var pino5 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/pino_05.asset");
            var roca1 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/roca_01.asset");
            var roca2 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/roca_02.asset");
            var roca3 = AssetDatabase.LoadAssetAtPath<Tile>($"{DECOR_TILES_DIR}/roca_03.asset");

            // Cartel cerca del spawn inicial
            if (cartel != null) tm.SetTile(new Vector3Int(-10, -2, 0), cartel);

            // Huellas en la nieve cerca de la salida
            if (huellas != null)
            {
                tm.SetTile(new Vector3Int(-9, -2, 0), huellas);
                tm.SetTile(new Vector3Int(-7, -2, 0), huellas);
                tm.SetTile(new Vector3Int(-1, -2, 0), huellas);
                tm.SetTile(new Vector3Int(1, -2, 0), huellas);
            }

            // Pinos al fondo (Environment, orden 1 - detrás del jugador)
            if (pino1 != null) tm.SetTile(new Vector3Int(-13, -2, 0), pino1);
            if (pino2 != null) tm.SetTile(new Vector3Int(-6, -2, 0), pino2);
            if (pino3 != null) tm.SetTile(new Vector3Int(0, -2, 0), pino3);
            if (pino4 != null) tm.SetTile(new Vector3Int(8, -2, 0), pino4);
            if (pino5 != null) tm.SetTile(new Vector3Int(15, -2, 0), pino5);
            if (pino1 != null) tm.SetTile(new Vector3Int(22, -2, 0), pino1);
            if (pino2 != null) tm.SetTile(new Vector3Int(30, -2, 0), pino2);
            if (pino3 != null) tm.SetTile(new Vector3Int(40, -2, 0), pino3);
            if (pino4 != null) tm.SetTile(new Vector3Int(46, -2, 0), pino4);

            // Rocas decorativas en el suelo y junto a plataformas
            if (roca1 != null) tm.SetTile(new Vector3Int(-11, -2, 0), roca1);
            if (roca2 != null) tm.SetTile(new Vector3Int(-3, -2, 0), roca2);
            if (roca3 != null) tm.SetTile(new Vector3Int(5, -2, 0), roca3);
            if (roca1 != null) tm.SetTile(new Vector3Int(18, -2, 0), roca1);
            if (roca2 != null) tm.SetTile(new Vector3Int(35, -2, 0), roca2);

            // Montículos de nieve en las plataformas
            if (monticulo1 != null) tm.SetTile(new Vector3Int(-4, 1, 0), monticulo1); // Sobre Plat 1
            if (monticulo2 != null) tm.SetTile(new Vector3Int(4, 1, 0), monticulo2);  // Sobre Plat 2
            if (monticulo3 != null) tm.SetTile(new Vector3Int(10, 4, 0), monticulo3); // Sobre Plat 3 (cerca Mercader)

            tm.RefreshAllTiles();
        }
        #endregion
    }
}
