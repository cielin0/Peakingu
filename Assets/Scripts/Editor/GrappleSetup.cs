using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Peakingu.EditorTools
{
    /// <summary>
    /// Herramienta de editor para configurar el sistema de Gancho Cyborg.
    /// Pasos:
    ///   1. Crea la capa "GrapplePoint" si no existe.
    ///   2. Crea el Sprite del punto de agarre.
    ///   3. Crea el Prefab GrapplePoint.
    ///   4. Añade GrappleController al Prefab de Pingu.
    ///   5. Instancia puntos de agarre en la escena Cueva.
    /// </summary>
    public static class GrappleSetup
    {
        private const string PREFAB_PATH = "Assets/Prefabs/GrapplePoint.prefab";
        private const string SPRITE_PATH = "Assets/Sprites/GrapplePoint.png";
        private const string LAYER_NAME  = "GrapplePoint";

        [MenuItem("Tools/Peakingu/15. Configurar Gancho Cyborg (GrapplePoint + GrappleController)")]
        public static void SetupGrapple()
        {
            try
            {
                // 1. Capa
                EnsureGrappleLayer();

                // 2. Sprite
                EnsureGrappleSprite();

                // 3. Prefab GrapplePoint
                var gpPrefab = CreateGrapplePointPrefab();

                // 4. GrappleController en Pingu.prefab
                AddGrappleControllerToPingu();

                // 5. Instanciar puntos en Cueva.unity
                PlaceGrapplePointsInCueva(gpPrefab);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[GrappleSetup] ✓ Gancho Cyborg configurado: prefab, capa, GrappleController en Pingu y puntos en Cueva.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GrappleSetup] Error: {ex}");
            }
        }

        // ─── Paso 1: Capa ────────────────────────────────────────────────────────

        private static void EnsureGrappleLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            // Comprobar si ya existe
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                if (layersProp.GetArrayElementAtIndex(i).stringValue == LAYER_NAME)
                {
                    Debug.Log($"[GrappleSetup] Capa '{LAYER_NAME}' ya existe en slot {i}.");
                    return;
                }
            }

            // Buscar el primer slot libre (a partir del 12 para no pisar las capas del proyecto)
            for (int i = 12; i < Mathf.Min(31, layersProp.arraySize); i++)
            {
                if (string.IsNullOrEmpty(layersProp.GetArrayElementAtIndex(i).stringValue))
                {
                    layersProp.GetArrayElementAtIndex(i).stringValue = LAYER_NAME;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[GrappleSetup] Capa '{LAYER_NAME}' creada en slot {i}.");
                    return;
                }
            }

            Debug.LogWarning($"[GrappleSetup] No se encontró slot libre para la capa '{LAYER_NAME}'. Asígnala manualmente.");
        }

        // ─── Paso 2: Sprite ──────────────────────────────────────────────────────

        private static void EnsureGrappleSprite()
        {
            if (File.Exists(SPRITE_PATH)) return;

            string dir = Path.GetDirectoryName(SPRITE_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Anillo de 32×32 pixel art que representa el punto de agarre
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            int cx = size / 2, cy = size / 2;
            int outerR = 14, innerR = 9;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= outerR && dist >= innerR)
                    {
                        // Gradiente de color celeste → azul según el ángulo
                        float t = Mathf.Clamp01((dist - innerR) / (outerR - innerR));
                        Color c = Color.Lerp(
                            new Color(0.4f, 0.95f, 1f, 1f),   // celeste interior
                            new Color(0.1f, 0.55f, 0.9f, 0.9f) // azul exterior
                        , t);

                        // Borde exterior más oscuro
                        if (dist >= outerR - 1.5f)
                            c = new Color(0.05f, 0.3f, 0.6f, 1f);

                        tex.SetPixel(x, y, c);
                    }
                    // Cruz interior (mira de puntería)
                    else if ((Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) <= innerR - 1f) ||
                             (Mathf.Abs(dy) <= 1 && Mathf.Abs(dx) <= innerR - 1f))
                    {
                        if (dist < innerR)
                            tex.SetPixel(x, y, new Color(0.6f, 0.95f, 1f, 0.7f));
                    }
                }
            }

            // Brillo superior izquierdo
            tex.SetPixel(cx - 4, cy + 8, new Color(1f, 1f, 1f, 0.8f));
            tex.SetPixel(cx - 5, cy + 7, new Color(1f, 1f, 1f, 0.5f));

            tex.Apply();
            File.WriteAllBytes(SPRITE_PATH, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(SPRITE_PATH);
            var importer = AssetImporter.GetAtPath(SPRITE_PATH) as TextureImporter;
            if (importer != null)
            {
                importer.textureType     = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode      = FilterMode.Point;
                importer.spritePivot     = new Vector2(0.5f, 0.5f);
                importer.SaveAndReimport();
            }

            Debug.Log($"[GrappleSetup] Sprite GrapplePoint creado en '{SPRITE_PATH}'.");
        }

        // ─── Paso 3: Prefab ──────────────────────────────────────────────────────

        private static GameObject CreateGrapplePointPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
            int gpLayer = LayerMask.NameToLayer(LAYER_NAME);
            if (gpLayer == -1) gpLayer = 0; // Fallback a Default si la capa aún no está registrada

            var obj = new GameObject("GrapplePoint");
            obj.tag   = "Untagged";
            obj.layer = gpLayer;

            // SpriteRenderer
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite           = sprite;
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder     = 10;
            sr.color            = new Color(0.4f, 0.9f, 1f, 0.9f);

            // Rigidbody2D estático (el joint necesita un Rigidbody2D en el anchor)
            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType     = RigidbodyType2D.Static;
            rb.gravityScale = 0f;

            // CircleCollider2D de trigger (solo detección, no físicas sólidas)
            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.4f;

            // Componente lógico
            obj.AddComponent<GrapplePoint>();

            // Guardar prefab
            if (!Directory.Exists("Assets/Prefabs"))
                Directory.CreateDirectory("Assets/Prefabs");

            var prefab = PrefabUtility.SaveAsPrefabAsset(obj, PREFAB_PATH);
            Object.DestroyImmediate(obj);

            Debug.Log($"[GrappleSetup] Prefab GrapplePoint guardado en '{PREFAB_PATH}'.");
            return prefab;
        }

        // ─── Paso 4: GrappleController en Pingu.prefab ───────────────────────────

        private static void AddGrappleControllerToPingu()
        {
            const string PINGU_PATH = "Assets/Prefabs/Pingu.prefab";
            var pinguPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PINGU_PATH);
            if (pinguPrefab == null)
            {
                Debug.LogWarning("[GrappleSetup] No se encontró 'Assets/Prefabs/Pingu.prefab'. Añade GrappleController manualmente.");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(PINGU_PATH))
            {
                var root = scope.prefabContentsRoot;

                // GrappleController (requiere LineRenderer en el mismo GameObject)
                if (root.GetComponent<GrappleController>() == null)
                {
                    // LineRenderer (requerido por GrappleController)
                    if (root.GetComponent<LineRenderer>() == null)
                    {
                        var lr = root.AddComponent<LineRenderer>();
                        lr.enabled = false;
                        lr.positionCount = 2;
                        lr.startWidth = 0.06f;
                        lr.endWidth   = 0.06f;
                        lr.useWorldSpace = true;
                        lr.material = new Material(Shader.Find("Sprites/Default"));
                        lr.material.color = new Color(0.6f, 0.85f, 1f);
                        lr.sortingLayerName = "Foreground";
                        lr.sortingOrder = 20;
                    }

                    root.AddComponent<GrappleController>();
                    Debug.Log("[GrappleSetup] GrappleController añadido a Pingu.prefab.");
                }
                else
                {
                    Debug.Log("[GrappleSetup] GrappleController ya existe en Pingu.prefab.");
                }
            }

            AssetDatabase.SaveAssetIfDirty(pinguPrefab);
        }

        // ─── Paso 5: Instanciar GrapplePoints en Cueva ───────────────────────────

        private static void PlaceGrapplePointsInCueva(GameObject gpPrefab)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Cueva.unity", OpenSceneMode.Single);

            // Limpiar puntos previos
            var container = GameObject.Find("GrapplePoints_Container");
            if (container != null) Object.DestroyImmediate(container);

            container = new GameObject("GrapplePoints_Container");

            // Posiciones estratégicas para la Cueva:
            // Techo del nivel (y ≈ 6.5), sobre plataformas y en zonas de riesgo
            Vector3[] positions = new Vector3[]
            {
                // Entrada de la cueva
                new Vector3(-8f,  6.5f, 0f),
                new Vector3(-2f,  6.5f, 0f),

                // Sobre Plataforma 1 (x=2..7, y=0)
                new Vector3(2.5f, 6.5f, 0f),
                new Vector3(6f,   6.5f, 0f),

                // Entre plataformas 1-2
                new Vector3(10f,  6.5f, 0f),

                // Sobre Plataforma 2 (x=12..17, y=2)
                new Vector3(13f,  6.5f, 0f),
                new Vector3(16.5f,6.5f, 0f),

                // Entre plataformas 2-3
                new Vector3(20f,  6.5f, 0f),

                // Sobre Plataforma 3 (x=22..27, y=1)
                new Vector3(23f,  6.5f, 0f),
                new Vector3(26f,  6.5f, 0f),

                // Entre plataformas 3-4
                new Vector3(31f,  6.5f, 0f),

                // Sobre Plataforma 4 (x=32..37, y=0)
                new Vector3(33f,  6.5f, 0f),
                new Vector3(36.5f,6.5f, 0f),

                // Final del nivel
                new Vector3(42f,  6.5f, 0f),
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var gp = (GameObject)PrefabUtility.InstantiatePrefab(gpPrefab);
                gp.name = $"GrapplePoint_{i + 1}";
                gp.transform.position = positions[i];
                gp.transform.SetParent(container.transform, true);
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[GrappleSetup] {positions.Length} GrapplePoints colocados en Cueva.unity.");
        }
    }
}
