using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Peakingu.EditorTools
{
    public static class TilemapsStructureSetup
    {
        private const string ICE_MATERIAL_PATH = "Assets/Physics/Ice_Material2D.physicsMaterial2D";
        private const string DECOR_SPRITES_DIR = "Assets/Sprites/Clean/Decor";
        private const string DECOR_TILES_DIR = "Assets/Tilemaps/Tiles/Decor";
        private const string PREFABS_DIR = "Assets/Prefabs";

        private static readonly string[] ScenePaths = new string[]
        {
            "Assets/Scenes/Nivel1.unity",
            "Assets/Scenes/Nivel2.unity",
            "Assets/Scenes/Cueva.unity",
            "Assets/Scenes/Nivel4.unity",
            "Assets/Scenes/Nivel5.unity"
        };

        [MenuItem("Tools/Peakingu/21. Configurar Tilemaps Separados (Ground, Ice, Decoration)")]
        public static void SetupTilemapsStructure()
        {
            try
            {
                // 1. Cargar material de física de hielo
                var iceMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ICE_MATERIAL_PATH);

                // 2. Configurar sprites y tiles de decoración (sin colisión)
                SetupDecorSpritesAndTiles();

                // 3. Crear/actualizar prefabs de Tilemaps
                CreateTilemapPrefabs(iceMat);

                // 4. Configurar cada escena con la jerarquía estandarizada
                foreach (var scenePath in ScenePaths)
                {
                    if (File.Exists(scenePath))
                    {
                        ConfigureScene(scenePath, iceMat);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[TilemapsStructureSetup] SUCCESS: Tilemaps separados configurados (Ground con CompositeCollider, Ice con baja fricción y Decoration sin colisión).");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TilemapsStructureSetup] Error: {ex}");
            }
        }

        #region 1. Configuración por Escena
        public static void ConfigureScene(string scenePath, PhysicsMaterial2D iceMat)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);

            // 1. Encontrar o crear Grid principal
            var gridObj = GameObject.Find("Grid");
            if (gridObj == null)
            {
                var anyGrid = Object.FindAnyObjectByType<Grid>();
                if (anyGrid != null)
                {
                    gridObj = anyGrid.gameObject;
                    gridObj.name = "Grid";
                }
                else
                {
                    gridObj = new GameObject("Grid");
                    gridObj.AddComponent<Grid>();
                }
            }
            else
            {
                if (gridObj.GetComponent<Grid>() == null)
                {
                    gridObj.AddComponent<Grid>();
                }
            }

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1) groundLayer = 0;
            int defaultLayer = LayerMask.NameToLayer("Default");
            if (defaultLayer == -1) defaultLayer = 0;

            // ==========================================
            // 2. Tilemap GROUND
            // ==========================================
            GameObject groundObj = FindChildNamed(gridObj, "Ground", "Tilemap_Ground");
            if (groundObj == null)
            {
                // Buscar si existe en la raíz
                groundObj = GameObject.Find("Tilemap_Ground");
                if (groundObj == null) groundObj = GameObject.Find("Ground");

                if (groundObj == null)
                {
                    groundObj = new GameObject("Ground");
                }
            }
            groundObj.name = "Ground";
            groundObj.transform.SetParent(gridObj.transform, false);
            groundObj.tag = "Ground";
            groundObj.layer = groundLayer;

            var groundTm = EnsureComponent<Tilemap>(groundObj);
            var groundRend = EnsureComponent<TilemapRenderer>(groundObj);
            groundRend.sortingLayerName = "Environment";
            groundRend.sortingOrder = 0;

            var groundRb = EnsureComponent<Rigidbody2D>(groundObj);
            groundRb.bodyType = RigidbodyType2D.Static;

            var groundComp = EnsureComponent<CompositeCollider2D>(groundObj);
            groundComp.geometryType = CompositeCollider2D.GeometryType.Polygons;

            var groundTmCol = EnsureComponent<TilemapCollider2D>(groundObj);
            groundTmCol.compositeOperation = Collider2D.CompositeOperation.Merge;

            // ==========================================
            // 3. Tilemap ICE
            // ==========================================
            GameObject iceObj = FindChildNamed(gridObj, "Ice", "Tilemap_Ice", "Tilemap_IceGround");
            if (iceObj == null)
            {
                iceObj = GameObject.Find("Tilemap_Ice");
                if (iceObj == null) iceObj = GameObject.Find("Tilemap_IceGround");
                if (iceObj == null) iceObj = GameObject.Find("Ice");

                if (iceObj == null)
                {
                    iceObj = new GameObject("Ice");
                }
            }
            iceObj.name = "Ice";
            iceObj.transform.SetParent(gridObj.transform, false);
            iceObj.tag = "Ground";
            iceObj.layer = groundLayer;

            var iceTm = EnsureComponent<Tilemap>(iceObj);
            var iceRend = EnsureComponent<TilemapRenderer>(iceObj);
            iceRend.sortingLayerName = "Environment";
            iceRend.sortingOrder = 0;

            var iceRb = EnsureComponent<Rigidbody2D>(iceObj);
            iceRb.bodyType = RigidbodyType2D.Static;
            if (iceMat != null) iceRb.sharedMaterial = iceMat;

            var iceComp = EnsureComponent<CompositeCollider2D>(iceObj);
            iceComp.geometryType = CompositeCollider2D.GeometryType.Polygons;
            if (iceMat != null) iceComp.sharedMaterial = iceMat;

            var iceTmCol = EnsureComponent<TilemapCollider2D>(iceObj);
            iceTmCol.compositeOperation = Collider2D.CompositeOperation.Merge;
            if (iceMat != null) iceTmCol.sharedMaterial = iceMat;

            // ==========================================
            // 4. Tilemap DECORATION (Sin colisión)
            // ==========================================
            GameObject decorObj = FindChildNamed(gridObj, "Decoration", "Tilemap_Decoration", "Decor");
            if (decorObj == null)
            {
                decorObj = GameObject.Find("Tilemap_Decoration");
                if (decorObj == null) decorObj = GameObject.Find("Decoration");

                if (decorObj == null)
                {
                    decorObj = new GameObject("Decoration");
                }
            }
            decorObj.name = "Decoration";
            decorObj.transform.SetParent(gridObj.transform, false);
            decorObj.tag = "Untagged";
            decorObj.layer = defaultLayer;

            var decorTm = EnsureComponent<Tilemap>(decorObj);
            var decorRend = EnsureComponent<TilemapRenderer>(decorObj);
            decorRend.sortingLayerName = "Environment";
            decorRend.sortingOrder = 1; // Encima de los bloques de suelo para que se vea el decorado

            // REQUISITO ESTRICTO: SIN COLISIÓN
            RemoveComponentIfExists<Collider2D>(decorObj);
            RemoveComponentIfExists<Rigidbody2D>(decorObj);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[TilemapsStructureSetup] Escena '{Path.GetFileName(scenePath)}' configurada con Ground, Ice y Decoration.");
        }
        #endregion

        #region 2. Prefabs Estandarizados
        private static void CreateTilemapPrefabs(PhysicsMaterial2D iceMat)
        {
            if (!Directory.Exists(PREFABS_DIR))
            {
                Directory.CreateDirectory(PREFABS_DIR);
            }

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1) groundLayer = 0;

            // 1. Prefab Ground
            GameObject gObj = new GameObject("Ground");
            gObj.tag = "Ground";
            gObj.layer = groundLayer;
            gObj.AddComponent<Tilemap>();
            var gRend = gObj.AddComponent<TilemapRenderer>();
            gRend.sortingLayerName = "Environment";
            gRend.sortingOrder = 0;
            var gRb = gObj.AddComponent<Rigidbody2D>();
            gRb.bodyType = RigidbodyType2D.Static;
            var gComp = gObj.AddComponent<CompositeCollider2D>();
            gComp.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var gTmCol = gObj.AddComponent<TilemapCollider2D>();
            gTmCol.compositeOperation = Collider2D.CompositeOperation.Merge;
            PrefabUtility.SaveAsPrefabAsset(gObj, $"{PREFABS_DIR}/Tilemap_Ground.prefab");
            Object.DestroyImmediate(gObj);

            // 2. Prefab Ice
            GameObject iObj = new GameObject("Ice");
            iObj.tag = "Ground";
            iObj.layer = groundLayer;
            iObj.AddComponent<Tilemap>();
            var iRend = iObj.AddComponent<TilemapRenderer>();
            iRend.sortingLayerName = "Environment";
            iRend.sortingOrder = 0;
            var iRb = iObj.AddComponent<Rigidbody2D>();
            iRb.bodyType = RigidbodyType2D.Static;
            if (iceMat != null) iRb.sharedMaterial = iceMat;
            var iComp = iObj.AddComponent<CompositeCollider2D>();
            iComp.geometryType = CompositeCollider2D.GeometryType.Polygons;
            if (iceMat != null) iComp.sharedMaterial = iceMat;
            var iTmCol = iObj.AddComponent<TilemapCollider2D>();
            iTmCol.compositeOperation = Collider2D.CompositeOperation.Merge;
            if (iceMat != null) iTmCol.sharedMaterial = iceMat;
            PrefabUtility.SaveAsPrefabAsset(iObj, $"{PREFABS_DIR}/Tilemap_Ice.prefab");
            Object.DestroyImmediate(iObj);

            // 3. Prefab Decoration (Sin colisión)
            GameObject dObj = new GameObject("Decoration");
            dObj.layer = LayerMask.NameToLayer("Default");
            dObj.AddComponent<Tilemap>();
            var dRend = dObj.AddComponent<TilemapRenderer>();
            dRend.sortingLayerName = "Environment";
            dRend.sortingOrder = 1;
            PrefabUtility.SaveAsPrefabAsset(dObj, $"{PREFABS_DIR}/Tilemap_Decoration.prefab");
            Object.DestroyImmediate(dObj);

            // 4. Prefab Grid con los 3 Tilemaps hijos
            GameObject gridRoot = new GameObject("Grid_Environment");
            gridRoot.AddComponent<Grid>();

            var gChild = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}/Tilemap_Ground.prefab")) as GameObject;
            gChild.transform.SetParent(gridRoot.transform, false);

            var iChild = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}/Tilemap_Ice.prefab")) as GameObject;
            iChild.transform.SetParent(gridRoot.transform, false);

            var dChild = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}/Tilemap_Decoration.prefab")) as GameObject;
            dChild.transform.SetParent(gridRoot.transform, false);

            PrefabUtility.SaveAsPrefabAsset(gridRoot, $"{PREFABS_DIR}/Grid_Environment.prefab");
            Object.DestroyImmediate(gridRoot);

            Debug.Log($"[TilemapsStructureSetup] Prefabs creados en '{PREFABS_DIR}'.");
        }
        #endregion

        #region 3. Sprites y Tiles de Decoración
        private static void SetupDecorSpritesAndTiles()
        {
            if (!Directory.Exists(DECOR_SPRITES_DIR)) return;
            if (!Directory.Exists(DECOR_TILES_DIR))
            {
                Directory.CreateDirectory(DECOR_TILES_DIR);
            }

            var decorFiles = Directory.GetFiles(DECOR_SPRITES_DIR, "*.png");
            foreach (var file in decorFiles)
            {
                string assetPath = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 128;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    string tileName = Path.GetFileNameWithoutExtension(assetPath);
                    string tileAssetPath = $"{DECOR_TILES_DIR}/{tileName}.asset";
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
                    if (tile == null)
                    {
                        tile = ScriptableObject.CreateInstance<Tile>();
                        AssetDatabase.CreateAsset(tile, tileAssetPath);
                    }

                    tile.sprite = sprite;
                    tile.color = Color.white;
                    // REQUISITO ESTRICTO: Sin colisión
                    tile.colliderType = Tile.ColliderType.None;

                    EditorUtility.SetDirty(tile);
                }
            }
        }
        #endregion

        #region Helpers
        private static GameObject FindChildNamed(GameObject parent, params string[] names)
        {
            foreach (var n in names)
            {
                var t = parent.transform.Find(n);
                if (t != null) return t.gameObject;
            }
            return null;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null)
            {
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        private static void RemoveComponentIfExists<T>(GameObject go) where T : Component
        {
            var comps = go.GetComponents<T>();
            foreach (var c in comps)
            {
                Object.DestroyImmediate(c);
            }
        }
        #endregion
    }
}
