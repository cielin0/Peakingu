using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Peakingu.EditorTools
{
    public static class ReplacePlatformsWithWinterTiles
    {
        private const string SUELO_RULE_TILE_PATH = "Assets/Tilemaps/Suelo_RuleTile.asset";

        private static readonly string[] TargetScenes = new string[]
        {
            "Assets/Scenes/Nivel1.unity",
            "Assets/Scenes/Nivel2.unity",
            "Assets/Scenes/Nivel4.unity",
            "Assets/Scenes/Nivel5.unity"
        };

        [MenuItem("Tools/Peakingu/22. Reemplazar Plataformas Verdes con Tiles de Invierno")]
        public static void ReplaceGreenPlatforms()
        {
            try
            {
                var ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(SUELO_RULE_TILE_PATH);
                if (ruleTile == null)
                {
                    Debug.LogError($"[ReplacePlatforms] No se encontró el Rule Tile en '{SUELO_RULE_TILE_PATH}'.");
                    return;
                }

                foreach (var scenePath in TargetScenes)
                {
                    if (File.Exists(scenePath))
                    {
                        ReplacePlatformsInScene(scenePath, ruleTile);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[ReplacePlatforms] SUCCESS: Plataformas y suelo verde reemplazados exitosamente por plataformas de 2 filas con el Rule Tile de invierno.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ReplacePlatforms] Error: {ex}");
            }
        }

        public static void ReplacePlatformsInScene(string scenePath, RuleTile ruleTile)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var groundObj = GameObject.Find("Grid/Ground");
            if (groundObj == null) groundObj = GameObject.Find("Ground");
            if (groundObj == null)
            {
                Debug.LogWarning($"[ReplacePlatforms] No se encontró Ground en '{scenePath}'.");
                return;
            }

            var tm = groundObj.GetComponent<Tilemap>();
            if (tm == null)
            {
                Debug.LogWarning($"[ReplacePlatforms] No se encontró Tilemap en Ground en '{scenePath}'.");
                return;
            }

            // Limpiar los tiles verdes temporales
            tm.ClearAllTiles();

            // 1. Suelo base continuo de 2 filas de grosor (x = -15 a 50)
            // Fila superior: y = -3 (suelo_borde_sup y esquinas)
            // Fila inferior: y = -4 (suelo_borde_inf y esquinas)
            for (int x = -15; x <= 50; x++)
            {
                tm.SetTile(new Vector3Int(x, -3, 0), ruleTile);
                tm.SetTile(new Vector3Int(x, -4, 0), ruleTile);
            }

            // 2. Plataforma 1 (Baja / Inicial, x = -5 a -2)
            // 2 filas de grosor: Fila superior en y = 0, Fila inferior en y = -1
            // Deja aire debajo (y = -2) antes del suelo (y = -3)
            for (int x = -5; x <= -2; x++)
            {
                tm.SetTile(new Vector3Int(x, 0, 0), ruleTile);
                tm.SetTile(new Vector3Int(x, -1, 0), ruleTile);
            }

            // 3. Plataforma 2 (Media, x = 2 a 7)
            // 2 filas de grosor: Fila superior en y = 0, Fila inferior en y = -1
            // Deja aire debajo (y = -2) antes del suelo (y = -3)
            for (int x = 2; x <= 7; x++)
            {
                tm.SetTile(new Vector3Int(x, 0, 0), ruleTile);
                tm.SetTile(new Vector3Int(x, -1, 0), ruleTile);
            }

            // 4. Plataforma 3 (Alta / Mercader NPC, x = 9 a 14)
            // 2 filas de grosor: Fila superior en y = 3, Fila inferior en y = 2
            // Deja aire debajo antes del suelo
            for (int x = 9; x <= 14; x++)
            {
                tm.SetTile(new Vector3Int(x, 3, 0), ruleTile);
                tm.SetTile(new Vector3Int(x, 2, 0), ruleTile);
            }

            // Forzar actualización inmediata del tilemap y de la física compuesta
            tm.RefreshAllTiles();
            var tmCollider = groundObj.GetComponent<TilemapCollider2D>();
            if (tmCollider != null)
            {
                tmCollider.ProcessTilemapChanges();
            }

            EditorUtility.SetDirty(tm);
            EditorUtility.SetDirty(groundObj);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ReplacePlatforms] Escena '{Path.GetFileName(scenePath)}' actualizada con plataformas de 2 filas de grosor.");
        }
    }
}
