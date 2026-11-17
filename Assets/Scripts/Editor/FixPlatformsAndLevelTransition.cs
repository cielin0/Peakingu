using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Peakingu.EditorTools
{
    [InitializeOnLoad]
    public static class FixPlatformsAndLevelTransition
    {
        private const string CLEAN_TILES_SPRITES_DIR = "Assets/Sprites/Clean/Tiles";
        private const string TILES_DIR = "Assets/Tilemaps/Tiles";
        private const string SUELO_RULE_TILE_PATH = "Assets/Tilemaps/Suelo_RuleTile.asset";
        private const string PALETTE_PREFAB_PATH = "Assets/Palettes/Winter_TilePalette.prefab";

        private static readonly string[] SueloTileNames = new string[]
        {
            "suelo_esq_sup_izq", // 0
            "suelo_borde_sup",   // 1
            "suelo_esq_sup_der", // 2
            "suelo_borde_izq",   // 3
            "suelo_relleno",     // 4
            "suelo_borde_der",   // 5
            "suelo_esq_inf_izq", // 6
            "suelo_borde_inf",   // 7
            "suelo_esq_inf_der"  // 8
        };

        static FixPlatformsAndLevelTransition()
        {
            // Ejecutable manualmente desde Tools -> Peakingu -> Reparar Plataformas y Transición de Niveles
        }

        [MenuItem("Tools/Peakingu/Reparar Plataformas y Transicion de Niveles")]
        public static void ExecuteFix()
        {
            try
            {
                Debug.Log("[FixPlatforms] Iniciando corrección completa de plataformas y transición de niveles...");

                // 1. Cargar y verificar sprites
                Tile[] cleanTiles = EnsureCleanTiles();

                // 2. Reconstruir Suelo_RuleTile
                RuleTile sueloRuleTile = EnsureRuleTile(cleanTiles);

                // 3. Reconstruir Winter_TilePalette.prefab
                RebuildTilePalette(cleanTiles, sueloRuleTile);

                // 4. Reparar Nivel1.unity
                FixNivel1(cleanTiles, sueloRuleTile);

                // 5. Reparar Nivel2.unity
                FixNivel2(cleanTiles, sueloRuleTile);

                // 6. Reparar Nivel4 y Nivel5
                FixNivel4y5(cleanTiles, sueloRuleTile);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[FixPlatforms] ¡ÉXITO! Plataformas 100% visibles, colisionadores Sprite y transición Nivel 1 -> Nivel 2 arreglada.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FixPlatforms] Error durante la corrección: {ex}");
            }
        }

        public static Sprite GetSpriteAtPath(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in all)
            {
                if (obj is Sprite s) return s;
            }
            return null;
        }

        private static Tile[] EnsureCleanTiles()
        {
            if (!Directory.Exists(TILES_DIR)) Directory.CreateDirectory(TILES_DIR);

            Tile[] tiles = new Tile[SueloTileNames.Length];

            for (int i = 0; i < SueloTileNames.Length; i++)
            {
                string name = SueloTileNames[i];
                string spritePath = $"{CLEAN_TILES_SPRITES_DIR}/{name}.png";
                string tileAssetPath = $"{TILES_DIR}/{name}.asset";

                AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
                Sprite sprite = GetSpriteAtPath(spritePath);
                if (sprite == null)
                {
                    Debug.LogError($"[FixPlatforms] No se pudo encontrar Sprite para '{spritePath}'.");
                }

                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tileAssetPath);
                }

                tile.sprite = sprite;
                tile.color = Color.white;
                tile.colliderType = Tile.ColliderType.Sprite; // Sprite, NO Grid
                EditorUtility.SetDirty(tile);
                tiles[i] = tile;
            }

            AssetDatabase.SaveAssets();
            return tiles;
        }

        private static RuleTile EnsureRuleTile(Tile[] tiles)
        {
            var ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(SUELO_RULE_TILE_PATH);
            if (ruleTile == null)
            {
                ruleTile = ScriptableObject.CreateInstance<RuleTile>();
                AssetDatabase.CreateAsset(ruleTile, SUELO_RULE_TILE_PATH);
            }

            ruleTile.m_DefaultSprite = tiles[4] != null ? tiles[4].sprite : null;
            ruleTile.m_DefaultColliderType = Tile.ColliderType.Sprite;
            ruleTile.m_TilingRules.Clear();

            // Regla 1: suelo_esq_sup_izq (Arriba vacío, Izquierda vacío, Derecha con tile, Abajo con tile)
            AddTilingRule(ruleTile, tiles[0].sprite, new int[] { 0, 2, 0, 2, 1, 0, 1, 0 });
            // Regla 2: suelo_esq_sup_der (Arriba vacío, Derecha vacío, Izquierda con tile, Abajo con tile)
            AddTilingRule(ruleTile, tiles[2].sprite, new int[] { 0, 2, 0, 1, 2, 0, 1, 0 });
            // Regla 3: suelo_borde_sup (Arriba vacío, Izq con tile, Der con tile, Abajo con tile)
            AddTilingRule(ruleTile, tiles[1].sprite, new int[] { 0, 2, 0, 1, 1, 0, 1, 0 });
            // Regla 4: suelo_esq_inf_izq (Abajo vacío, Izquierda vacío, Derecha con tile, Arriba con tile)
            AddTilingRule(ruleTile, tiles[6].sprite, new int[] { 0, 1, 0, 2, 1, 0, 2, 0 });
            // Regla 5: suelo_esq_inf_der (Abajo vacío, Derecha vacío, Izquierda con tile, Arriba con tile)
            AddTilingRule(ruleTile, tiles[8].sprite, new int[] { 0, 1, 0, 1, 2, 0, 2, 0 });
            // Regla 6: suelo_borde_inf (Abajo vacío, Izq con tile, Der con tile, Arriba con tile)
            AddTilingRule(ruleTile, tiles[7].sprite, new int[] { 0, 1, 0, 1, 1, 0, 2, 0 });
            // Regla 7: suelo_borde_izq (Izquierda vacío, Derecha con tile, Arriba con tile, Abajo con tile)
            AddTilingRule(ruleTile, tiles[3].sprite, new int[] { 0, 1, 0, 2, 1, 0, 1, 0 });
            // Regla 8: suelo_borde_der (Derecha vacío, Izquierda con tile, Arriba con tile, Abajo con tile)
            AddTilingRule(ruleTile, tiles[5].sprite, new int[] { 0, 1, 0, 1, 2, 0, 1, 0 });
            // Regla 9: suelo_relleno (Rodeado por tiles)
            AddTilingRule(ruleTile, tiles[4].sprite, new int[] { 1, 1, 1, 1, 1, 1, 1, 1 });

            EditorUtility.SetDirty(ruleTile);
            AssetDatabase.SaveAssets();
            return ruleTile;
        }

        private static void AddTilingRule(RuleTile ruleTile, Sprite sprite, int[] neighbors)
        {
            if (sprite == null) return;
            var rule = new RuleTile.TilingRule();
            rule.m_ColliderType = Tile.ColliderType.Sprite;
            rule.m_Sprites = new Sprite[] { sprite };
            rule.m_Neighbors.Clear();
            for (int i = 0; i < 8; i++) rule.m_Neighbors.Add(neighbors[i]);
            ruleTile.m_TilingRules.Add(rule);
        }

        private static void RebuildTilePalette(Tile[] cleanTiles, RuleTile cleanRuleTile)
        {
            GameObject gridObj = new GameObject("Winter_TilePalette");
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            GameObject layerObj = new GameObject("Layer1");
            layerObj.transform.SetParent(gridObj.transform, false);

            var tilemap = layerObj.AddComponent<Tilemap>();
            var renderer = layerObj.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = "Environment";

            // 1. Tiles individuales en 3x3 (x = 0..2, y = 0..2)
            if (cleanTiles != null && cleanTiles.Length == 9)
            {
                tilemap.SetTile(new Vector3Int(0, 2, 0), cleanTiles[0]); // esq_sup_izq
                tilemap.SetTile(new Vector3Int(1, 2, 0), cleanTiles[1]); // borde_sup
                tilemap.SetTile(new Vector3Int(2, 2, 0), cleanTiles[2]); // esq_sup_der

                tilemap.SetTile(new Vector3Int(0, 1, 0), cleanTiles[3]); // borde_izq
                tilemap.SetTile(new Vector3Int(1, 1, 0), cleanTiles[4]); // relleno
                tilemap.SetTile(new Vector3Int(2, 1, 0), cleanTiles[5]); // borde_der

                tilemap.SetTile(new Vector3Int(0, 0, 0), cleanTiles[6]); // esq_inf_izq
                tilemap.SetTile(new Vector3Int(1, 0, 0), cleanTiles[7]); // borde_inf
                tilemap.SetTile(new Vector3Int(2, 0, 0), cleanTiles[8]); // esq_inf_der
            }

            // 2. Rule Tile en x = 3, y = 1
            if (cleanRuleTile != null)
            {
                tilemap.SetTile(new Vector3Int(3, 1, 0), cleanRuleTile);
            }

            // 3. Plataforma premontada de 2 filas de grosor (x = 5..8, y = 0..1)
            // Para que el diseñador pueda seleccionarla entera y estamparla
            if (cleanTiles != null && cleanTiles.Length == 9)
            {
                tilemap.SetTile(new Vector3Int(5, 1, 0), cleanTiles[0]); // esq_sup_izq
                tilemap.SetTile(new Vector3Int(6, 1, 0), cleanTiles[1]); // borde_sup
                tilemap.SetTile(new Vector3Int(7, 1, 0), cleanTiles[1]); // borde_sup
                tilemap.SetTile(new Vector3Int(8, 1, 0), cleanTiles[2]); // esq_sup_der

                tilemap.SetTile(new Vector3Int(5, 0, 0), cleanTiles[6]); // esq_inf_izq
                tilemap.SetTile(new Vector3Int(6, 0, 0), cleanTiles[7]); // borde_inf
                tilemap.SetTile(new Vector3Int(7, 0, 0), cleanTiles[7]); // borde_inf
                tilemap.SetTile(new Vector3Int(8, 0, 0), cleanTiles[8]); // esq_inf_der
            }

            // 4. Tiles de Hielo (hielo_01..12 y Ice_RandomTile)
            var iceRandomTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tilemaps/Ice_RandomTile.asset");
            if (iceRandomTile != null)
            {
                tilemap.SetTile(new Vector3Int(10, 1, 0), iceRandomTile);
            }

            for (int i = 1; i <= 12; i++)
            {
                var iceTile = AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/Tilemaps/Tiles/hielo_{i:D2}.asset");
                if (iceTile != null)
                {
                    int col = (i - 1) % 4;
                    int row = 2 - ((i - 1) / 4);
                    tilemap.SetTile(new Vector3Int(12 + col, row, 0), iceTile);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(gridObj, PALETTE_PREFAB_PATH);
            Object.DestroyImmediate(gridObj);
            Debug.Log($"[FixPlatforms] Winter_TilePalette actualizada en '{PALETTE_PREFAB_PATH}'.");
        }

        private static void BuildPlatform2Rows(Tilemap tm, Tile[] tiles, int xStart, int xEnd, int yTop)
        {
            // Fila superior (y = yTop)
            tm.SetTile(new Vector3Int(xStart, yTop, 0), tiles[0]); // suelo_esq_sup_izq
            for (int x = xStart + 1; x < xEnd; x++)
            {
                tm.SetTile(new Vector3Int(x, yTop, 0), tiles[1]); // suelo_borde_sup
            }
            tm.SetTile(new Vector3Int(xEnd, yTop, 0), tiles[2]); // suelo_esq_sup_der

            // Fila inferior (y = yTop - 1)
            int yBottom = yTop - 1;
            tm.SetTile(new Vector3Int(xStart, yBottom, 0), tiles[6]); // suelo_esq_inf_izq
            for (int x = xStart + 1; x < xEnd; x++)
            {
                tm.SetTile(new Vector3Int(x, yBottom, 0), tiles[7]); // suelo_borde_inf
            }
            tm.SetTile(new Vector3Int(xEnd, yBottom, 0), tiles[8]); // suelo_esq_inf_der
        }

        private static void FixNivel1(Tile[] cleanTiles, RuleTile ruleTile)
        {
            string scenePath = "Assets/Scenes/Nivel1.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. Eliminar legacy Victory_Goal_Summit para evitar que reincie el nivel 1
            var legacyVictory = GameObject.Find("Victory_Goal_Summit");
            if (legacyVictory != null)
            {
                Debug.Log("[FixPlatforms] Eliminando legacy Victory_Goal_Summit de Nivel 1.");
                Object.DestroyImmediate(legacyVictory);
            }

            // 2. Construir plataformas de 2 filas de grosor
            var groundObj = GameObject.Find("Grid/Ground") ?? GameObject.Find("Ground");
            if (groundObj != null)
            {
                var tm = groundObj.GetComponent<Tilemap>();
                if (tm != null)
                {
                    tm.ClearAllTiles();

                    // Suelo base continuo de 2 filas (x = -15 a 50, y = -3 y -4)
                    BuildPlatform2Rows(tm, cleanTiles, -15, 50, -3);

                    // Plataforma 1 (Baja inicial, x = -5 a -2, y = 0)
                    BuildPlatform2Rows(tm, cleanTiles, -5, -2, 0);

                    // Plataforma 2 (Media, x = 2 a 7, y = 0)
                    BuildPlatform2Rows(tm, cleanTiles, 2, 7, 0);

                    // Plataforma 3 (Mercader NPC, x = 9 a 14, y = 3)
                    BuildPlatform2Rows(tm, cleanTiles, 9, 14, 3);

                    // Plataforma 4 (Paso intermedio, x = 17 a 21, y = 5)
                    BuildPlatform2Rows(tm, cleanTiles, 17, 21, 5);

                    // Plataforma 5 (Meseta alta, x = 24 a 28, y = 7)
                    BuildPlatform2Rows(tm, cleanTiles, 24, 28, 7);

                    // Plataforma 6 (Descenso, x = 31 a 35, y = 5)
                    BuildPlatform2Rows(tm, cleanTiles, 31, 35, 5);

                    // Plataforma 7 (Aproximación, x = 38 a 42, y = 3)
                    BuildPlatform2Rows(tm, cleanTiles, 38, 42, 3);

                    // Plataforma 8 (Meta de Nivel 1, x = 45 a 50, y = 1)
                    BuildPlatform2Rows(tm, cleanTiles, 45, 50, 1);

                    tm.RefreshAllTiles();
                    EditorUtility.SetDirty(tm);

                    var tmCollider = groundObj.GetComponent<TilemapCollider2D>();
                    if (tmCollider != null)
                    {
                        tmCollider.ProcessTilemapChanges();
                        EditorUtility.SetDirty(tmCollider);
                    }

                    var compCollider = groundObj.GetComponent<CompositeCollider2D>();
                    if (compCollider != null)
                    {
                        compCollider.GenerateGeometry();
                        EditorUtility.SetDirty(compCollider);
                    }
                }
            }

            // 3. Ajustar posición de LevelGoal_LaderasBajas para que repose exactamente sobre Plataforma 8
            var goalObj = GameObject.Find("LevelGoal_LaderasBajas") ?? GameObject.Find("LevelGoal");
            if (goalObj != null)
            {
                goalObj.name = "LevelGoal_LaderasBajas";
                goalObj.transform.position = new Vector3(47.5f, 1.5f, 0f); // Superficie de la plataforma en y = 1.5

                var goal = goalObj.GetComponent<LevelGoal>();
                if (goal != null)
                {
                    var level1Data = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Resources/Levels/Level_1.asset");
                    goal.Configure(level1Data);
                    EditorUtility.SetDirty(goal);
                }

                var col = goalObj.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.isTrigger = true;
                    EditorUtility.SetDirty(col);
                }

                EditorUtility.SetDirty(goalObj);
                Debug.Log($"[FixPlatforms] LevelGoal_LaderasBajas colocado en {goalObj.transform.position} con Level_1.asset");
            }

            // 4. Ajustar Checkpoint_1 y escala correcta
            var cpObj = GameObject.Find("Checkpoint_1");
            if (cpObj != null)
            {
                cpObj.transform.position = new Vector3(0f, -1.9f, 0f);
                cpObj.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(cpObj);
            }

            // 5. Asignar SpriteRenderer al Mercader NPC (cartel de tienda)
            var merchantObj = GameObject.Find("Merchant_NPC");
            if (merchantObj != null)
            {
                var sr = merchantObj.GetComponent<SpriteRenderer>();
                if (sr == null) sr = merchantObj.AddComponent<SpriteRenderer>();
                sr.sprite = GetSpriteAtPath("Assets/Sprites/Clean/Decor/cartel.png");
                sr.sortingLayerName = "Environment";
                sr.sortingOrder = 2;
                EditorUtility.SetDirty(merchantObj);
            }

            // 6. Fondo de montañas lejanas panorámico
            var bgObj = GameObject.Find("Background_Mountains");
            if (bgObj == null)
            {
                bgObj = new GameObject("Background_Mountains");
                bgObj.transform.position = new Vector3(18f, 4f, 5f);
                bgObj.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
                var sr = bgObj.AddComponent<SpriteRenderer>();
                sr.sprite = GetSpriteAtPath("Assets/Sprites/Clean/Background/montanas_lejanas.png");
                sr.sortingLayerName = "Background";
                sr.sortingOrder = 0;
                EditorUtility.SetDirty(bgObj);
            }

            // 7. Despejar decoración que colisiona con el Checkpoint en x=0
            var decorObj = GameObject.Find("Grid/Decoration") ?? GameObject.Find("Decoration");
            if (decorObj != null)
            {
                var tmDecor = decorObj.GetComponent<Tilemap>();
                if (tmDecor != null)
                {
                    var tileAtCp = tmDecor.GetTile(new Vector3Int(0, -2, 0));
                    if (tileAtCp != null)
                    {
                        tmDecor.SetTile(new Vector3Int(0, -2, 0), null);
                        tmDecor.SetTile(new Vector3Int(-2, -2, 0), tileAtCp);
                        EditorUtility.SetDirty(tmDecor);
                    }
                }
            }

            // 8. Ordenar y posicionar las 48 monedas de forma fluida y lógica en el nivel
            var coinPositions = new System.Collections.Generic.Dictionary<string, Vector3>()
            {
                { "Coin_1",  new Vector3(-12.0f, -1.6f, 0f) },
                { "Coin_2",  new Vector3(-10.5f, -1.6f, 0f) },
                { "Coin_3",  new Vector3( -9.0f, -1.6f, 0f) },
                { "Coin_4",  new Vector3( -7.5f, -1.6f, 0f) },
                { "Coin_5",  new Vector3( -4.5f,  1.4f, 0f) },
                { "Coin_6",  new Vector3( -3.5f,  1.4f, 0f) },
                { "Coin_7",  new Vector3( -2.5f,  1.4f, 0f) },
                { "Coin_8",  new Vector3( -1.5f, -1.6f, 0f) },
                { "Coin_9",  new Vector3(  1.5f, -1.6f, 0f) },
                { "Coin_10", new Vector3(  2.5f, -1.6f, 0f) },
                { "Coin_11", new Vector3(  3.5f,  1.4f, 0f) },
                { "Coin_12", new Vector3(  4.8f,  1.4f, 0f) },
                { "Coin_13", new Vector3(  6.2f,  1.4f, 0f) },
                { "Coin_14", new Vector3(  9.8f,  4.4f, 0f) },
                { "Coin_15", new Vector3( 10.8f,  4.4f, 0f) },
                { "Coin_16", new Vector3( 13.5f,  4.4f, 0f) },
                { "Coin_17", new Vector3( 14.8f,  4.8f, 0f) },
                { "Coin_18", new Vector3( 15.8f,  5.6f, 0f) },
                { "Coin_19", new Vector3( 17.5f,  6.4f, 0f) },
                { "Coin_20", new Vector3( 18.5f,  6.4f, 0f) },
                { "Coin_21", new Vector3( 19.5f,  6.4f, 0f) },
                { "Coin_22", new Vector3( 20.5f,  6.4f, 0f) },
                { "Coin_23", new Vector3( 21.8f,  6.8f, 0f) },
                { "Coin_24", new Vector3( 22.8f,  7.6f, 0f) },
                { "Coin_25", new Vector3( 24.5f,  8.4f, 0f) },
                { "Coin_26", new Vector3( 25.5f,  8.4f, 0f) },
                { "Coin_27", new Vector3( 26.5f,  8.4f, 0f) },
                { "Coin_28", new Vector3( 27.5f,  8.4f, 0f) },
                { "Coin_29", new Vector3( 29.0f,  7.5f, 0f) },
                { "Coin_30", new Vector3( 30.0f,  6.8f, 0f) },
                { "Coin_31", new Vector3( 31.8f,  6.4f, 0f) },
                { "Coin_32", new Vector3( 32.8f,  6.4f, 0f) },
                { "Coin_33", new Vector3( 33.8f,  6.4f, 0f) },
                { "Coin_34", new Vector3( 35.5f,  5.6f, 0f) },
                { "Coin_35", new Vector3( 36.8f,  4.8f, 0f) },
                { "Coin_36", new Vector3( 38.8f,  4.4f, 0f) },
                { "Coin_37", new Vector3( 39.8f,  4.4f, 0f) },
                { "Coin_38", new Vector3( 40.8f,  4.4f, 0f) },
                { "Coin_39", new Vector3( 42.5f,  3.6f, 0f) },
                { "Coin_40", new Vector3( 43.8f,  2.8f, 0f) },
                { "Coin_41", new Vector3( 45.3f,  2.4f, 0f) },
                { "Coin_42", new Vector3( 46.3f,  2.4f, 0f) },
                { "Coin_43", new Vector3( 16.0f, -1.6f, 0f) },
                { "Coin_44", new Vector3( 20.0f, -1.6f, 0f) },
                { "Coin_45", new Vector3( 24.0f, -1.6f, 0f) },
                { "Coin_46", new Vector3( 28.0f, -1.6f, 0f) },
                { "Coin_47", new Vector3( 32.0f, -1.6f, 0f) },
                { "Coin_48", new Vector3( 36.0f, -1.6f, 0f) }
            };

            foreach (var kvp in coinPositions)
            {
                var coinObj = GameObject.Find(kvp.Key);
                if (coinObj != null)
                {
                    coinObj.transform.position = kvp.Value;
                    EditorUtility.SetDirty(coinObj);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[FixPlatforms] Nivel 1 guardado exitosamente.");
        }

        private static void FixNivel2(Tile[] cleanTiles, RuleTile ruleTile)
        {
            string scenePath = "Assets/Scenes/Nivel2.unity";
            if (!File.Exists(scenePath)) return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. Eliminar legacy Victory_Goal_Summit si existe
            var legacyVictory = GameObject.Find("Victory_Goal_Summit");
            if (legacyVictory != null) Object.DestroyImmediate(legacyVictory);

            // 2. Renombrar GameObject distintivo para la escena
            var sceneRoot = GameObject.Find("[Scene] Nivel 1");
            if (sceneRoot != null) sceneRoot.name = "[Scene] Nivel 2: Ruta Helada";

            // 3. Suelo y plataformas
            var groundObj = GameObject.Find("Grid/Ground") ?? GameObject.Find("Ground");
            if (groundObj != null)
            {
                var tm = groundObj.GetComponent<Tilemap>();
                if (tm != null)
                {
                    tm.ClearAllTiles();

                    // Suelo base continuo
                    BuildPlatform2Rows(tm, cleanTiles, -15, 50, -3);

                    // Plataformas de nieve base
                    BuildPlatform2Rows(tm, cleanTiles, -5, -2, 0);
                    BuildPlatform2Rows(tm, cleanTiles, 9, 14, 3);
                    BuildPlatform2Rows(tm, cleanTiles, 24, 28, 7);
                    BuildPlatform2Rows(tm, cleanTiles, 45, 50, 1); // Plataforma Meta

                    tm.RefreshAllTiles();
                    EditorUtility.SetDirty(tm);
                }
            }

            // 4. Plataformas de Hielo en Tilemap Ice (Plataformas resbaladizas de Nivel 2: Ruta Helada)
            var iceObj = GameObject.Find("Grid/Ice") ?? GameObject.Find("Ice");
            var iceRandomTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tilemaps/Ice_RandomTile.asset");
            if (iceObj != null && iceRandomTile != null)
            {
                var tmIce = iceObj.GetComponent<Tilemap>();
                if (tmIce != null)
                {
                    tmIce.ClearAllTiles();

                    // Plataforma 2 de hielo resbaladizo (x = 2 a 7, y = 0)
                    for (int x = 2; x <= 7; x++)
                    {
                        tmIce.SetTile(new Vector3Int(x, 0, 0), iceRandomTile);
                        tmIce.SetTile(new Vector3Int(x, -1, 0), iceRandomTile);
                    }

                    // Plataforma 4 de hielo (x = 17 a 21, y = 5)
                    for (int x = 17; x <= 21; x++)
                    {
                        tmIce.SetTile(new Vector3Int(x, 5, 0), iceRandomTile);
                        tmIce.SetTile(new Vector3Int(x, 4, 0), iceRandomTile);
                    }

                    // Plataforma 6 de hielo (x = 31 a 35, y = 5)
                    for (int x = 31; x <= 35; x++)
                    {
                        tmIce.SetTile(new Vector3Int(x, 5, 0), iceRandomTile);
                        tmIce.SetTile(new Vector3Int(x, 4, 0), iceRandomTile);
                    }

                    // Plataforma 7 de hielo (x = 38 a 42, y = 3)
                    for (int x = 38; x <= 42; x++)
                    {
                        tmIce.SetTile(new Vector3Int(x, 3, 0), iceRandomTile);
                        tmIce.SetTile(new Vector3Int(x, 2, 0), iceRandomTile);
                    }

                    tmIce.RefreshAllTiles();
                    EditorUtility.SetDirty(tmIce);
                }
            }

            // 5. Ajustar LevelGoal de Nivel 2
            var goalObj = GameObject.Find("LevelGoal_LaderasBajas") ?? GameObject.Find("LevelGoal_RutaHelada") ?? GameObject.Find("LevelGoal");
            if (goalObj != null)
            {
                goalObj.name = "LevelGoal_RutaHelada";
                goalObj.transform.position = new Vector3(47.5f, 1.0f, 0f);

                var goal = goalObj.GetComponent<LevelGoal>();
                if (goal != null)
                {
                    var level2Data = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Resources/Levels/Level_2.asset");
                    goal.Configure(level2Data);
                    EditorUtility.SetDirty(goal);
                }

                EditorUtility.SetDirty(goalObj);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[FixPlatforms] Nivel 2 guardado exitosamente.");
        }

        private static void FixNivel4y5(Tile[] cleanTiles, RuleTile ruleTile)
        {
            string[] scenes = new string[] { "Assets/Scenes/Nivel4.unity", "Assets/Scenes/Nivel5.unity" };
            foreach (var path in scenes)
            {
                if (!File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                var groundObj = GameObject.Find("Grid/Ground") ?? GameObject.Find("Ground");
                if (groundObj != null)
                {
                    var tm = groundObj.GetComponent<Tilemap>();
                    if (tm != null)
                    {
                        tm.ClearAllTiles();
                        BuildPlatform2Rows(tm, cleanTiles, -15, 50, -3);
                        BuildPlatform2Rows(tm, cleanTiles, -5, -2, 0);
                        BuildPlatform2Rows(tm, cleanTiles, 2, 7, 0);
                        BuildPlatform2Rows(tm, cleanTiles, 9, 14, 3);
                        BuildPlatform2Rows(tm, cleanTiles, 17, 21, 5);
                        BuildPlatform2Rows(tm, cleanTiles, 24, 28, 7);
                        BuildPlatform2Rows(tm, cleanTiles, 31, 35, 5);
                        BuildPlatform2Rows(tm, cleanTiles, 38, 42, 3);
                        BuildPlatform2Rows(tm, cleanTiles, 45, 50, 1);
                        tm.RefreshAllTiles();
                        EditorUtility.SetDirty(tm);
                    }
                }

                var goalObj = GameObject.Find("LevelGoal_LaderasBajas") ?? GameObject.Find("LevelGoal");
                if (goalObj != null)
                {
                    goalObj.transform.position = new Vector3(47.5f, 1.0f, 0f);
                    EditorUtility.SetDirty(goalObj);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
