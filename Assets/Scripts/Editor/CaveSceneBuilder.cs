using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

namespace Peakingu.EditorTools
{
    public static class CaveSceneBuilder
    {
        [MenuItem("Tools/Peakingu/14. Configurar Escena Cueva (Hielo, Estalactitas, Luces 2D)")]
        public static void BuildCaveScene()
        {
            try
            {
                // 1. Crear Texturas / Sprites para la Cueva
                CreateCaveSprites();

                // 2. Crear PhysicsMaterial2D de baja fricción
                var iceMaterial = CreateIcePhysicsMaterial();

                // 3. Crear Prefab de Estalactita
                var stalactitePrefab = CreateStalactitePrefab();

                // 4. Montar la escena Cueva
                SetupCaveScene(iceMaterial, stalactitePrefab);

                Debug.Log("[Peakingu] SUCCESS: Escena Cueva configurada con Physics Material 2D de hielo, Estalactitas interactivas, indicadores de peligro y Luz 2D en Pingu.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Peakingu] Error al configurar escena Cueva: {ex}");
            }
        }

        #region Creación de Sprites
        public static void CreateCaveSprites()
        {
            string spritesDir = "Assets/Sprites";
            if (!Directory.Exists(spritesDir)) Directory.CreateDirectory(spritesDir);

            // 1. Sprite de Estalactita (32 x 64 pixel art)
            string stalactitePath = Path.Combine(spritesDir, "Stalactite.png");
            if (!File.Exists(stalactitePath))
            {
                int w = 32, h = 64;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;

                Color clear = new Color(0, 0, 0, 0);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        tex.SetPixel(x, y, clear);

                int centerX = w / 2;
                for (int y = 0; y < h; y++)
                {
                    float factor = (float)y / (float)h; // 0 en la punta (abajo), 1 en la base (arriba)
                    int halfWidth = Mathf.Max(1, Mathf.RoundToInt(13 * factor));

                    for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
                    {
                        float xRel = (float)(x - (centerX - halfWidth)) / (float)(halfWidth * 2);

                        Color pixelColor;
                        if (x == centerX - halfWidth || x == centerX + halfWidth || y == 0)
                        {
                            // Borde cristalino oscuro
                            pixelColor = new Color(0.08f, 0.28f, 0.48f, 0.95f);
                        }
                        else if (y >= h - 4)
                        {
                            // Nieve/hielo adherido a la roca
                            pixelColor = new Color(0.95f, 0.98f, 1f, 1f);
                        }
                        else if (xRel < 0.35f)
                        {
                            // Brillo y reflejo de hielo celeste
                            pixelColor = new Color(0.72f, 0.94f, 1f, 0.95f);
                        }
                        else if (xRel < 0.65f)
                        {
                            // Núcleo translúcido de hielo
                            pixelColor = new Color(0.32f, 0.74f, 0.95f, 0.95f);
                        }
                        else
                        {
                            // Sombra profunda de hielo
                            pixelColor = new Color(0.12f, 0.46f, 0.72f, 0.95f);
                        }

                        tex.SetPixel(x, y, pixelColor);
                    }
                }

                tex.Apply();
                File.WriteAllBytes(stalactitePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(stalactitePath);

                var importer = AssetImporter.GetAtPath(stalactitePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 16;
                    importer.filterMode = FilterMode.Point;
                    importer.spritePivot = new Vector2(0.5f, 1f); // Pivot en la parte superior (agarre en el techo)
                    importer.SaveAndReimport();
                }
            }

            // 2. Sprite de Indicador de Peligro (32 x 32 rombo con exclamación)
            string dangerPath = Path.Combine(spritesDir, "Danger_Indicator.png");
            if (!File.Exists(dangerPath))
            {
                int s = 32;
                Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                Color clear = new Color(0, 0, 0, 0);

                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                        tex.SetPixel(x, y, clear);

                int center = s / 2;
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        int dist = Mathf.Abs(x - center) + Mathf.Abs(y - center);
                        if (dist <= 13)
                        {
                            if (dist >= 11)
                            {
                                tex.SetPixel(x, y, new Color(0.15f, 0.05f, 0.05f, 1f)); // Borde
                            }
                            else
                            {
                                tex.SetPixel(x, y, new Color(1f, 0.28f, 0.15f, 1f)); // Relleno ámbar-rojo
                            }
                        }
                    }
                }

                // Signo de exclamación en el centro
                for (int y = 14; y <= 23; y++)
                {
                    tex.SetPixel(center, y, Color.white);
                    tex.SetPixel(center - 1, y, Color.white);
                }
                tex.SetPixel(center, 9, Color.white);
                tex.SetPixel(center - 1, 9, Color.white);
                tex.SetPixel(center, 10, Color.white);
                tex.SetPixel(center - 1, 10, Color.white);

                tex.Apply();
                File.WriteAllBytes(dangerPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(dangerPath);

                var importer = AssetImporter.GetAtPath(dangerPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 16;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                }
            }

            // 3. Sprite de Zona de Peligro en Suelo (48 x 16 elipse rayada)
            string dropZonePath = Path.Combine(spritesDir, "DropZone_Warning.png");
            if (!File.Exists(dropZonePath))
            {
                int w = 48, h = 16;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                Color clear = new Color(0, 0, 0, 0);

                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        tex.SetPixel(x, y, clear);

                float rx = w * 0.45f;
                float ry = h * 0.45f;
                float cx = w * 0.5f;
                float cy = h * 0.5f;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float dx = (x - cx) / rx;
                        float dy = (y - cy) / ry;
                        float distSq = dx * dx + dy * dy;

                        if (distSq <= 1.0f)
                        {
                            if (distSq >= 0.7f)
                            {
                                // Borde punteado de alerta roja
                                bool isStripe = ((x + y) / 3) % 2 == 0;
                                tex.SetPixel(x, y, isStripe ? new Color(1f, 0.2f, 0.15f, 0.9f) : new Color(1f, 0.85f, 0.2f, 0.9f));
                            }
                            else
                            {
                                // Interior translúcido
                                tex.SetPixel(x, y, new Color(1f, 0.2f, 0.15f, 0.35f));
                            }
                        }
                    }
                }

                tex.Apply();
                File.WriteAllBytes(dropZonePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(dropZonePath);

                var importer = AssetImporter.GetAtPath(dropZonePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 16;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();
        }
        #endregion

        #region PhysicsMaterial2D de Hielo
        public static PhysicsMaterial2D CreateIcePhysicsMaterial()
        {
            string physicsDir = "Assets/Physics";
            if (!Directory.Exists(physicsDir)) Directory.CreateDirectory(physicsDir);

            string matPath = Path.Combine(physicsDir, "Ice_Material2D.physicsMaterial2D");
            PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);

            if (mat == null)
            {
                mat = new PhysicsMaterial2D("Ice_Material2D");
                mat.friction = 0.02f;   // Muy baja fricción (hielo súper resbaladizo)
                mat.bounciness = 0.0f; // Sin rebote
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.friction = 0.02f;
                mat.bounciness = 0.0f;
                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CaveSceneBuilder] PhysicsMaterial2D creado en {matPath} con fricción={mat.friction}.");
            return mat;
        }
        #endregion

        #region Prefab de Estalactita
        public static GameObject CreateStalactitePrefab()
        {
            string prefabPath = "Assets/Prefabs/Stalactite.prefab";
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            var stalactiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Stalactite.png");
            var dangerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Danger_Indicator.png");
            var dropZoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/DropZone_Warning.png");

            GameObject obj = new GameObject("Stalactite");
            obj.tag = "Hazard";
            int hazardLayer = LayerMask.NameToLayer("Hazard");
            if (hazardLayer != -1) obj.layer = hazardLayer;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = stalactiteSprite;
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 5;

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = obj.AddComponent<PolygonCollider2D>();
            // Colisionador ajustado a la forma cónica hacia abajo (pivot superior)
            Vector2[] points = new Vector2[]
            {
                new Vector2(-0.75f, 0.0f),
                new Vector2(0.75f, 0.0f),
                new Vector2(0.12f, -3.8f),
                new Vector2(-0.12f, -3.8f)
            };
            col.SetPath(0, points);

            // 1. Indicador flotante de advertencia
            GameObject warnObj = new GameObject("Warning_Indicator");
            warnObj.transform.SetParent(obj.transform, false);
            warnObj.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            var srWarn = warnObj.AddComponent<SpriteRenderer>();
            srWarn.sprite = dangerSprite;
            srWarn.sortingLayerName = "Foreground";
            srWarn.sortingOrder = 15;
            warnObj.SetActive(false);

            // 2. Marcador en suelo
            GameObject groundObj = new GameObject("DropZone_Warning");
            groundObj.transform.SetParent(obj.transform, false);
            groundObj.transform.localPosition = new Vector3(0f, -7.5f, 0f);

            var srGround = groundObj.AddComponent<SpriteRenderer>();
            srGround.sprite = dropZoneSprite;
            srGround.sortingLayerName = "Environment";
            srGround.sortingOrder = 2;
            groundObj.SetActive(false);

            // 3. Luz 2D en estalactita
            GameObject lightObj = new GameObject("Stalactite_Light2D");
            lightObj.transform.SetParent(obj.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, -1.8f, 0f);

            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Point;
            light2D.color = new Color(0.45f, 0.85f, 1f, 1f); // Resplandor celeste inicial
            light2D.pointLightInnerRadius = 0.5f;
            light2D.pointLightOuterRadius = 3.2f;
            light2D.intensity = 0.8f;

            var sLayers = SortingLayer.layers;
            int[] layerIds = new int[sLayers.Length];
            for (int i = 0; i < sLayers.Length; i++) layerIds[i] = sLayers[i].id;
            light2D.targetSortingLayers = layerIds;

            // 4. Componente FallingStalactite
            var stalactiteComp = obj.AddComponent<FallingStalactite>();
            stalactiteComp.Configure(2.4f, 9.0f, 0.5f, warnObj, groundObj, light2D);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Object.DestroyImmediate(obj);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CaveSceneBuilder] Prefab de Estalactita guardado en {prefabPath}.");
            return prefab;
        }
        #endregion

        #region Montaje de la Escena Cueva
        public static void SetupCaveScene(PhysicsMaterial2D iceMaterial, GameObject stalactitePrefab)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Cueva.unity", OpenSceneMode.Single);

            // 1. Asegurar EventSystem
            var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 2. Luz Global 2D Oscura y Subterránea
            var globalLight = Object.FindAnyObjectByType<Light2D>();
            if (globalLight == null || globalLight.lightType != Light2D.LightType.Global)
            {
                GameObject glObj = new GameObject("Global_Light2D_Cave");
                globalLight = glObj.AddComponent<Light2D>();
                globalLight.lightType = Light2D.LightType.Global;
            }
            globalLight.color = new Color(0.06f, 0.09f, 0.18f, 1f); // Azul medianoche muy oscuro
            globalLight.intensity = 0.22f; // Ambiente oscuro que resalta la luz de Pingu

            var sLayers = SortingLayer.layers;
            int[] allLayerIds = new int[sLayers.Length];
            for (int i = 0; i < sLayers.Length; i++) allLayerIds[i] = sLayers[i].id;
            globalLight.targetSortingLayers = allLayerIds;

            // 3. Tilemap de Hielo con baja fricción (Ice_Material2D)
            var existingGrid = GameObject.Find("Grid");
            if (existingGrid != null) Object.DestroyImmediate(existingGrid);

            GameObject gridObj = new GameObject("Grid");
            gridObj.AddComponent<Grid>();

            GameObject tmObj = new GameObject("Tilemap_IceGround");
            tmObj.transform.SetParent(gridObj.transform, false);
            tmObj.tag = "Ground";
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1) tmObj.layer = groundLayer;

            var tilemap = tmObj.AddComponent<Tilemap>();
            var tmRenderer = tmObj.AddComponent<TilemapRenderer>();
            tmRenderer.sortingLayerName = "Environment";

            var tmCollider = tmObj.AddComponent<TilemapCollider2D>();
            tmCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            tmCollider.sharedMaterial = iceMaterial;

            var rb2d = tmObj.AddComponent<Rigidbody2D>();
            rb2d.bodyType = RigidbodyType2D.Static;
            rb2d.sharedMaterial = iceMaterial;

            var compCollider = tmObj.AddComponent<CompositeCollider2D>();
            compCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
            compCollider.sharedMaterial = iceMaterial;

            // Cargar o crear Tile de Hielo
            string tilePath = "Assets/Tilemaps/IceTile.asset";
            Tile iceTile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (iceTile == null)
            {
                iceTile = ScriptableObject.CreateInstance<Tile>();
                iceTile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Square.png");
                iceTile.color = new Color(0.35f, 0.75f, 0.95f, 1f); // Cyan gélido
                iceTile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(iceTile, tilePath);
            }

            // Pintar suelo de hielo (-15 a 48, y = -3, -4)
            for (int x = -15; x <= 48; x++)
            {
                tilemap.SetTile(new Vector3Int(x, -3, 0), iceTile);
                tilemap.SetTile(new Vector3Int(x, -4, 0), iceTile);
            }

            // Pintar techo de la cueva (-15 a 48, y = 7, 8)
            for (int x = -15; x <= 48; x++)
            {
                tilemap.SetTile(new Vector3Int(x, 7, 0), iceTile);
                tilemap.SetTile(new Vector3Int(x, 8, 0), iceTile);
            }

            // Plataformas elevadas dentro de la cueva
            // Plataforma 1 (x = 2 a 7, y = 0)
            for (int x = 2; x <= 7; x++) tilemap.SetTile(new Vector3Int(x, 0, 0), iceTile);
            // Plataforma 2 (x = 12 a 17, y = 2)
            for (int x = 12; x <= 17; x++) tilemap.SetTile(new Vector3Int(x, 2, 0), iceTile);
            // Plataforma 3 (x = 22 a 27, y = 1)
            for (int x = 22; x <= 27; x++) tilemap.SetTile(new Vector3Int(x, 1, 0), iceTile);
            // Plataforma 4 (x = 32 a 37, y = 0)
            for (int x = 32; x <= 37; x++) tilemap.SetTile(new Vector3Int(x, 0, 0), iceTile);

            tilemap.RefreshAllTiles();
            tmCollider.ProcessTilemapChanges();
            compCollider.GenerateGeometry();

            // 4. Instanciar Pingu con Luz 2D propia
            var existingPingu = GameObject.Find("Pingu");
            if (existingPingu != null) Object.DestroyImmediate(existingPingu);

            var pinguPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pingu.prefab");
            GameObject pingu = null;
            if (pinguPrefab != null)
            {
                pingu = (GameObject)PrefabUtility.InstantiatePrefab(pinguPrefab);
            }
            if (pingu != null)
            {
                pingu.transform.position = new Vector3(-10f, -1f, 0f);

                // Agregar Luz 2D en Pingu (linterna / bioluminiscencia)
                var existingLight = pingu.transform.Find("Pingu_Cave_Light2D");
                if (existingLight != null) Object.DestroyImmediate(existingLight.gameObject);

                GameObject pinguLightObj = new GameObject("Pingu_Cave_Light2D");
                pinguLightObj.transform.SetParent(pingu.transform, false);
                pinguLightObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);

                var pLight = pinguLightObj.AddComponent<Light2D>();
                pLight.lightType = Light2D.LightType.Point;
                pLight.color = new Color(0.75f, 0.95f, 1f, 1f); // Luz cálida celeste/blanca
                pLight.pointLightInnerRadius = 1.2f;
                pLight.pointLightOuterRadius = 5.5f;
                pLight.intensity = 1.35f;
                pLight.targetSortingLayers = allLayerIds;
            }

            // 5. Instanciar Estalactitas Colgantes en el techo
            var existingStalactites = GameObject.Find("Stalactites_Container");
            if (existingStalactites != null) Object.DestroyImmediate(existingStalactites);

            GameObject stalactitesContainer = new GameObject("Stalactites_Container");
            Vector3[] stalactitePositions = new Vector3[]
            {
                new Vector3(-4f, 6.8f, 0f),   // Camino inicial
                new Vector3(4.5f, 6.8f, 0f),  // Sobre Plataforma 1
                new Vector3(9.5f, 6.8f, 0f),  // Entre plataformas
                new Vector3(14.5f, 6.8f, 0f), // Sobre Plataforma 2
                new Vector3(19.5f, 6.8f, 0f), // Gap hacia Plataforma 3
                new Vector3(25f, 6.8f, 0f),   // Sobre Plataforma 3
                new Vector3(30f, 6.8f, 0f),   // Descenso
                new Vector3(35f, 6.8f, 0f),   // Sobre Plataforma 4
                new Vector3(40f, 6.8f, 0f)    // Tramo final antes de la meta
            };

            for (int i = 0; i < stalactitePositions.Length; i++)
            {
                GameObject stInstance = (GameObject)PrefabUtility.InstantiatePrefab(stalactitePrefab);
                stInstance.name = $"Stalactite_{i + 1}";
                stInstance.transform.position = stalactitePositions[i];
                stInstance.transform.SetParent(stalactitesContainer.transform, true);
            }

            // 6. Monedas en la cueva
            var existingCoins = GameObject.Find("Coins_Container");
            if (existingCoins != null) Object.DestroyImmediate(existingCoins);

            var coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coin.prefab");
            if (coinPrefab != null)
            {
                GameObject coinsContainer = new GameObject("Coins_Container");
                Vector3[] coinPositions = new Vector3[]
                {
                    new Vector3(-8f, -2f, 0f),
                    new Vector3(-6f, -2f, 0f),
                    new Vector3(-4f, -2f, 0f),
                    new Vector3(3f, 1.2f, 0f),
                    new Vector3(4.5f, 1.2f, 0f),
                    new Vector3(6f, 1.2f, 0f),
                    new Vector3(9f, -2f, 0f),
                    new Vector3(13f, 3.2f, 0f),
                    new Vector3(14.5f, 3.2f, 0f),
                    new Vector3(16f, 3.2f, 0f),
                    new Vector3(23f, 2.2f, 0f),
                    new Vector3(24.5f, 2.2f, 0f),
                    new Vector3(26f, 2.2f, 0f),
                    new Vector3(33f, 1.2f, 0f),
                    new Vector3(34.5f, 1.2f, 0f),
                    new Vector3(36f, 1.2f, 0f)
                };

                for (int i = 0; i < coinPositions.Length; i++)
                {
                    GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);
                    c.name = $"Coin_Cave_{i + 1}";
                    c.transform.position = coinPositions[i];
                    c.transform.SetParent(coinsContainer.transform, true);
                }
            }

            // 7. HUD y UI completa (HUD.prefab con PauseManager, GameOver y Victoria)
            var existingHUD = GameObject.Find("HUD");
            if (existingHUD != null) Object.DestroyImmediate(existingHUD);

            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HUD.prefab");
            if (hudPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(hudPrefab);
            }

            // 8. GameManager
            var existingGM = Object.FindAnyObjectByType<GameManager>();
            if (existingGM == null)
            {
                var gmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab");
                if (gmPrefab != null)
                {
                    PrefabUtility.InstantiatePrefab(gmPrefab);
                }
            }

            // 9. Meta de Victoria
            var existingGoal = GameObject.Find("Victory_Goal_Cave");
            if (existingGoal != null) Object.DestroyImmediate(existingGoal);

            var heightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Height_Icon.png");
            GameObject goalObj = new GameObject("Victory_Goal_Cave");
            goalObj.transform.position = new Vector3(44f, -1.5f, 0f);

            var srGoal = goalObj.AddComponent<SpriteRenderer>();
            srGoal.sprite = heightSprite;
            srGoal.color = new Color(0.36f, 0.95f, 1f);
            srGoal.sortingLayerName = "Collectibles";
            goalObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            var colGoal = goalObj.AddComponent<BoxCollider2D>();
            colGoal.isTrigger = true;
            colGoal.size = new Vector2(2.5f, 2.5f);
            goalObj.AddComponent<VictoryTrigger>();

            // 10. Checkpoint y KillZone
            var existingCP = Object.FindAnyObjectByType<Checkpoint>();
            if (existingCP == null)
            {
                var cpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Checkpoint.prefab");
                if (cpPrefab != null)
                {
                    var cpObj = (GameObject)PrefabUtility.InstantiatePrefab(cpPrefab);
                    cpObj.name = "Checkpoint_Cave";
                    cpObj.transform.position = new Vector3(14.5f, 2.2f, 0f);
                }
            }

            var existingKZ = Object.FindAnyObjectByType<KillZone>();
            if (existingKZ == null)
            {
                GameObject kzObj = new GameObject("KillZone");
                kzObj.transform.position = new Vector3(15f, -7f, 0f);
                var kzCol = kzObj.AddComponent<BoxCollider2D>();
                kzCol.isTrigger = true;
                kzCol.size = new Vector2(160f, 2f);
                kzObj.AddComponent<KillZone>();
            }

            // 11. Confiner 2D y Cámara Cinemachine
            var existingConfiner = GameObject.Find("CameraConfiner_Cave");
            if (existingConfiner != null) Object.DestroyImmediate(existingConfiner);

            GameObject confinerObj = new GameObject("CameraConfiner_Cave");
            var polyCollider = confinerObj.AddComponent<PolygonCollider2D>();
            polyCollider.isTrigger = true;
            Vector2[] confinerPoints = new Vector2[]
            {
                new Vector2(-20f, -6f),
                new Vector2(52f, -6f),
                new Vector2(52f, 12f),
                new Vector2(-20f, 12f)
            };
            polyCollider.SetPath(0, confinerPoints);

            // Cámara
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                if (mainCam.GetComponent<CinemachineBrain>() == null)
                {
                    mainCam.gameObject.AddComponent<CinemachineBrain>();
                }
            }

            var existingCM = GameObject.Find("CM_Camera2D_Cave");
            if (existingCM != null) Object.DestroyImmediate(existingCM);

            GameObject cmObj = new GameObject("CM_Camera2D_Cave");
            var cmCam = cmObj.AddComponent<CinemachineCamera>();
            if (pingu != null) cmCam.Target.TrackingTarget = pingu.transform;
            cmCam.Lens.OrthographicSize = 5f;

            var composer = cmObj.AddComponent<CinemachinePositionComposer>();
            composer.Damping = new Vector3(0.8f, 1.2f, 0f);
            composer.Composition.DeadZone.Enabled = true;
            composer.Composition.DeadZone.Size = new Vector2(0.12f, 0.2f);

            var confiner = cmObj.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = polyCollider;

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        #endregion
    }
}
