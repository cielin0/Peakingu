using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Peakingu.EditorTools
{
    public static class IceTilemapSetup
    {
        private const string HIELO_SPRITES_DIR = "Assets/Sprites/Clean/Tiles";
        private const string TILES_DIR = "Assets/Tilemaps/Tiles";
        private const string ICE_RANDOM_TILE_PATH = "Assets/Tilemaps/Ice_RandomTile.asset";
        private const string ICE_TILE_PATH = "Assets/Tilemaps/IceTile.asset";
        private const string ICE_MATERIAL_PATH = "Assets/Physics/Ice_Material2D.physicsMaterial2D";
        private const string PALETTE_PREFAB_PATH = "Assets/Palettes/Winter_TilePalette.prefab";
        private const string ICE_PREFAB_PATH = "Assets/Prefabs/Tilemap_Ice.prefab";
        private const string CUEVA_SCENE_PATH = "Assets/Scenes/Cueva.unity";

        [MenuItem("Tools/Peakingu/20. Configurar Tilemap Ice con Random Tile y Physics Material")]
        public static void SetupIceTilemap()
        {
            try
            {
                // 1. Configurar TextureImporters de hielo_01..12
                SetupHieloSpriteImporters();

                // 2. Cargar sprites de hielo
                var sprites = LoadHieloSprites();
                if (sprites == null || sprites.Length == 0)
                {
                    Debug.LogError("[IceTilemapSetup] No se pudieron cargar los sprites de hielo.");
                    return;
                }

                // 3. Crear tiles individuales de hielo_01..12
                var individualTiles = CreateIndividualHieloTiles(sprites);

                // 4. Crear/Actualizar el Random Tile de hielo
                var randomTile = CreateIceRandomTile(sprites);

                // 5. Cargar Physics Material 2D de hielo existente
                var iceMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ICE_MATERIAL_PATH);
                if (iceMat == null)
                {
                    Debug.LogWarning($"[IceTilemapSetup] No se encontró material en '{ICE_MATERIAL_PATH}'.");
                }

                // 6. Crear/Actualizar Prefab de Tilemap Ice
                CreateIceTilemapPrefab(iceMat, randomTile);

                // 7. Actualizar la Tile Palette con el Random Tile y los tiles de hielo
                UpdateTilePalette(randomTile, individualTiles);

                // 8. Actualizar la escena Cueva
                UpdateCuevaScene(randomTile, iceMat);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[IceTilemapSetup] SUCCESS: Tilemap Ice configurado con Random Tile (hielo_01..12) y Physics Material 2D.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[IceTilemapSetup] Error durante la configuración: {ex}");
            }
        }

        private static void SetupHieloSpriteImporters()
        {
            for (int i = 1; i <= 12; i++)
            {
                string path = $"{HIELO_SPRITES_DIR}/hielo_{i:D2}.png";
                if (!File.Exists(path)) continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 128;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }

        private static Sprite[] LoadHieloSprites()
        {
            var list = new List<Sprite>();
            for (int i = 1; i <= 12; i++)
            {
                string path = $"{HIELO_SPRITES_DIR}/hielo_{i:D2}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    list.Add(sprite);
                }
                else
                {
                    Debug.LogWarning($"[IceTilemapSetup] No se encontró sprite en '{path}'.");
                }
            }
            return list.ToArray();
        }

        private static Tile[] CreateIndividualHieloTiles(Sprite[] sprites)
        {
            if (!Directory.Exists(TILES_DIR))
            {
                Directory.CreateDirectory(TILES_DIR);
            }

            var tiles = new Tile[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                int num = i + 1;
                string assetPath = $"{TILES_DIR}/hielo_{num:D2}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, assetPath);
                }

                tile.sprite = sprites[i];
                tile.color = Color.white;
                tile.colliderType = Tile.ColliderType.Sprite; // Sprite, NO Grid

                EditorUtility.SetDirty(tile);
                tiles[i] = tile;
            }
            return tiles;
        }

        private static RandomTile CreateIceRandomTile(Sprite[] sprites)
        {
            var randomTile = AssetDatabase.LoadAssetAtPath<RandomTile>(ICE_RANDOM_TILE_PATH);
            if (randomTile == null)
            {
                randomTile = ScriptableObject.CreateInstance<RandomTile>();
                AssetDatabase.CreateAsset(randomTile, ICE_RANDOM_TILE_PATH);
            }

            randomTile.m_Sprites = sprites;
            randomTile.color = Color.white;
            randomTile.colliderType = Tile.ColliderType.Sprite; // Sprite, NO Grid
            if (sprites.Length > 0)
            {
                randomTile.sprite = sprites[0];
            }

            EditorUtility.SetDirty(randomTile);

            // También actualizamos o creamos IceTile.asset como RandomTile para máxima compatibilidad
            var legacyTile = AssetDatabase.LoadAssetAtPath<TileBase>(ICE_TILE_PATH);
            if (legacyTile == null || !(legacyTile is RandomTile))
            {
                // Reemplazamos IceTile.asset con una instancia de RandomTile
                AssetDatabase.DeleteAsset(ICE_TILE_PATH);
                var newIceTile = ScriptableObject.CreateInstance<RandomTile>();
                newIceTile.m_Sprites = sprites;
                newIceTile.color = Color.white;
                newIceTile.colliderType = Tile.ColliderType.Sprite;
                if (sprites.Length > 0) newIceTile.sprite = sprites[0];
                AssetDatabase.CreateAsset(newIceTile, ICE_TILE_PATH);
                EditorUtility.SetDirty(newIceTile);
            }
            else if (legacyTile is RandomTile rt)
            {
                rt.m_Sprites = sprites;
                rt.color = Color.white;
                rt.colliderType = Tile.ColliderType.Sprite;
                if (sprites.Length > 0) rt.sprite = sprites[0];
                EditorUtility.SetDirty(rt);
            }

            return randomTile;
        }

        private static void CreateIceTilemapPrefab(PhysicsMaterial2D iceMat, RandomTile randomTile)
        {
            if (!Directory.Exists("Assets/Prefabs"))
            {
                Directory.CreateDirectory("Assets/Prefabs");
            }

            GameObject go = new GameObject("Tilemap_Ice");
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1)
            {
                go.layer = groundLayer;
            }

            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            if (iceMat != null)
            {
                composite.sharedMaterial = iceMat;
            }

            var tmCollider = go.AddComponent<TilemapCollider2D>();
            tmCollider.usedByComposite = true;
            if (iceMat != null)
            {
                tmCollider.sharedMaterial = iceMat;
            }

            PrefabUtility.SaveAsPrefabAsset(go, ICE_PREFAB_PATH);
            Object.DestroyImmediate(go);
            Debug.Log($"[IceTilemapSetup] Prefab creado en '{ICE_PREFAB_PATH}'.");
        }

        private static void UpdateTilePalette(RandomTile randomTile, Tile[] individualTiles)
        {
            if (!File.Exists(PALETTE_PREFAB_PATH))
            {
                Debug.LogWarning($"[IceTilemapSetup] No se encontró paleta en '{PALETTE_PREFAB_PATH}'.");
                return;
            }

            GameObject paletteRoot = PrefabUtility.LoadPrefabContents(PALETTE_PREFAB_PATH);
            var tilemap = paletteRoot.GetComponentInChildren<Tilemap>();
            if (tilemap != null)
            {
                // Colocar Ice_RandomTile en x = 10, y = 1
                if (randomTile != null)
                {
                    tilemap.SetTile(new Vector3Int(10, 1, 0), randomTile);
                }

                // Colocar los 12 tiles individuales en un bloque de 4 columnas x 3 filas (x = 12..15, y = 2..0)
                if (individualTiles != null && individualTiles.Length == 12)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        int col = i % 4;
                        int row = 2 - (i / 4);
                        tilemap.SetTile(new Vector3Int(12 + col, row, 0), individualTiles[i]);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(paletteRoot, PALETTE_PREFAB_PATH);
                Debug.Log($"[IceTilemapSetup] Tile Palette actualizada con Random Tile y tiles de hielo.");
            }
            PrefabUtility.UnloadPrefabContents(paletteRoot);
        }

        private static void UpdateCuevaScene(RandomTile randomTile, PhysicsMaterial2D iceMat)
        {
            if (!File.Exists(CUEVA_SCENE_PATH))
            {
                Debug.LogWarning($"[IceTilemapSetup] No se encontró escena en '{CUEVA_SCENE_PATH}'.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(CUEVA_SCENE_PATH);

            // Buscar Tilemap_IceGround o Tilemap_Ice o Ice
            GameObject iceGo = GameObject.Find("Tilemap_IceGround");
            if (iceGo == null) iceGo = GameObject.Find("Tilemap_Ice");
            if (iceGo == null) iceGo = GameObject.Find("Ice");

            if (iceGo != null)
            {
                iceGo.name = "Tilemap_Ice";

                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer != -1)
                {
                    iceGo.layer = groundLayer;
                }

                var tilemap = iceGo.GetComponent<Tilemap>();
                if (tilemap != null && randomTile != null)
                {
                    // Reemplazar los tiles de la cueva por el RandomTile
                    var bounds = tilemap.cellBounds;
                    int replacedCount = 0;
                    foreach (var pos in bounds.allPositionsWithin)
                    {
                        if (tilemap.HasTile(pos))
                        {
                            tilemap.SetTile(pos, randomTile);
                            replacedCount++;
                        }
                    }
                    Debug.Log($"[IceTilemapSetup] {replacedCount} tiles en Cueva actualizados a RandomTile.");
                }

                // Asegurar Physics Material 2D de baja fricción
                var tmCol = iceGo.GetComponent<TilemapCollider2D>();
                if (tmCol != null && iceMat != null)
                {
                    tmCol.sharedMaterial = iceMat;
                }

                var compCol = iceGo.GetComponent<CompositeCollider2D>();
                if (compCol != null && iceMat != null)
                {
                    compCol.sharedMaterial = iceMat;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[IceTilemapSetup] Escena Cueva guardada con éxito.");
            }
            else
            {
                Debug.LogWarning("[IceTilemapSetup] No se encontró GameObject de Tilemap de hielo en Cueva.");
            }
        }
    }
}
