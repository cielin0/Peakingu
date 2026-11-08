#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;
using TMPro;

namespace Peakingu.Editor
{
    public class ProjectSetup : EditorWindow
    {
        private static readonly string[] RequiredTags =
        {
            "Player",
            "Ground",
            "Hazard",
            "Coin",
            "Merchant",
            "Checkpoint"
        };

        private static readonly (int index, string name)[] RequiredLayers =
        {
            (6, "Player"),
            (7, "Ground"),
            (8, "Hazard"),
            (9, "Coin"),
            (10, "Merchant"),
            (11, "Checkpoint")
        };

        private static readonly string[] RequiredSortingLayers =
        {
            "Background",
            "Default",
            "Environment",
            "Collectibles",
            "Characters",
            "Foreground"
        };

        private Vector2 scrollPos;

        [MenuItem("Tools/Peakingu/1. Configurar Tags, Layers y Matriz 2D")]
        public static void SetupProjectSettings()
        {
            SetupTagsAndLayers();
            SetupCollisionMatrix();
            Debug.Log("[Peakingu] Tags, Layers, Sorting Layers y Matriz 2D configurados exitosamente.");
        }

        [MenuItem("Tools/Peakingu/2. Reparar y Poblar Escenas (Menu, Nivel1, Cueva)")]
        public static void SetupScenesWithURP2D()
        {
            CreateOrUpdateScene("Assets/Scenes/Menu.unity", "[Scene] Menu Principal");
            CreateOrUpdateScene("Assets/Scenes/Nivel1.unity", "[Scene] Nivel 1");
            CreateOrUpdateScene("Assets/Scenes/Cueva.unity", "[Scene] Cueva");

            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Nivel1.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Cueva.unity", true)
            };
            EditorBuildSettings.scenes = scenes;

            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Peakingu] Escenas reparadas con Camara URP 2D, Luz Global 2D y registradas en Build Settings.");
        }

        [MenuItem("Tools/Peakingu/3. Visor de Matriz de Colisiones 2D")]
        public static void OpenMatrixViewer()
        {
            GetWindow<ProjectSetup>("Matriz Fisica 2D");
        }

        [MenuItem("Tools/Peakingu/4. Armar Prefab Pingu")]
        public static void BuildPinguPrefab()
        {
            string spritePath = "Assets/Sprites/Square.png";
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            var existingPingu = GameObject.Find("Pingu");
            if (existingPingu != null) DestroyImmediate(existingPingu);

            GameObject pingu = new GameObject("Pingu");
            pingu.transform.position = new Vector3(0, 0, 0);
            pingu.tag = "Player";
            pingu.layer = LayerMask.NameToLayer("Player");

            var pinguSr = pingu.AddComponent<SpriteRenderer>();
            pinguSr.sprite = sprite;
            pinguSr.color = new Color(0.2f, 0.8f, 1f);
            pinguSr.sortingLayerName = "Characters";

            var rb = pingu.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = pingu.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1f);

            pingu.AddComponent<PlayerController>();

            PrefabUtility.SaveAsPrefabAsset(pingu, "Assets/Prefabs/Pingu.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("[Peakingu] Prefab Pingu actualizado en Assets/Prefabs/Pingu.prefab.");
        }

        [MenuItem("Tools/Peakingu/5. Configurar Nivel 1 (Tilemap y Cinemachine 2D)")]
        public static void SetupLevel1EnvironmentAndCamera()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Nivel1.unity", OpenSceneMode.Single);

            string spritePath = "Assets/Sprites/Square.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            // 1. Tilemap Tile asset con colisionador tipo Grid
            string tilePath = "Assets/Tilemaps/GroundTile.asset";
            Tile groundTile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (groundTile == null)
            {
                groundTile = ScriptableObject.CreateInstance<Tile>();
                groundTile.sprite = sprite;
                groundTile.color = new Color(0.35f, 0.65f, 0.35f);
                groundTile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(groundTile, tilePath);
            }
            else
            {
                groundTile.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(groundTile);
            }

            // 2. Limpiar objetos viejos temporales
            var oldGround = GameObject.Find("Ground_Test");
            if (oldGround != null) DestroyImmediate(oldGround);
            var oldPlat = GameObject.Find("Platform_Test");
            if (oldPlat != null) DestroyImmediate(oldPlat);

            // 3. Grid y Tilemap_Ground con CompositeCollider2D (API Unity 6)
            var existingGrid = GameObject.Find("Grid");
            if (existingGrid != null) DestroyImmediate(existingGrid);

            GameObject gridObj = new GameObject("Grid");
            gridObj.AddComponent<Grid>();

            GameObject tilemapObj = new GameObject("Tilemap_Ground");
            tilemapObj.transform.parent = gridObj.transform;
            tilemapObj.tag = "Ground";
            tilemapObj.layer = LayerMask.NameToLayer("Ground");

            var tilemap = tilemapObj.AddComponent<Tilemap>();
            var tmRenderer = tilemapObj.AddComponent<TilemapRenderer>();
            tmRenderer.sortingLayerName = "Environment";

            var tmCollider = tilemapObj.AddComponent<TilemapCollider2D>();
            tmCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            var rb2d = tilemapObj.GetComponent<Rigidbody2D>();
            if (rb2d == null) rb2d = tilemapObj.AddComponent<Rigidbody2D>();
            rb2d.bodyType = RigidbodyType2D.Static;

            var compCollider = tilemapObj.AddComponent<CompositeCollider2D>();
            compCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;

            // Pintar suelo y plataformas
            for (int x = -15; x <= 22; x++)
            {
                tilemap.SetTile(new Vector3Int(x, -3, 0), groundTile);
                tilemap.SetTile(new Vector3Int(x, -4, 0), groundTile);
            }
            for (int x = 2; x <= 7; x++)
            {
                tilemap.SetTile(new Vector3Int(x, 0, 0), groundTile);
            }
            for (int x = 9; x <= 14; x++)
            {
                tilemap.SetTile(new Vector3Int(x, 3, 0), groundTile);
            }

            // Forzar actualización inmediata de la geometría física en el Tilemap
            tilemap.RefreshAllTiles();
            tmCollider.ProcessTilemapChanges();
            compCollider.GenerateGeometry();

            // 4. Instanciar Pingu
            var pingu = GameObject.Find("Pingu");
            if (pingu == null)
            {
                var pinguPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pingu.prefab");
                if (pinguPrefab != null)
                {
                    pingu = (GameObject)PrefabUtility.InstantiatePrefab(pinguPrefab);
                }
                else
                {
                    BuildPinguPrefab();
                    pingu = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pingu.prefab"));
                }
            }
            pingu.transform.position = new Vector3(-8, -1, 0);

            // 5. Confiner 2D
            var existingConfiner = GameObject.Find("CameraConfiner_Level1");
            if (existingConfiner != null) DestroyImmediate(existingConfiner);

            GameObject confinerObj = new GameObject("CameraConfiner_Level1");
            var polyCollider = confinerObj.AddComponent<PolygonCollider2D>();
            polyCollider.isTrigger = true;
            Vector2[] confinerPoints = new Vector2[]
            {
                new Vector2(-20f, -6f),
                new Vector2(25f, -6f),
                new Vector2(25f, 15f),
                new Vector2(-20f, 15f)
            };
            polyCollider.SetPath(0, confinerPoints);

            // 6. Main Camera con CinemachineBrain
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                if (mainCam.GetComponent<CinemachineBrain>() == null)
                {
                    mainCam.gameObject.AddComponent<CinemachineBrain>();
                }
            }

            // 7. CinemachineCamera 2D (Unity 6 / Cinemachine 3)
            var existingCM = GameObject.Find("CM_Camera2D");
            if (existingCM != null) DestroyImmediate(existingCM);

            GameObject cmObj = new GameObject("CM_Camera2D");
            var cmCam = cmObj.AddComponent<CinemachineCamera>();
            cmCam.Target.TrackingTarget = pingu.transform;
            cmCam.Lens.OrthographicSize = 5f;

            // Position Composer (Dead Zone, Damping, Lookahead suave en Y)
            var composer = cmObj.AddComponent<CinemachinePositionComposer>();
            composer.Damping = new Vector3(0.8f, 1.2f, 0f);

            composer.Composition.DeadZone.Enabled = true;
            composer.Composition.DeadZone.Size = new Vector2(0.12f, 0.2f);

            composer.Lookahead.Enabled = true;
            composer.Lookahead.Time = 0.25f;
            composer.Lookahead.Smoothing = 4.0f;
            composer.Lookahead.IgnoreY = false;

            // Confiner 2D
            var confiner = cmObj.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = polyCollider;

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Peakingu] Nivel 1 configurado exitosamente: Grid con Tilemap + CompositeCollider2D, Confiner 2D y CinemachineCamera2D con seguimiento y look-ahead vertical.");
        }

        private void OnGUI()
        {
            GUILayout.Label("Matriz de Colisiones 2D - Peakingu", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "En Unity 6, la matriz nativa se encuentra en 'Edit > Project Settings > Physics 2D', bajando con el scroll hasta el final de la ventana.\n\nAquí puedes ver y alternar directamente las colisiones entre capas del juego:",
                MessageType.Info
            );

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            string[] layers = { "Default", "Player", "Ground", "Hazard", "Coin", "Merchant", "Checkpoint" };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(90));
            foreach (var l in layers)
            {
                GUILayout.Label(l, GUILayout.Width(75));
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < layers.Length; i++)
            {
                int layerA = LayerMask.NameToLayer(layers[i]);
                if (layerA < 0) continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(layers[i], GUILayout.Width(90));

                for (int j = 0; j < layers.Length; j++)
                {
                    int layerB = LayerMask.NameToLayer(layers[j]);
                    if (layerB < 0)
                    {
                        GUILayout.Label("-", GUILayout.Width(75));
                        continue;
                    }

                    if (j < i)
                    {
                        GUILayout.Label("", GUILayout.Width(75));
                        continue;
                    }

                    bool collides = !Physics2D.GetIgnoreLayerCollision(layerA, layerB);
                    bool newCollides = GUILayout.Toggle(collides, "", GUILayout.Width(75));
                    if (newCollides != collides)
                    {
                        Physics2D.IgnoreLayerCollision(layerA, layerB, !newCollides);
                        var physicsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
                        if (physicsAsset != null && physicsAsset.Length > 0)
                        {
                            EditorUtility.SetDirty(physicsAsset[0]);
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(15);
            if (GUILayout.Button("Restablecer Matriz Recomendada de Peakingu", GUILayout.Height(30)))
            {
                SetupCollisionMatrix();
            }
        }

        private static void CreateOrUpdateScene(string path, string identifierName)
        {
            Scene scene;
            string templatePath = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";

            if (System.IO.File.Exists(templatePath))
            {
                scene = EditorSceneManager.OpenScene(templatePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            GameObject idObj = new GameObject(identifierName);
            idObj.transform.position = Vector3.zero;

            // Asegurar que Global Light 2D ilumine todas las Sorting Layers
            var light2D = Object.FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>();
            if (light2D != null)
            {
                var sLayers = SortingLayer.layers;
                int[] ids = new int[sLayers.Length];
                for (int i = 0; i < sLayers.Length; i++) ids[i] = sLayers[i].id;
                light2D.targetSortingLayers = ids;
                EditorUtility.SetDirty(light2D);
            }

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void SetupTagsAndLayers()
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAsset == null || tagManagerAsset.Length == 0)
            {
                Debug.LogError("No se pudo cargar TagManager.asset");
                return;
            }

            var tagManager = new SerializedObject(tagManagerAsset[0]);

            var tagsProp = tagManager.FindProperty("tags");
            foreach (var tag in RequiredTags)
            {
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    int index = tagsProp.arraySize;
                    tagsProp.InsertArrayElementAtIndex(index);
                    tagsProp.GetArrayElementAtIndex(index).stringValue = tag;
                }
            }

            var layersProp = tagManager.FindProperty("layers");
            foreach (var (index, name) in RequiredLayers)
            {
                if (index < layersProp.arraySize)
                {
                    layersProp.GetArrayElementAtIndex(index).stringValue = name;
                }
            }

            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayersProp != null)
            {
                foreach (var sLayer in RequiredSortingLayers)
                {
                    if (sLayer == "Default") continue;

                    bool exists = false;
                    for (int i = 0; i < sortingLayersProp.arraySize; i++)
                    {
                        var entry = sortingLayersProp.GetArrayElementAtIndex(i);
                        var nameProp = entry.FindPropertyRelative("name");
                        if (nameProp != null && nameProp.stringValue == sLayer)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        int newIdx = sortingLayersProp.arraySize;
                        sortingLayersProp.InsertArrayElementAtIndex(newIdx);
                        var newEntry = sortingLayersProp.GetArrayElementAtIndex(newIdx);
                        newEntry.FindPropertyRelative("name").stringValue = sLayer;
                        newEntry.FindPropertyRelative("uniqueID").intValue = newIdx + 1000;
                    }
                }
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        public static void SetupCollisionMatrix()
        {
            var physicsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");

            int defaultL = LayerMask.NameToLayer("Default");
            int player = LayerMask.NameToLayer("Player");
            int ground = LayerMask.NameToLayer("Ground");
            int hazard = LayerMask.NameToLayer("Hazard");
            int coin = LayerMask.NameToLayer("Coin");
            int merchant = LayerMask.NameToLayer("Merchant");
            int checkpoint = LayerMask.NameToLayer("Checkpoint");

            Physics2D.IgnoreLayerCollision(coin, coin, true);
            Physics2D.IgnoreLayerCollision(coin, hazard, true);
            Physics2D.IgnoreLayerCollision(coin, merchant, true);
            Physics2D.IgnoreLayerCollision(coin, checkpoint, true);
            Physics2D.IgnoreLayerCollision(coin, ground, true);
            Physics2D.IgnoreLayerCollision(coin, defaultL, true);

            Physics2D.IgnoreLayerCollision(hazard, hazard, true);
            Physics2D.IgnoreLayerCollision(hazard, merchant, true);
            Physics2D.IgnoreLayerCollision(hazard, checkpoint, true);
            Physics2D.IgnoreLayerCollision(hazard, ground, false);

            Physics2D.IgnoreLayerCollision(checkpoint, checkpoint, true);
            Physics2D.IgnoreLayerCollision(checkpoint, merchant, true);
            Physics2D.IgnoreLayerCollision(checkpoint, hazard, true);
            Physics2D.IgnoreLayerCollision(checkpoint, ground, true);
            Physics2D.IgnoreLayerCollision(checkpoint, defaultL, true);

            Physics2D.IgnoreLayerCollision(merchant, merchant, true);
            Physics2D.IgnoreLayerCollision(merchant, ground, false);

            Physics2D.IgnoreLayerCollision(player, ground, false);
            Physics2D.IgnoreLayerCollision(player, hazard, false);
            Physics2D.IgnoreLayerCollision(player, coin, false);
            Physics2D.IgnoreLayerCollision(player, merchant, false);
            Physics2D.IgnoreLayerCollision(player, checkpoint, false);

            if (physicsAsset != null && physicsAsset.Length > 0)
            {
                EditorUtility.SetDirty(physicsAsset[0]);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Peakingu/6. Importar Sprites de UI y Coleccionables")]
        public static void ConfigureUISprites()
        {
            string[] paths = new string[]
            {
                "Assets/Sprites/Heart_Full.png",
                "Assets/Sprites/Heart_Empty.png",
                "Assets/Sprites/Coin.png",
                "Assets/Sprites/Height_Icon.png"
            };

            foreach (var path in paths)
            {
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

            AssetDatabase.Refresh();
            Debug.Log("[Peakingu] Sprites de UI y Coleccionables configurados como Pixel Art (Point filter, 16 PPU).");
        }

        [MenuItem("Tools/Peakingu/7. Armar Prefab Moneda (Coin)")]
        public static void BuildCoinPrefab()
        {
            ConfigureUISprites();

            string coinSpritePath = "Assets/Sprites/Coin.png";
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(coinSpritePath);

            var existingTemp = GameObject.Find("Temp_Coin");
            if (existingTemp != null) DestroyImmediate(existingTemp);

            GameObject coinObj = new GameObject("Coin");
            coinObj.tag = "Coin";
            int coinLayer = LayerMask.NameToLayer("Coin");
            if (coinLayer != -1) coinObj.layer = coinLayer;

            var sr = coinObj.AddComponent<SpriteRenderer>();
            sr.sprite = coinSprite;
            sr.sortingLayerName = "Collectibles";

            var col = coinObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;

            coinObj.AddComponent<Coin>();

            PrefabUtility.SaveAsPrefabAsset(coinObj, "Assets/Prefabs/Coin.prefab");
            DestroyImmediate(coinObj);

            AssetDatabase.SaveAssets();
            Debug.Log("[Peakingu] Prefab Moneda armado en Assets/Prefabs/Coin.prefab.");
        }

        [MenuItem("Tools/Peakingu/8. Armar Prefab HUD (Canvas + TextMeshPro)")]
        public static void BuildHUDPrefab()
        {
            ConfigureUISprites();

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var heartFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Heart_Full.png");
            var heartEmpty = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Heart_Empty.png");
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Coin.png");
            var heightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Height_Icon.png");

            var existingHUD = GameObject.Find("HUD");
            if (existingHUD != null) DestroyImmediate(existingHUD);

            // 1. Root Canvas
            GameObject hudCanvas = new GameObject("HUD");
            var canvas = hudCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = hudCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            hudCanvas.AddComponent<GraphicRaycaster>();
            var hudManager = hudCanvas.AddComponent<HUDManager>();

            // 2. Contenedor Superior Izquierdo: Vidas y Temporizador
            GameObject topLeft = new GameObject("TopLeft_Group");
            topLeft.transform.SetParent(hudCanvas.transform, false);
            var rtTL = topLeft.AddComponent<RectTransform>();
            rtTL.anchorMin = new Vector2(0f, 1f);
            rtTL.anchorMax = new Vector2(0f, 1f);
            rtTL.pivot = new Vector2(0f, 1f);
            rtTL.anchoredPosition = new Vector2(35f, -30f);
            rtTL.sizeDelta = new Vector2(600f, 60f);

            // Corazones
            GameObject heartsParent = new GameObject("Hearts_Container");
            heartsParent.transform.SetParent(topLeft.transform, false);
            var rtHearts = heartsParent.AddComponent<RectTransform>();
            rtHearts.anchorMin = new Vector2(0f, 0.5f);
            rtHearts.anchorMax = new Vector2(0f, 0.5f);
            rtHearts.pivot = new Vector2(0f, 0.5f);
            rtHearts.anchoredPosition = new Vector2(0f, 0f);
            rtHearts.sizeDelta = new Vector2(230f, 44f);

            var hLayout = heartsParent.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            List<Image> heartImages = new List<Image>();
            for (int i = 0; i < 5; i++)
            {
                GameObject heartObj = new GameObject($"Heart_{i + 1}");
                heartObj.transform.SetParent(heartsParent.transform, false);
                var img = heartObj.AddComponent<Image>();
                img.sprite = heartFull;
                img.preserveAspect = true;
                var rtH = heartObj.GetComponent<RectTransform>();
                rtH.sizeDelta = new Vector2(38f, 38f);
                heartImages.Add(img);
            }

            // Temporizador
            GameObject timerObj = new GameObject("Timer_Text");
            timerObj.transform.SetParent(topLeft.transform, false);
            var rtTimer = timerObj.AddComponent<RectTransform>();
            rtTimer.anchorMin = new Vector2(0f, 0.5f);
            rtTimer.anchorMax = new Vector2(0f, 0.5f);
            rtTimer.pivot = new Vector2(0f, 0.5f);
            rtTimer.anchoredPosition = new Vector2(245f, 0f);
            rtTimer.sizeDelta = new Vector2(260f, 48f);

            var tmpTimer = timerObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpTimer.font = font;
            tmpTimer.fontSize = 32f;
            tmpTimer.fontStyle = FontStyles.Bold;
            tmpTimer.color = Color.white;
            tmpTimer.text = "TIME: 20:00";
            tmpTimer.alignment = TextAlignmentOptions.MidlineLeft;

            // 3. Contenedor Superior Derecho: Monedas y Altura
            GameObject topRight = new GameObject("TopRight_Group");
            topRight.transform.SetParent(hudCanvas.transform, false);
            var rtTR = topRight.AddComponent<RectTransform>();
            rtTR.anchorMin = new Vector2(1f, 1f);
            rtTR.anchorMax = new Vector2(1f, 1f);
            rtTR.pivot = new Vector2(1f, 1f);
            rtTR.anchoredPosition = new Vector2(-35f, -30f);
            rtTR.sizeDelta = new Vector2(320f, 110f);

            // Monedas
            GameObject coinsObj = new GameObject("Coins_Group");
            coinsObj.transform.SetParent(topRight.transform, false);
            var rtCoins = coinsObj.AddComponent<RectTransform>();
            rtCoins.anchorMin = new Vector2(1f, 1f);
            rtCoins.anchorMax = new Vector2(1f, 1f);
            rtCoins.pivot = new Vector2(1f, 1f);
            rtCoins.anchoredPosition = new Vector2(0f, 0f);
            rtCoins.sizeDelta = new Vector2(180f, 44f);

            GameObject coinIconObj = new GameObject("Coin_Icon");
            coinIconObj.transform.SetParent(coinsObj.transform, false);
            var imgCoin = coinIconObj.AddComponent<Image>();
            imgCoin.sprite = coinSprite;
            imgCoin.preserveAspect = true;
            var rtCI = coinIconObj.GetComponent<RectTransform>();
            rtCI.anchorMin = new Vector2(0f, 0.5f);
            rtCI.anchorMax = new Vector2(0f, 0.5f);
            rtCI.pivot = new Vector2(0f, 0.5f);
            rtCI.anchoredPosition = new Vector2(0f, 0f);
            rtCI.sizeDelta = new Vector2(36f, 36f);

            GameObject coinTextObj = new GameObject("Coins_Text");
            coinTextObj.transform.SetParent(coinsObj.transform, false);
            var rtCT = coinTextObj.AddComponent<RectTransform>();
            rtCT.anchorMin = new Vector2(0f, 0.5f);
            rtCT.anchorMax = new Vector2(1f, 0.5f);
            rtCT.pivot = new Vector2(0f, 0.5f);
            rtCT.anchoredPosition = new Vector2(44f, 0f);
            rtCT.sizeDelta = new Vector2(120f, 40f);

            var tmpCoins = coinTextObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpCoins.font = font;
            tmpCoins.fontSize = 32f;
            tmpCoins.fontStyle = FontStyles.Bold;
            tmpCoins.color = new Color(1f, 0.85f, 0.2f);
            tmpCoins.text = "x 0";
            tmpCoins.alignment = TextAlignmentOptions.MidlineLeft;

            // Altura
            GameObject heightObj = new GameObject("Height_Group");
            heightObj.transform.SetParent(topRight.transform, false);
            var rtHGroup = heightObj.AddComponent<RectTransform>();
            rtHGroup.anchorMin = new Vector2(1f, 1f);
            rtHGroup.anchorMax = new Vector2(1f, 1f);
            rtHGroup.pivot = new Vector2(1f, 1f);
            rtHGroup.anchoredPosition = new Vector2(0f, -48f);
            rtHGroup.sizeDelta = new Vector2(250f, 44f);

            GameObject heightTextObj = new GameObject("Height_Text");
            heightTextObj.transform.SetParent(heightObj.transform, false);
            var rtHT = heightTextObj.AddComponent<RectTransform>();
            rtHT.anchorMin = new Vector2(0f, 0.5f);
            rtHT.anchorMax = new Vector2(1f, 0.5f);
            rtHT.pivot = new Vector2(0f, 0.5f);
            rtHT.anchoredPosition = new Vector2(0f, 0f);
            rtHT.sizeDelta = new Vector2(200f, 40f);

            var tmpHeight = heightTextObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpHeight.font = font;
            tmpHeight.fontSize = 28f;
            tmpHeight.fontStyle = FontStyles.Bold;
            tmpHeight.color = Color.white;
            tmpHeight.text = "HEIGHT: 0m";
            tmpHeight.alignment = TextAlignmentOptions.MidlineRight;

            GameObject heightIconObj = new GameObject("Height_Icon");
            heightIconObj.transform.SetParent(heightObj.transform, false);
            var imgHeight = heightIconObj.AddComponent<Image>();
            imgHeight.sprite = heightSprite;
            imgHeight.preserveAspect = true;
            var rtHI = heightIconObj.GetComponent<RectTransform>();
            rtHI.anchorMin = new Vector2(1f, 0.5f);
            rtHI.anchorMax = new Vector2(1f, 0.5f);
            rtHI.pivot = new Vector2(1f, 0.5f);
            rtHI.anchoredPosition = new Vector2(0f, 0f);
            rtHI.sizeDelta = new Vector2(36f, 36f);

            // 4. Panel de Game Over
            GameObject goPanel = new GameObject("GameOver_Panel");
            goPanel.transform.SetParent(hudCanvas.transform, false);
            var rtGOPanel = goPanel.AddComponent<RectTransform>();
            rtGOPanel.anchorMin = Vector2.zero;
            rtGOPanel.anchorMax = Vector2.one;
            rtGOPanel.offsetMin = Vector2.zero;
            rtGOPanel.offsetMax = Vector2.zero;

            var imgGOPanel = goPanel.AddComponent<Image>();
            imgGOPanel.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

            GameObject goTitleObj = new GameObject("GameOver_Title");
            goTitleObj.transform.SetParent(goPanel.transform, false);
            var rtGOTitle = goTitleObj.AddComponent<RectTransform>();
            rtGOTitle.anchorMin = new Vector2(0.5f, 0.6f);
            rtGOTitle.anchorMax = new Vector2(0.5f, 0.6f);
            rtGOTitle.pivot = new Vector2(0.5f, 0.5f);
            rtGOTitle.sizeDelta = new Vector2(600f, 100f);

            var tmpGOTitle = goTitleObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpGOTitle.font = font;
            tmpGOTitle.fontSize = 64f;
            tmpGOTitle.fontStyle = FontStyles.Bold;
            tmpGOTitle.color = new Color(1f, 0.25f, 0.25f);
            tmpGOTitle.text = "GAME OVER";
            tmpGOTitle.alignment = TextAlignmentOptions.Center;

            GameObject goReasonObj = new GameObject("GameOver_Reason");
            goReasonObj.transform.SetParent(goPanel.transform, false);
            var rtGOReason = goReasonObj.AddComponent<RectTransform>();
            rtGOReason.anchorMin = new Vector2(0.5f, 0.45f);
            rtGOReason.anchorMax = new Vector2(0.5f, 0.45f);
            rtGOReason.pivot = new Vector2(0.5f, 0.5f);
            rtGOReason.sizeDelta = new Vector2(800f, 80f);

            var tmpGOReason = goReasonObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpGOReason.font = font;
            tmpGOReason.fontSize = 28f;
            tmpGOReason.color = Color.white;
            tmpGOReason.text = "¡El tiempo se ha agotado!\nEl pescado en el horno se ha quemado.";
            tmpGOReason.alignment = TextAlignmentOptions.Center;

            goPanel.SetActive(false);

            // 5. Enlazar referencias en HUDManager
            hudManager.Configure(
                heartsParent.transform,
                heartImages,
                heartFull,
                heartEmpty,
                tmpCoins,
                tmpTimer,
                tmpHeight,
                goPanel,
                tmpGOReason,
                baseHeight: -1f
            );

            PrefabUtility.SaveAsPrefabAsset(hudCanvas, "Assets/Prefabs/HUD.prefab");
            DestroyImmediate(hudCanvas);

            AssetDatabase.SaveAssets();
            Debug.Log("[Peakingu] Prefab HUD armado exitosamente en Assets/Prefabs/HUD.prefab.");
        }

        [MenuItem("Tools/Peakingu/9. Integrar HUD y Monedas en Nivel 1")]
        public static void SetupLevel1HUDAndCoins()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Nivel1.unity", OpenSceneMode.Single);

            // Asegurar EventSystem
            var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Asegurar HUD Prefab instanciado
            var existingHUD = GameObject.Find("HUD");
            if (existingHUD != null) DestroyImmediate(existingHUD);

            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HUD.prefab");
            if (hudPrefab == null)
            {
                BuildHUDPrefab();
                hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HUD.prefab");
            }
            if (hudPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(hudPrefab);
            }

            // Asegurar Prefab de Moneda
            var coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coin.prefab");
            if (coinPrefab == null)
            {
                BuildCoinPrefab();
                coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coin.prefab");
            }

            // Colocar monedas en plataformas de hielo
            var existingCoins = GameObject.Find("Coins_Container");
            if (existingCoins != null) DestroyImmediate(existingCoins);

            GameObject coinsContainer = new GameObject("Coins_Container");
            Vector3[] coinPositions = new Vector3[]
            {
                new Vector3(3f, 1.2f, 0f),
                new Vector3(4.5f, 1.2f, 0f),
                new Vector3(6f, 1.2f, 0f),
                new Vector3(10f, 4.2f, 0f),
                new Vector3(11.5f, 4.2f, 0f),
                new Vector3(13f, 4.2f, 0f)
            };

            for (int i = 0; i < coinPositions.Length; i++)
            {
                GameObject coinInstance = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);
                coinInstance.name = $"Coin_{i + 1}";
                coinInstance.transform.position = coinPositions[i];
                coinInstance.transform.SetParent(coinsContainer.transform, true);
            }

            // Asegurar GameManager en escena
            var existingGM = Object.FindAnyObjectByType<GameManager>();
            if (existingGM == null)
            {
                var gmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab");
                if (gmPrefab != null)
                {
                    PrefabUtility.InstantiatePrefab(gmPrefab);
                }
            }

            // Asegurar Checkpoint
            var existingCP = Object.FindAnyObjectByType<Checkpoint>();
            if (existingCP == null)
            {
                var cpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Checkpoint.prefab");
                if (cpPrefab != null)
                {
                    var cpObj = (GameObject)PrefabUtility.InstantiatePrefab(cpPrefab);
                    cpObj.name = "Checkpoint_1";
                    cpObj.transform.position = new Vector3(0f, -2f, 0f);
                }
            }

            // Asegurar KillZone
            var existingKZ = Object.FindAnyObjectByType<KillZone>();
            if (existingKZ == null)
            {
                GameObject kzObj = new GameObject("KillZone");
                kzObj.transform.position = new Vector3(0f, -7f, 0f);
                var kzCol = kzObj.AddComponent<BoxCollider2D>();
                kzCol.isTrigger = true;
                kzCol.size = new Vector2(100f, 2f);
                kzObj.AddComponent<KillZone>();
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Peakingu] Nivel 1 actualizado con HUD (Canvas + TextMeshPro), GameManager, Checkpoint y monedas flotantes interactivas.");
        }

        [MenuItem("Tools/Peakingu/11. Configurar Escena Menu (Canvas, Botones, Controles, Settings, Fade)")]
        public static void SetupMenuScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Single);

            // 1. Configurar Sprites del Menú
            string[] menuSpritePaths = new string[]
            {
                "Assets/Sprites/Button_Bg.png",
                "Assets/Sprites/Panel_Bg.png",
                "Assets/Sprites/Slider_Track.png",
                "Assets/Sprites/Slider_Fill.png",
                "Assets/Sprites/Menu_Background.png"
            };

            foreach (var path in menuSpritePaths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.filterMode = FilterMode.Bilinear;
                    if (path.Contains("Button_Bg")) importer.spriteBorder = new Vector4(8, 8, 8, 8);
                    if (path.Contains("Panel_Bg")) importer.spriteBorder = new Vector4(12, 12, 12, 12);
                    if (path.Contains("Slider")) importer.spriteBorder = new Vector4(4, 4, 4, 4);
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
            AssetDatabase.Refresh();

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Button_Bg.png");
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Panel_Bg.png");
            var menuBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Menu_Background.png");
            var sliderTrackSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Slider_Track.png");
            var sliderFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Slider_Fill.png");

            // 2. Asegurar EventSystem
            var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 3. Limpiar Canvas previo si existe
            var existingCanvas = GameObject.Find("Canvas_Menu");
            if (existingCanvas != null) DestroyImmediate(existingCanvas);

            // 4. Canvas Principal
            GameObject canvasMenu = new GameObject("Canvas_Menu");
            var canvas = canvasMenu.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasMenu.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasMenu.AddComponent<GraphicRaycaster>();
            var menuManager = canvasMenu.AddComponent<MenuManager>();

            // 5. Fondo Panorámico Artico
            GameObject bgObj = new GameObject("Background_Image");
            bgObj.transform.SetParent(canvasMenu.transform, false);
            var rtBg = bgObj.AddComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.offsetMin = Vector2.zero;
            rtBg.offsetMax = Vector2.zero;
            var imgBg = bgObj.AddComponent<Image>();
            imgBg.sprite = menuBgSprite;

            // 6. Logo y Título "PEAKINGU" (Alineado arriba al centro-izquierda)
            GameObject logoObj = new GameObject("Logo_Title");
            logoObj.transform.SetParent(canvasMenu.transform, false);
            var rtLogo = logoObj.AddComponent<RectTransform>();
            rtLogo.anchorMin = new Vector2(0.5f, 0.82f);
            rtLogo.anchorMax = new Vector2(0.5f, 0.82f);
            rtLogo.pivot = new Vector2(0.5f, 0.5f);
            rtLogo.anchoredPosition = new Vector2(-200f, 0f);
            rtLogo.sizeDelta = new Vector2(700f, 140f);

            var tmpLogo = logoObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpLogo.font = font;
            tmpLogo.text = "PEAKINGU";
            tmpLogo.fontSize = 92f;
            tmpLogo.fontStyle = FontStyles.Bold;
            tmpLogo.color = new Color(0.12f, 0.85f, 1f);
            tmpLogo.alignment = TextAlignmentOptions.Center;

            GameObject subObj = new GameObject("Sub_Title");
            subObj.transform.SetParent(logoObj.transform, false);
            var rtSub = subObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 0f);
            rtSub.anchorMax = new Vector2(0.5f, 0f);
            rtSub.pivot = new Vector2(0.5f, 1f);
            rtSub.anchoredPosition = new Vector2(0f, -8f);
            rtSub.sizeDelta = new Vector2(600f, 40f);

            var tmpSub = subObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpSub.font = font;
            tmpSub.text = "¡Sube la montaña antes de que el pescado se queme!";
            tmpSub.fontSize = 24f;
            tmpSub.fontStyle = FontStyles.Italic;
            tmpSub.color = new Color(0.85f, 0.95f, 1f, 0.95f);
            tmpSub.alignment = TextAlignmentOptions.Center;

            // 7. Panel Principal de Botones (Alineado a la derecha como en el Mockup 1)
            GameObject mainPanel = new GameObject("MainMenu_Panel");
            mainPanel.transform.SetParent(canvasMenu.transform, false);
            var rtMain = mainPanel.AddComponent<RectTransform>();
            rtMain.anchorMin = new Vector2(0.5f, 0.5f);
            rtMain.anchorMax = new Vector2(0.5f, 0.5f);
            rtMain.pivot = new Vector2(0.5f, 0.5f);
            rtMain.anchoredPosition = new Vector2(460f, -60f);
            rtMain.sizeDelta = new Vector2(380f, 420f);

            var vLayout = mainPanel.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 18f;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = false;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = false;
            vLayout.childForceExpandHeight = false;

            Button btnStart = CreateMenuButton(mainPanel.transform, "Btn_Iniciar", "INICIAR", buttonSprite, font, 340f, 68f, 32f);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnStart.onClick, menuManager.OnStartClicked);

            Button btnLoad = CreateMenuButton(mainPanel.transform, "Btn_Cargar", "CARGAR PARTIDA", buttonSprite, font, 340f, 68f, 26f);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnLoad.onClick, menuManager.OnLoadGameClicked);

            Button btnControls = CreateMenuButton(mainPanel.transform, "Btn_Controles", "CONTROLES", buttonSprite, font, 340f, 68f, 28f);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnControls.onClick, menuManager.OnControlsClicked);

            Button btnSettings = CreateMenuButton(mainPanel.transform, "Btn_Configuracion", "CONFIGURACIÓN", buttonSprite, font, 340f, 68f, 26f);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnSettings.onClick, menuManager.OnSettingsClicked);

            // 8. Panel de Controles
            GameObject controlsPanel = new GameObject("Controls_Panel");
            controlsPanel.transform.SetParent(canvasMenu.transform, false);
            var rtCtrl = controlsPanel.AddComponent<RectTransform>();
            rtCtrl.anchorMin = new Vector2(0.5f, 0.5f);
            rtCtrl.anchorMax = new Vector2(0.5f, 0.5f);
            rtCtrl.pivot = new Vector2(0.5f, 0.5f);
            rtCtrl.anchoredPosition = Vector2.zero;
            rtCtrl.sizeDelta = new Vector2(740f, 560f);

            var imgCtrl = controlsPanel.AddComponent<Image>();
            imgCtrl.sprite = panelSprite;
            imgCtrl.type = Image.Type.Sliced;

            GameObject ctrlTitleObj = new GameObject("Title_Controles");
            ctrlTitleObj.transform.SetParent(controlsPanel.transform, false);
            var rtCtrlTitle = ctrlTitleObj.AddComponent<RectTransform>();
            rtCtrlTitle.anchorMin = new Vector2(0.5f, 1f);
            rtCtrlTitle.anchorMax = new Vector2(0.5f, 1f);
            rtCtrlTitle.pivot = new Vector2(0.5f, 1f);
            rtCtrlTitle.anchoredPosition = new Vector2(0f, -25f);
            rtCtrlTitle.sizeDelta = new Vector2(600f, 60f);

            var tmpCtrlTitle = ctrlTitleObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpCtrlTitle.font = font;
            tmpCtrlTitle.text = "CONTROLES";
            tmpCtrlTitle.fontSize = 42f;
            tmpCtrlTitle.fontStyle = FontStyles.Bold;
            tmpCtrlTitle.color = new Color(0.25f, 0.85f, 1f);
            tmpCtrlTitle.alignment = TextAlignmentOptions.Center;

            GameObject ctrlContentObj = new GameObject("Content_Controles");
            ctrlContentObj.transform.SetParent(controlsPanel.transform, false);
            var rtCtrlContent = ctrlContentObj.AddComponent<RectTransform>();
            rtCtrlContent.anchorMin = new Vector2(0.5f, 0.5f);
            rtCtrlContent.anchorMax = new Vector2(0.5f, 0.5f);
            rtCtrlContent.pivot = new Vector2(0.5f, 0.5f);
            rtCtrlContent.anchoredPosition = new Vector2(0f, -10f);
            rtCtrlContent.sizeDelta = new Vector2(640f, 320f);

            var tmpCtrlContent = ctrlContentObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpCtrlContent.font = font;
            tmpCtrlContent.text = "<b>W, A, S, D / Flechas:</b> Moverse horizontalmente\n" +
                                  "<b>Espacio:</b> Saltar plataformas\n" +
                                  "<b>F:</b> Impulso Dash horizontal\n" +
                                  "<b>R:</b> Mano con Gancho Cyborg\n" +
                                  "<b>E:</b> Interactuar con Mercader / Tienda\n" +
                                  "<b>ESC:</b> Menú de Pausa / Configuración";
            tmpCtrlContent.fontSize = 25f;
            tmpCtrlContent.lineSpacing = 16f;
            tmpCtrlContent.color = Color.white;
            tmpCtrlContent.alignment = TextAlignmentOptions.Left;

            Button btnBackCtrl = CreateMenuButton(controlsPanel.transform, "Btn_Volver_Controles", "VOLVER", buttonSprite, font, 240f, 54f, 24f);
            var rtBackCtrl = btnBackCtrl.GetComponent<RectTransform>();
            rtBackCtrl.anchorMin = new Vector2(0.5f, 0f);
            rtBackCtrl.anchorMax = new Vector2(0.5f, 0f);
            rtBackCtrl.pivot = new Vector2(0.5f, 0f);
            rtBackCtrl.anchoredPosition = new Vector2(0f, 25f);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnBackCtrl.onClick, menuManager.OnBackToMainMenuClicked);

            controlsPanel.SetActive(false);

            // 9. Panel de Configuración
            GameObject settingsPanel = new GameObject("Settings_Panel");
            settingsPanel.transform.SetParent(canvasMenu.transform, false);
            var rtSet = settingsPanel.AddComponent<RectTransform>();
            rtSet.anchorMin = new Vector2(0.5f, 0.5f);
            rtSet.anchorMax = new Vector2(0.5f, 0.5f);
            rtSet.pivot = new Vector2(0.5f, 0.5f);
            rtSet.anchoredPosition = Vector2.zero;
            rtSet.sizeDelta = new Vector2(680f, 460f);

            var imgSet = settingsPanel.AddComponent<Image>();
            imgSet.sprite = panelSprite;
            imgSet.type = Image.Type.Sliced;

            GameObject setTitleObj = new GameObject("Title_Configuracion");
            setTitleObj.transform.SetParent(settingsPanel.transform, false);
            var rtSetTitle = setTitleObj.AddComponent<RectTransform>();
            rtSetTitle.anchorMin = new Vector2(0.5f, 1f);
            rtSetTitle.anchorMax = new Vector2(0.5f, 1f);
            rtSetTitle.pivot = new Vector2(0.5f, 1f);
            rtSetTitle.anchoredPosition = new Vector2(0f, -25f);
            rtSetTitle.sizeDelta = new Vector2(600f, 60f);

            var tmpSetTitle = setTitleObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpSetTitle.font = font;
            tmpSetTitle.text = "CONFIGURACIÓN";
            tmpSetTitle.fontSize = 42f;
            tmpSetTitle.fontStyle = FontStyles.Bold;
            tmpSetTitle.color = new Color(0.25f, 0.85f, 1f);
            tmpSetTitle.alignment = TextAlignmentOptions.Center;

            // Etiqueta Volumen
            GameObject volLabelObj = new GameObject("Label_Volumen");
            volLabelObj.transform.SetParent(settingsPanel.transform, false);
            var rtVolLabel = volLabelObj.AddComponent<RectTransform>();
            rtVolLabel.anchorMin = new Vector2(0.5f, 0.65f);
            rtVolLabel.anchorMax = new Vector2(0.5f, 0.65f);
            rtVolLabel.pivot = new Vector2(0.5f, 0.5f);
            rtVolLabel.anchoredPosition = new Vector2(-150f, 0f);
            rtVolLabel.sizeDelta = new Vector2(280f, 40f);

            var tmpVolLabel = volLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpVolLabel.font = font;
            tmpVolLabel.text = "Volumen Maestro:";
            tmpVolLabel.fontSize = 26f;
            tmpVolLabel.fontStyle = FontStyles.Bold;
            tmpVolLabel.color = Color.white;
            tmpVolLabel.alignment = TextAlignmentOptions.Left;

            // Texto Porcentaje
            GameObject volValObj = new GameObject("Text_Porcentaje");
            volValObj.transform.SetParent(settingsPanel.transform, false);
            var rtVolVal = volValObj.AddComponent<RectTransform>();
            rtVolVal.anchorMin = new Vector2(0.5f, 0.65f);
            rtVolVal.anchorMax = new Vector2(0.5f, 0.65f);
            rtVolVal.pivot = new Vector2(0.5f, 0.5f);
            rtVolVal.anchoredPosition = new Vector2(170f, 0f);
            rtVolVal.sizeDelta = new Vector2(100f, 40f);

            var tmpVolVal = volValObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpVolVal.font = font;
            tmpVolVal.text = "100%";
            tmpVolVal.fontSize = 26f;
            tmpVolVal.fontStyle = FontStyles.Bold;
            tmpVolVal.color = new Color(1f, 0.85f, 0.2f);
            tmpVolVal.alignment = TextAlignmentOptions.Right;

            // Slider de Volumen
            GameObject sliderObj = new GameObject("Slider_Volumen");
            sliderObj.transform.SetParent(settingsPanel.transform, false);
            var rtSlider = sliderObj.AddComponent<RectTransform>();
            rtSlider.anchorMin = new Vector2(0.5f, 0.50f);
            rtSlider.anchorMax = new Vector2(0.5f, 0.50f);
            rtSlider.pivot = new Vector2(0.5f, 0.5f);
            rtSlider.anchoredPosition = Vector2.zero;
            rtSlider.sizeDelta = new Vector2(460f, 28f);

            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Background del slider
            GameObject sliderBgObj = new GameObject("Background");
            sliderBgObj.transform.SetParent(sliderObj.transform, false);
            var rtSBg = sliderBgObj.AddComponent<RectTransform>();
            rtSBg.anchorMin = Vector2.zero;
            rtSBg.anchorMax = Vector2.one;
            rtSBg.offsetMin = Vector2.zero;
            rtSBg.offsetMax = Vector2.zero;
            var imgSBg = sliderBgObj.AddComponent<Image>();
            imgSBg.sprite = sliderTrackSprite;
            imgSBg.type = Image.Type.Sliced;

            // Fill Area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            var rtFA = fillAreaObj.AddComponent<RectTransform>();
            rtFA.anchorMin = Vector2.zero;
            rtFA.anchorMax = Vector2.one;
            rtFA.offsetMin = new Vector2(4, 0);
            rtFA.offsetMax = new Vector2(-4, 0);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            var rtFill = fillObj.AddComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.offsetMin = Vector2.zero;
            rtFill.offsetMax = Vector2.zero;
            var imgFill = fillObj.AddComponent<Image>();
            imgFill.sprite = sliderFillSprite;
            imgFill.type = Image.Type.Sliced;

            slider.targetGraphic = imgFill;
            slider.fillRect = rtFill;

            Button btnBackSet = CreateMenuButton(settingsPanel.transform, "Btn_Volver_Config", "VOLVER", buttonSprite, font, 240f, 54f, 24f);
            var rtBackSet = btnBackSet.GetComponent<RectTransform>();
            rtBackSet.anchorMin = new Vector2(0.5f, 0f);
            rtBackSet.anchorMax = new Vector2(0.5f, 0f);
            rtBackSet.pivot = new Vector2(0.5f, 0f);
            rtBackSet.anchoredPosition = new Vector2(0f, 25f);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnBackSet.onClick, menuManager.OnBackToMainMenuClicked);

            settingsPanel.SetActive(false);

            // 10. Panel de Fundido a Negro (Fade Overlay)
            GameObject fadeObj = new GameObject("Fade_Overlay");
            fadeObj.transform.SetParent(canvasMenu.transform, false);
            var rtFade = fadeObj.AddComponent<RectTransform>();
            rtFade.anchorMin = Vector2.zero;
            rtFade.anchorMax = Vector2.one;
            rtFade.offsetMin = Vector2.zero;
            rtFade.offsetMax = Vector2.zero;

            var imgFade = fadeObj.AddComponent<Image>();
            imgFade.color = Color.black;

            var fadeGroup = fadeObj.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;

            // 11. Asignar referencias al MenuManager
            menuManager.SetupInEditor(
                fadeGroup,
                mainPanel,
                controlsPanel,
                settingsPanel,
                slider,
                tmpVolVal,
                "Nivel1"
            );

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Peakingu] Escena Menu configurada con éxito: Canvas, Botones Iniciar/Cargar/Controles/Settings, Slider de volumen y Transición de fundido.");
        }

        private static Button CreateMenuButton(
            Transform parent,
            string gameObjectName,
            string buttonText,
            Sprite bgSprite,
            TMP_FontAsset font,
            float width = 340f,
            float height = 65f,
            float fontSize = 28f)
        {
            GameObject btnObj = new GameObject(gameObjectName);
            btnObj.transform.SetParent(parent, false);

            var rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var img = btnObj.AddComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 0.95f, 1f);
            colors.pressedColor = new Color(0.6f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var rtText = textObj.AddComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.offsetMin = Vector2.zero;
            rtText.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = buttonText;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.05f, 0.15f, 0.3f); // Azul marino profundo
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        [MenuItem("Tools/Peakingu/12. Ejecutar Configuracion Completa de Peakingu")]
        public static void RunFullPipeline()
        {
            SetupProjectSettings();
            SetupScenesWithURP2D();
            BuildPinguPrefab();
            SetupLevel1EnvironmentAndCamera();
            ConfigureUISprites();
            BuildCoinPrefab();
            BuildHUDPrefab();
            SetupLevel1HUDAndCoins();
            SetupMenuScene();
            Debug.Log("[Peakingu] ¡Pipeline completo de Peakingu ejecutado con éxito total!");
        }
    }
}
#endif
