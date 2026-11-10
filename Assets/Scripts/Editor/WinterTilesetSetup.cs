using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

namespace Peakingu.EditorTools
{
    public static class WinterTilesetSetup
    {
        private const string PIXEL_SPRITES_DIR = "Assets/Sprites/Tiles";
        private const string CLEAN_SPRITES_DIR = "Assets/Sprites/Clean/Tiles";
        private const string TILES_DIR = "Assets/Tilemaps/Tiles";
        private const string PALETTES_DIR = "Assets/Palettes";
        private const string WINTER_RULE_TILE_PATH = "Assets/Tilemaps/Winter_RuleTile.asset";
        private const string SUELO_RULE_TILE_PATH = "Assets/Tilemaps/Suelo_RuleTile.asset";

        // 9 nombres estándar
        private static readonly string[] StandardTileNames = new string[]
        {
            "esquina_sup_izq",
            "borde_sup",
            "esquina_sup_der",
            "borde_izq",
            "relleno",
            "borde_der",
            "esquina_inf_izq",
            "borde_inf",
            "esquina_inf_der"
        };

        // 9 nombres con prefijo suelo_ (Clean)
        private static readonly string[] SueloTileNames = new string[]
        {
            "suelo_esq_sup_izq",
            "suelo_borde_sup",
            "suelo_esq_sup_der",
            "suelo_borde_izq",
            "suelo_relleno",
            "suelo_borde_der",
            "suelo_esq_inf_izq",
            "suelo_borde_inf",
            "suelo_esq_inf_der"
        };

        [MenuItem("Tools/Peakingu/19. Configurar Rule Tile y Tile Palette de Invierno (9 Tiles, Sprite Collider)")]
        public static void SetupWinterTileset()
        {
            try
            {
                // 1. Configurar TextureImporters de los sprites (Clean PPU 128 y Pixel PPU 16)
                SetupSpriteImporters();

                // 2. Crear los 9 Tiles individuales estándar y los 9 tiles suelo_* con ColliderType.Sprite
                var pixelTiles = CreateIndividualTiles(PIXEL_SPRITES_DIR, StandardTileNames, "");
                var cleanTiles = CreateIndividualTiles(CLEAN_SPRITES_DIR, SueloTileNames, "");

                // 3. Crear los RuleTiles (Winter_RuleTile y Suelo_RuleTile) con ColliderType.Sprite
                var winterRuleTile = CreateRuleTile(WINTER_RULE_TILE_PATH, pixelTiles);
                var sueloRuleTile = CreateRuleTile(SUELO_RULE_TILE_PATH, cleanTiles);

                // 4. Crear la Tile Palette (.prefab) con los tiles individuales y RuleTiles
                CreateTilePalette(cleanTiles, sueloRuleTile, pixelTiles, winterRuleTile);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[WinterTilesetSetup] SUCCESS: Tiles individuales (ColliderType.Sprite), RuleTiles para plataformas de 2 filas y Tile Palette configurados exitosamente.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WinterTilesetSetup] Error al configurar tileset: {ex}");
            }
        }

        #region 1. Importación de Sprites
        private static void SetupSpriteImporters()
        {
            // Clean Sprites (128x128, PPU 128)
            foreach (var name in SueloTileNames)
            {
                string path = $"{CLEAN_SPRITES_DIR}/{name}.png";
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

            // Pixel Art Sprites (16x16, PPU 16)
            foreach (var name in StandardTileNames)
            {
                string path = $"{PIXEL_SPRITES_DIR}/{name}.png";
                if (!File.Exists(path)) continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 16;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }

            string sheetPath = $"{PIXEL_SPRITES_DIR}/Winter_Tileset_3x3.png";
            if (File.Exists(sheetPath))
            {
                var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 16;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }
        #endregion

        #region 2. Crear Tiles Individuales
        private static Tile[] CreateIndividualTiles(string spriteDir, string[] names, string prefix)
        {
            if (!Directory.Exists(TILES_DIR))
            {
                Directory.CreateDirectory(TILES_DIR);
            }

            Tile[] tiles = new Tile[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                string spritePath = $"{spriteDir}/{name}.png";
                string tileAssetPath = $"{TILES_DIR}/{prefix}{name}.asset";

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[WinterTilesetSetup] No se encontró sprite en '{spritePath}'.");
                    continue;
                }

                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tileAssetPath);
                }

                tile.sprite = sprite;
                tile.color = Color.white;

                // REQUISITO ESTRICTO: Collider Type = Sprite, NO Grid
                tile.colliderType = Tile.ColliderType.Sprite;

                EditorUtility.SetDirty(tile);
                tiles[i] = tile;

                Debug.Log($"[WinterTilesetSetup] Tile creado: '{tileAssetPath}' con ColliderType={tile.colliderType}");
            }

            AssetDatabase.SaveAssets();
            return tiles;
        }
        #endregion

        #region 3. Crear Rule Tile
        private static RuleTile CreateRuleTile(string assetPath, Tile[] tiles)
        {
            // Mapeo por índice:
            // 0: esquina_sup_izq / suelo_esq_sup_izq
            // 1: borde_sup       / suelo_borde_sup
            // 2: esquina_sup_der / suelo_esq_sup_der
            // 3: borde_izq       / suelo_borde_izq
            // 4: relleno         / suelo_relleno
            // 5: borde_der       / suelo_borde_der
            // 6: esquina_inf_izq / suelo_esq_inf_izq
            // 7: borde_inf       / suelo_borde_inf
            // 8: esquina_inf_der / suelo_esq_inf_der

            var ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(assetPath);
            if (ruleTile == null)
            {
                ruleTile = ScriptableObject.CreateInstance<RuleTile>();
                AssetDatabase.CreateAsset(ruleTile, assetPath);
            }

            // Sprite por defecto: Relleno (índice 4)
            ruleTile.m_DefaultSprite = tiles[4] != null ? tiles[4].sprite : null;

            // REQUISITO ESTRICTO: Collider Type del Rule Tile = Sprite, NO Grid
            ruleTile.m_DefaultColliderType = Tile.ColliderType.Sprite;

            ruleTile.m_TilingRules.Clear();

            // En RuleTile, los 8 vecinos de m_Neighbors corresponden a m_NeighborPositions:
            // Index 0: (-1, 1)  -> Top-Left
            // Index 1: (0, 1)   -> Top
            // Index 2: (1, 1)   -> Top-Right
            // Index 3: (-1, 0)  -> Left
            // Index 4: (1, 0)   -> Right
            // Index 5: (-1, -1) -> Bottom-Left
            // Index 6: (0, -1)  -> Bottom
            // Index 7: (1, -1)  -> Bottom-Right
            //
            // Valores:
            // 0 = Ignore (No importa)
            // 1 = This (Debe tener tile adyacente del mismo tipo)
            // 2 = NotThis (Debe estar VACÍO / sin tile)

            // REGLA 1: esquina_sup_izq (Arriba vacío, Izquierda vacío, Derecha con tile, Abajo con tile)
            AddTilingRule(ruleTile, tiles[0] != null ? tiles[0].sprite : null, new int[]
            {
                0, 2, 0,
                2,    1,
                0, 1, 0
            });

            // REGLA 2: esquina_sup_der (Arriba vacío, Derecha vacío, Izquierda con tile, Abajo con tile)
            AddTilingRule(ruleTile, tiles[2] != null ? tiles[2].sprite : null, new int[]
            {
                0, 2, 0,
                1,    2,
                0, 1, 0
            });

            // REGLA 3: borde_sup (Arriba vacío, Izquierda con tile, Derecha con tile, Abajo con tile)
            // NOTA: Funciona perfectamente en plataformas de 2 filas de grosor porque abajo tiene tile y arriba aire
            AddTilingRule(ruleTile, tiles[1] != null ? tiles[1].sprite : null, new int[]
            {
                0, 2, 0,
                1,    1,
                0, 1, 0
            });

            // REGLA 4: esquina_inf_izq (Abajo vacío, Izquierda vacío, Derecha con tile, Arriba con tile)
            AddTilingRule(ruleTile, tiles[6] != null ? tiles[6].sprite : null, new int[]
            {
                0, 1, 0,
                2,    1,
                0, 2, 0
            });

            // REGLA 5: esquina_inf_der (Abajo vacío, Derecha vacío, Izquierda con tile, Arriba con tile)
            AddTilingRule(ruleTile, tiles[8] != null ? tiles[8].sprite : null, new int[]
            {
                0, 1, 0,
                1,    2,
                0, 2, 0
            });

            // REGLA 6: borde_inf (Abajo vacío, Izquierda con tile, Derecha con tile, Arriba con tile)
            // NOTA: En la fila inferior de una plataforma de 2 filas de grosor, arriba tiene tile y abajo aire
            AddTilingRule(ruleTile, tiles[7] != null ? tiles[7].sprite : null, new int[]
            {
                0, 1, 0,
                1,    1,
                0, 2, 0
            });

            // REGLA 7: borde_izq (Izquierda vacío, Derecha con tile, Arriba con tile, Abajo con tile)
            // Solo aparece en masas de 3 o más filas de espesor
            AddTilingRule(ruleTile, tiles[3] != null ? tiles[3].sprite : null, new int[]
            {
                0, 1, 0,
                2,    1,
                0, 1, 0
            });

            // REGLA 8: borde_der (Derecha vacío, Izquierda con tile, Arriba con tile, Abajo con tile)
            // Solo aparece en masas de 3 o más filas de espesor
            AddTilingRule(ruleTile, tiles[5] != null ? tiles[5].sprite : null, new int[]
            {
                0, 1, 0,
                1,    2,
                0, 1, 0
            });

            // REGLA 9: relleno (Centro interior rodeado por tiles en las 8 direcciones)
            // Solo se utiliza en masas grandes de 3x3 o más
            AddTilingRule(ruleTile, tiles[4] != null ? tiles[4].sprite : null, new int[]
            {
                1, 1, 1,
                1,    1,
                1, 1, 1
            });

            EditorUtility.SetDirty(ruleTile);
            AssetDatabase.SaveAssets();

            Debug.Log($"[WinterTilesetSetup] RuleTile guardado en '{assetPath}' con {ruleTile.m_TilingRules.Count} reglas y ColliderType={ruleTile.m_DefaultColliderType}.");
            return ruleTile;
        }

        private static void AddTilingRule(RuleTile ruleTile, Sprite sprite, int[] neighbors)
        {
            if (sprite == null) return;

            var rule = new RuleTile.TilingRule();
            rule.m_ColliderType = Tile.ColliderType.Sprite; // Sprite, NO Grid!
            rule.m_Sprites = new Sprite[] { sprite };
            rule.m_Neighbors.Clear();

            for (int i = 0; i < 8; i++)
            {
                rule.m_Neighbors.Add(neighbors[i]);
            }

            ruleTile.m_TilingRules.Add(rule);
        }
        #endregion

        #region 4. Crear Tile Palette
        private static void CreateTilePalette(Tile[] cleanTiles, RuleTile cleanRuleTile, Tile[] pixelTiles, RuleTile pixelRuleTile)
        {
            if (!Directory.Exists(PALETTES_DIR))
            {
                Directory.CreateDirectory(PALETTES_DIR);
            }

            string palettePrefabPath = $"{PALETTES_DIR}/Winter_TilePalette.prefab";

            // Crear GameObject Grid para la paleta
            GameObject gridObj = new GameObject("Winter_TilePalette");
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            // Subobjeto con Tilemap
            GameObject layerObj = new GameObject("Layer1");
            layerObj.transform.SetParent(gridObj.transform, false);

            var tilemap = layerObj.AddComponent<Tilemap>();
            layerObj.AddComponent<TilemapRenderer>();

            // 1) Pintar tiles Clean (suelo_*) en cuadrícula 3x3 en x = 0..2
            if (cleanTiles != null && cleanTiles.Length == 9)
            {
                tilemap.SetTile(new Vector3Int(0, 2, 0), cleanTiles[0]); // suelo_esq_sup_izq
                tilemap.SetTile(new Vector3Int(1, 2, 0), cleanTiles[1]); // suelo_borde_sup
                tilemap.SetTile(new Vector3Int(2, 2, 0), cleanTiles[2]); // suelo_esq_sup_der

                tilemap.SetTile(new Vector3Int(0, 1, 0), cleanTiles[3]); // suelo_borde_izq
                tilemap.SetTile(new Vector3Int(1, 1, 0), cleanTiles[4]); // suelo_relleno
                tilemap.SetTile(new Vector3Int(2, 1, 0), cleanTiles[5]); // suelo_borde_der

                tilemap.SetTile(new Vector3Int(0, 0, 0), cleanTiles[6]); // suelo_esq_inf_izq
                tilemap.SetTile(new Vector3Int(1, 0, 0), cleanTiles[7]); // suelo_borde_inf
                tilemap.SetTile(new Vector3Int(2, 0, 0), cleanTiles[8]); // suelo_esq_inf_der
            }

            // Suelo Rule Tile en x=3, y=1
            if (cleanRuleTile != null)
            {
                tilemap.SetTile(new Vector3Int(3, 1, 0), cleanRuleTile);
            }

            // 2) Pintar tiles Pixel Art en cuadrícula 3x3 en x = 5..7
            if (pixelTiles != null && pixelTiles.Length == 9)
            {
                tilemap.SetTile(new Vector3Int(5, 2, 0), pixelTiles[0]); // esquina_sup_izq
                tilemap.SetTile(new Vector3Int(6, 2, 0), pixelTiles[1]); // borde_sup
                tilemap.SetTile(new Vector3Int(7, 2, 0), pixelTiles[2]); // esquina_sup_der

                tilemap.SetTile(new Vector3Int(5, 1, 0), pixelTiles[3]); // borde_izq
                tilemap.SetTile(new Vector3Int(6, 1, 0), pixelTiles[4]); // relleno
                tilemap.SetTile(new Vector3Int(7, 1, 0), pixelTiles[5]); // borde_der

                tilemap.SetTile(new Vector3Int(5, 0, 0), pixelTiles[6]); // esquina_inf_izq
                tilemap.SetTile(new Vector3Int(6, 0, 0), pixelTiles[7]); // borde_inf
                tilemap.SetTile(new Vector3Int(7, 0, 0), pixelTiles[8]); // esquina_inf_der
            }

            // Pixel Rule Tile en x=8, y=1
            if (pixelRuleTile != null)
            {
                tilemap.SetTile(new Vector3Int(8, 1, 0), pixelRuleTile);
            }

            // Guardar Prefab de la paleta
            PrefabUtility.SaveAsPrefabAsset(gridObj, palettePrefabPath);
            Object.DestroyImmediate(gridObj);

            Debug.Log($"[WinterTilesetSetup] Tile Palette guardada en '{palettePrefabPath}' lista para abrir en Window > 2D > Tile Palette.");
        }
        #endregion
    }
}

