#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;

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
    }
}
#endif
