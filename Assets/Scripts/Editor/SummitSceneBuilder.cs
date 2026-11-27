using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

namespace Peakingu.EditorTools
{
    public static class SummitSceneBuilder
    {
        // ─────────────────────────── Rutas de Assets ───────────────────────────
        private const string SCENE_PATH = "Assets/Scenes/Nivel5.unity";
        private const string LEVEL_DATA_PATH = "Assets/Resources/Levels/Level_5.asset";
        private const string TILES_DIR = "Assets/Tilemaps/Tiles";
        private const string ICE_TILE_PATH = "Assets/Tilemaps/Ice_RandomTile.asset";
        private const string ICE_PHYSICS_MAT_PATH = "Assets/Physics/Ice_Material2D.physicsMaterial2D";
        private const string COIN_PREFAB_PATH = "Assets/Prefabs/Coin.prefab";
        private const string CHECKPOINT_PREFAB_PATH = "Assets/Prefabs/Checkpoint.prefab";
        private const string GRAPPLE_PREFAB_PATH = "Assets/Prefabs/GrapplePoint.prefab";
        private const string GOAL_PREFAB_PATH = "Assets/Prefabs/LevelGoal.prefab";
        private const string CIMA_BG_PATH = "Assets/Sprites/Clean/Background/cima.png";
        private const string CASA_SPRITE_PATH = "Assets/Sprites/Clean/Decor/casa.png";

        // ─────────────────────────── Constantes de Altura y Límites ───────────────────────────
        private const int BASE_GROUND_X_MIN = -14;
        private const int BASE_GROUND_X_MAX = 16;
        private const int BASE_GROUND_Y_TOP = -3; // Superficie en Y = -2.0
        private const float BASE_SURFACE_Y = -2.0f;

        private const int SUMMIT_X_MIN = 0;
        private const int SUMMIT_X_MAX = 12;
        private const int SUMMIT_Y_TOP = 67;      // Superficie en Y = 68.0 (Ascenso total: 70 unidades)
        private const float SUMMIT_SURFACE_Y = 68.0f;

        private static readonly Vector3 PLAYER_SPAWN_POS = new Vector3(-10f, -1.0f, 0f);
        private static readonly Vector3 KILLZONE_POS = new Vector3(4f, -12f, 0f);
        private static readonly Vector2 KILLZONE_SIZE = new Vector2(120f, 4f);

        private const float CONFINER_MIN_X = -18f;
        private const float CONFINER_MAX_X = 26f;
        private const float CONFINER_MIN_Y = -6f;
        private const float CONFINER_MAX_Y = 76f;

        // ─────────────────────────── Fondo y Casa en la Cima ───────────────────────────
        private static readonly Color SKY_TOP_COLOR = new Color(0.055f, 0.141f, 0.294f, 1f); // RGB (14, 36, 75)
        private static readonly Vector3 BG_START_POS = new Vector3(4f, 3.0f, 5f);
        private static readonly Vector3 BG_SCALE = new Vector3(1.6f, 1.6f, 1f);
        private const float BG_PARALLAX_X = 0.12f;
        private const float BG_PARALLAX_Y = 0.90f;

        private static readonly Vector3 CASA_POS = new Vector3(8.5f, SUMMIT_SURFACE_Y, 0f);
        private static readonly Vector3 GOAL_POS = new Vector3(6.2f, SUMMIT_SURFACE_Y + 1.45f, 0f);

        // ─────────────────────────── Checkpoints (3 repartidos) ───────────────────────────
        private static readonly Vector3[] CHECKPOINT_POSITIONS =
        {
            new Vector3(18.0f, 16.95f, 0f), // Checkpoint 1: Fin Tramo 1 (superficie 16.0)
            new Vector3(-8.0f, 36.95f, 0f), // Checkpoint 2: Fin Tramo 2 (superficie 36.0)
            new Vector3( 9.0f, 52.95f, 0f), // Checkpoint 3: Mitad Tramo 3 (superficie 52.0)
        };

        // ─────────────────────────── GrapplePoints Opcionales (6 en tramos altos) ───────────────────────────
        private static readonly Vector3[] GRAPPLE_POSITIONS =
        {
            new Vector3(10.0f, 34.5f, 0f),
            new Vector3( 2.0f, 44.5f, 0f),
            new Vector3( 5.0f, 50.5f, 0f),
            new Vector3(12.0f, 56.5f, 0f),
            new Vector3( 3.0f, 62.5f, 0f),
            new Vector3(11.0f, 68.5f, 0f),
        };

        // ─────────────────────────── Plataformas del Ascenso (Paso vertical constante +2.0u) ───────────────────────────
        private struct PlatformDef
        {
            public int xStart;
            public int xEnd;
            public int yTop;
            public bool isIce;
        }

        private static readonly PlatformDef[] PLATFORMS =
        {
            // Tramo 1: Ladera Nevada (y = 0..16, Snow)
            new PlatformDef { xStart =  -4, xEnd =  1, yTop = -1, isIce = false }, // Sup 0
            new PlatformDef { xStart =   4, xEnd =  9, yTop =  1, isIce = false }, // Sup 2
            new PlatformDef { xStart =  12, xEnd = 17, yTop =  3, isIce = false }, // Sup 4
            new PlatformDef { xStart =   6, xEnd = 11, yTop =  5, isIce = false }, // Sup 6
            new PlatformDef { xStart =  -2, xEnd =  3, yTop =  7, isIce = false }, // Sup 8
            new PlatformDef { xStart = -10, xEnd = -4, yTop =  9, isIce = false }, // Sup 10
            new PlatformDef { xStart =  -1, xEnd =  5, yTop = 11, isIce = false }, // Sup 12
            new PlatformDef { xStart =   8, xEnd = 13, yTop = 13, isIce = false }, // Sup 14
            new PlatformDef { xStart =  15, xEnd = 21, yTop = 15, isIce = false }, // Sup 16 (Checkpoint 1)

            // Tramo 2: Cornisas de Hielo (y = 18..36, Hielo resbaladizo + descansos de nieve)
            new PlatformDef { xStart =   9, xEnd = 14, yTop = 17, isIce = true  }, // Sup 18
            new PlatformDef { xStart =   1, xEnd =  6, yTop = 19, isIce = true  }, // Sup 20
            new PlatformDef { xStart =  -7, xEnd = -2, yTop = 21, isIce = true  }, // Sup 22
            new PlatformDef { xStart = -12, xEnd = -8, yTop = 23, isIce = false }, // Sup 24 (Descanso nieve)
            new PlatformDef { xStart =  -5, xEnd =  0, yTop = 25, isIce = true  }, // Sup 26
            new PlatformDef { xStart =   3, xEnd =  8, yTop = 27, isIce = true  }, // Sup 28
            new PlatformDef { xStart =  11, xEnd = 16, yTop = 29, isIce = true  }, // Sup 30
            new PlatformDef { xStart =   4, xEnd =  9, yTop = 31, isIce = true  }, // Sup 32
            new PlatformDef { xStart =  -4, xEnd =  1, yTop = 33, isIce = true  }, // Sup 34
            new PlatformDef { xStart = -11, xEnd = -5, yTop = 35, isIce = false }, // Sup 36 (Checkpoint 2)

            // Tramo 3: Cresta del Viento (y = 38..66, Mixto Nieve y Hielo)
            new PlatformDef { xStart =  -2, xEnd =  3, yTop = 37, isIce = true  }, // Sup 38
            new PlatformDef { xStart =   6, xEnd = 11, yTop = 39, isIce = false }, // Sup 40
            new PlatformDef { xStart =  14, xEnd = 19, yTop = 41, isIce = true  }, // Sup 42
            new PlatformDef { xStart =   7, xEnd = 12, yTop = 43, isIce = false }, // Sup 44
            new PlatformDef { xStart =  -1, xEnd =  4, yTop = 45, isIce = true  }, // Sup 46
            new PlatformDef { xStart =  -9, xEnd = -4, yTop = 47, isIce = false }, // Sup 48
            new PlatformDef { xStart =  -2, xEnd =  3, yTop = 49, isIce = true  }, // Sup 50
            new PlatformDef { xStart =   6, xEnd = 12, yTop = 51, isIce = false }, // Sup 52 (Checkpoint 3)
            new PlatformDef { xStart =  14, xEnd = 19, yTop = 53, isIce = true  }, // Sup 54
            new PlatformDef { xStart =   7, xEnd = 12, yTop = 55, isIce = false }, // Sup 56
            new PlatformDef { xStart =   0, xEnd =  5, yTop = 57, isIce = true  }, // Sup 58
            new PlatformDef { xStart =  -7, xEnd = -2, yTop = 59, isIce = false }, // Sup 60
            new PlatformDef { xStart =   0, xEnd =  5, yTop = 61, isIce = true  }, // Sup 62
            new PlatformDef { xStart =   7, xEnd = 12, yTop = 63, isIce = false }, // Sup 64
            new PlatformDef { xStart =  14, xEnd = 18, yTop = 65, isIce = true  }, // Sup 66
        };

        // ─────────────────────────── 25 Monedas Guiando la Ruta ───────────────────────────
        private static readonly Vector3[] COIN_POSITIONS =
        {
            new Vector3( -6.5f, -1.25f, 0f),
            new Vector3( -1.5f,  0.75f, 0f),
            new Vector3(  6.5f,  2.75f, 0f),
            new Vector3( 14.5f,  4.75f, 0f),
            new Vector3(  8.5f,  6.75f, 0f),
            new Vector3(  0.5f,  8.75f, 0f),
            new Vector3( -7.0f, 10.75f, 0f),
            new Vector3(  2.0f, 12.75f, 0f),
            new Vector3( 10.5f, 14.75f, 0f),
            new Vector3( 11.5f, 18.75f, 0f),
            new Vector3(  3.5f, 20.75f, 0f),
            new Vector3( -4.5f, 22.75f, 0f),
            new Vector3(-10.0f, 24.75f, 0f),
            new Vector3( -2.5f, 26.75f, 0f),
            new Vector3(  5.5f, 28.75f, 0f),
            new Vector3( 13.5f, 30.75f, 0f),
            new Vector3(  6.5f, 32.75f, 0f),
            new Vector3( -1.5f, 34.75f, 0f),
            new Vector3(  0.5f, 38.75f, 0f),
            new Vector3( 16.5f, 42.75f, 0f),
            new Vector3(  1.5f, 46.75f, 0f),
            new Vector3( -6.5f, 48.75f, 0f),
            new Vector3( 16.5f, 54.75f, 0f),
            new Vector3( -4.5f, 60.75f, 0f),
            new Vector3(  9.5f, 64.75f, 0f),
        };

        private static readonly string[] SUELO_NAMES =
        {
            "suelo_esq_sup_izq", "suelo_borde_sup", "suelo_esq_sup_der",
            "suelo_borde_izq",   "suelo_relleno",   "suelo_borde_der",
            "suelo_esq_inf_izq", "suelo_borde_inf", "suelo_esq_inf_der"
        };

        [MenuItem("Tools/Peakingu/Construir Nivel 5 Cima")]
        public static void BuildSummitScene()
        {
            try
            {
                if (!File.Exists(SCENE_PATH))
                {
                    Debug.LogError($"[SummitSceneBuilder] No existe la escena: {SCENE_PATH}");
                    return;
                }

                var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

                // 1. Limpiar objetos de Nivel1 que no pertenecen a la Cima
                RemoveUnusedNivel1Objects();

                // 2. Configurar Pingu, KillZone y Scene Root
                SetupSceneMetadataAndPlayer();

                // 3. Reconstruir Tilemaps (Ground + Ice + Decoration)
                BuildTilemaps();

                // 4. Instanciar 3 Checkpoints, 25 Monedas y 6 GrapplePoints
                BuildCheckpoints();
                BuildCoins();
                BuildGrapplePoints();

                // 5. Configurar Cámara, Confiner y Fondo con Parallax
                SetupCameraAndParallaxBackground();

                // 6. Configurar Meseta de la Cima: Casa + LevelGoal (Level_5.asset)
                BuildSummitHouseAndGoal();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();

                Debug.Log("[SummitSceneBuilder] SUCCESS: Nivel 5 'Cima Peakingu' construido (ascenso 70u, 3 Checkpoints, 25 Monedas, 6 GrapplePoints, Casa y LevelGoal).");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SummitSceneBuilder] Error al construir Nivel 5: {ex}");
            }
        }

        private static void RemoveUnusedNivel1Objects()
        {
            string[] toRemove = { "Merchant_NPC", "Canvas_Shop", "Background_Mountains", "Coins_Container", "Checkpoints_Container", "GrapplePoints_Container", "Casa_Cima" };
            foreach (var name in toRemove)
            {
                var obj = GameObject.Find(name);
                if (obj != null) Object.DestroyImmediate(obj);
            }

            // Eliminar checkpoints y metas previos sueltos
            foreach (var cp in Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(cp.gameObject);
            }
            foreach (var goal in Object.FindObjectsByType<LevelGoal>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(goal.gameObject);
            }
            foreach (var gp in Object.FindObjectsByType<GrapplePoint>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(gp.gameObject);
            }
            foreach (var coin in Object.FindObjectsByType<Coin>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(coin.gameObject);
            }
        }

        private static void SetupSceneMetadataAndPlayer()
        {
            var oldRoot = GameObject.Find("[Scene] Nivel 1");
            if (oldRoot != null) oldRoot.name = "[Scene] Nivel 5: Cima Peakingu";

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = PLAYER_SPAWN_POS;
                EditorUtility.SetDirty(player);
            }

            var killZone = GameObject.Find("KillZone");
            if (killZone != null)
            {
                killZone.transform.position = KILLZONE_POS;
                var box = killZone.GetComponent<BoxCollider2D>();
                if (box != null) box.size = KILLZONE_SIZE;
                EditorUtility.SetDirty(killZone);
            }
        }

        private static void BuildTilemaps()
        {
            Tile[] sueloTiles = new Tile[SUELO_NAMES.Length];
            for (int i = 0; i < SUELO_NAMES.Length; i++)
            {
                sueloTiles[i] = AssetDatabase.LoadAssetAtPath<Tile>($"{TILES_DIR}/{SUELO_NAMES[i]}.asset");
            }

            var iceTile = AssetDatabase.LoadAssetAtPath<TileBase>(ICE_TILE_PATH);
            var iceMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ICE_PHYSICS_MAT_PATH);

            var groundObj = GameObject.Find("Grid/Ground") ?? GameObject.Find("Ground");
            var iceObj = GameObject.Find("Grid/Ice") ?? GameObject.Find("Ice");
            var decorObj = GameObject.Find("Grid/Decoration") ?? GameObject.Find("Decoration");

            Tilemap tmGround = groundObj != null ? groundObj.GetComponent<Tilemap>() : null;
            Tilemap tmIce = iceObj != null ? iceObj.GetComponent<Tilemap>() : null;
            Tilemap tmDecor = decorObj != null ? decorObj.GetComponent<Tilemap>() : null;

            if (tmGround != null) tmGround.ClearAllTiles();
            if (tmIce != null) tmIce.ClearAllTiles();
            if (tmDecor != null)
            {
                tmDecor.ClearAllTiles();
                tmDecor.tileAnchor = new Vector3(0.5f, 0f, 0f);
            }

            // Base nevada inicial y meseta de la cima en Ground
            if (tmGround != null)
            {
                Build2RowPlatform(tmGround, sueloTiles, BASE_GROUND_X_MIN, BASE_GROUND_X_MAX, BASE_GROUND_Y_TOP);
                Build2RowPlatform(tmGround, sueloTiles, SUMMIT_X_MIN, SUMMIT_X_MAX, SUMMIT_Y_TOP);
            }

            // Plataformas del ascenso
            foreach (var p in PLATFORMS)
            {
                if (p.isIce && tmIce != null && iceTile != null)
                {
                    for (int x = p.xStart; x <= p.xEnd; x++)
                    {
                        tmIce.SetTile(new Vector3Int(x, p.yTop, 0), iceTile);
                        tmIce.SetTile(new Vector3Int(x, p.yTop - 1, 0), iceTile);
                    }
                }
                else if (tmGround != null)
                {
                    Build2RowPlatform(tmGround, sueloTiles, p.xStart, p.xEnd, p.yTop);
                }
            }

            // Decoración ligera en la base y en la meseta final
            if (tmDecor != null)
            {
                var pino1 = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tilemaps/Tiles/Decor/pino_01.asset");
                var pino2 = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tilemaps/Tiles/Decor/pino_02.asset");
                var roca1 = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tilemaps/Tiles/Decor/roca_01.asset");
                if (pino1 != null) tmDecor.SetTile(new Vector3Int(-13, -2, 0), pino1);
                if (roca1 != null) tmDecor.SetTile(new Vector3Int(13, -2, 0), roca1);
                if (pino2 != null) tmDecor.SetTile(new Vector3Int(2, 68, 0), pino2);
                EditorUtility.SetDirty(tmDecor);
            }

            // Asegurar PhysicsMaterial2D resbaladizo y regenerar geometría de colisión
            if (iceObj != null && iceMat != null)
            {
                var iceTmCol = iceObj.GetComponent<TilemapCollider2D>();
                if (iceTmCol != null) iceTmCol.sharedMaterial = iceMat;
                var iceComp = iceObj.GetComponent<CompositeCollider2D>();
                if (iceComp != null) iceComp.sharedMaterial = iceMat;
                var iceRb = iceObj.GetComponent<Rigidbody2D>();
                if (iceRb != null) iceRb.sharedMaterial = iceMat;
            }

            RefreshTilemapAndCollider(groundObj, tmGround);
            RefreshTilemapAndCollider(iceObj, tmIce);
        }

        private static void Build2RowPlatform(Tilemap tm, Tile[] tiles, int xStart, int xEnd, int yTop)
        {
            if (tiles == null || tiles.Length < 9) return;
            tm.SetTile(new Vector3Int(xStart, yTop, 0), tiles[0]);
            for (int x = xStart + 1; x < xEnd; x++) tm.SetTile(new Vector3Int(x, yTop, 0), tiles[1]);
            tm.SetTile(new Vector3Int(xEnd, yTop, 0), tiles[2]);

            int yBot = yTop - 1;
            tm.SetTile(new Vector3Int(xStart, yBot, 0), tiles[6]);
            for (int x = xStart + 1; x < xEnd; x++) tm.SetTile(new Vector3Int(x, yBot, 0), tiles[7]);
            tm.SetTile(new Vector3Int(xEnd, yBot, 0), tiles[8]);
        }

        private static void RefreshTilemapAndCollider(GameObject obj, Tilemap tm)
        {
            if (obj == null || tm == null) return;
            tm.RefreshAllTiles();
            var tmCol = obj.GetComponent<TilemapCollider2D>();
            if (tmCol != null) tmCol.ProcessTilemapChanges();
            var comp = obj.GetComponent<CompositeCollider2D>();
            if (comp != null) comp.GenerateGeometry();
            EditorUtility.SetDirty(obj);
        }

        private static void BuildCheckpoints()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CHECKPOINT_PREFAB_PATH);
            if (prefab == null) return;

            var container = new GameObject("Checkpoints_Container");
            for (int i = 0; i < CHECKPOINT_POSITIONS.Length; i++)
            {
                var cp = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container.transform);
                cp.name = $"Checkpoint_{i + 1}";
                cp.transform.position = CHECKPOINT_POSITIONS[i];
                cp.transform.localScale = Vector3.one;
            }
        }

        private static void BuildCoins()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(COIN_PREFAB_PATH);
            if (prefab == null) return;

            var container = new GameObject("Coins_Container");
            for (int i = 0; i < COIN_POSITIONS.Length; i++)
            {
                var coin = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container.transform);
                coin.name = $"Coin_{i + 1}";
                coin.transform.position = COIN_POSITIONS[i];
            }
        }

        private static void BuildGrapplePoints()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GRAPPLE_PREFAB_PATH);
            if (prefab == null) return;

            var container = new GameObject("GrapplePoints_Container");
            for (int i = 0; i < GRAPPLE_POSITIONS.Length; i++)
            {
                var gp = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container.transform);
                gp.name = $"GrapplePoint_{i + 1}";
                gp.transform.position = GRAPPLE_POSITIONS[i];
            }
        }

        private static void SetupCameraAndParallaxBackground()
        {
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = SKY_TOP_COLOR;
                EditorUtility.SetDirty(mainCam);
            }

            var confinerObj = GameObject.Find("CameraConfiner_Level1") ?? GameObject.Find("CameraConfiner");
            if (confinerObj != null)
            {
                var poly = confinerObj.GetComponent<PolygonCollider2D>();
                if (poly != null)
                {
                    poly.points = new[]
                    {
                        new Vector2(CONFINER_MIN_X, CONFINER_MIN_Y),
                        new Vector2(CONFINER_MAX_X, CONFINER_MIN_Y),
                        new Vector2(CONFINER_MAX_X, CONFINER_MAX_Y),
                        new Vector2(CONFINER_MIN_X, CONFINER_MAX_Y)
                    };
                    EditorUtility.SetDirty(poly);
                }

                var cmCam = Object.FindAnyObjectByType<CinemachineConfiner2D>();
                if (cmCam != null)
                {
                    cmCam.InvalidateBoundingShapeCache();
                    EditorUtility.SetDirty(cmCam);
                }
            }

            var cimaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CIMA_BG_PATH);
            var bgObj = new GameObject("Background_Cima");
            bgObj.transform.position = BG_START_POS;
            bgObj.transform.localScale = BG_SCALE;

            var sr = bgObj.AddComponent<SpriteRenderer>();
            sr.sprite = cimaSprite;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 0;

            var parallax = bgObj.AddComponent<ParallaxBackground>();
            parallax.Configure(BG_PARALLAX_X, BG_PARALLAX_Y, mainCam != null ? mainCam.transform : null);
        }

        private static void BuildSummitHouseAndGoal()
        {
            var casaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CASA_SPRITE_PATH);
            var casaObj = new GameObject("Casa_Cima");
            casaObj.transform.position = CASA_POS;

            var sr = casaObj.AddComponent<SpriteRenderer>();
            sr.sprite = casaSprite;
            sr.sortingLayerName = "Environment";
            sr.sortingOrder = 2;

            var goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GOAL_PREFAB_PATH);
            var level5Data = AssetDatabase.LoadAssetAtPath<LevelData>(LEVEL_DATA_PATH);
            if (goalPrefab != null)
            {
                var goalObj = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab);
                goalObj.name = "LevelGoal_CimaPeakingu";
                goalObj.transform.position = GOAL_POS;

                var goal = goalObj.GetComponent<LevelGoal>();
                if (goal != null) goal.Configure(level5Data);
            }
        }
    }
}
