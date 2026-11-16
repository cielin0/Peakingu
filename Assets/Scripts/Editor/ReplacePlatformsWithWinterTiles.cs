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

            // 5. Plataforma 4 (Paso intermedio, x = 17 a 21, y = 5, 4)
            for (int x = 17; x <= 21; x++)
            {
                tm.SetTile(new Vector3Int(x, 5, 0), ruleTile);
                tm.SetTile(new Vector3Int(x, 4, 0), ruleTile);
            }

            // 6. Plataforma 5 (Meseta alta, x = 24 a 28, y = 7, 6)
            for (int x = 24; x <= 28; x++)
            {
                tm.SetTile(new Vector3Int(x, 7, 0), ruleTile);
                tm.SetTile(new Vector3Int(x, 6, 0), ruleTile);
            }

            // 7. Plataforma 6 (Descenso, x = 31 a 35, y = 5, 4)
            for (int x = 31; x <= 35; x++)
            {
                tm.SetTile(new Vector3Int(x, 5, 0), ruleTile);
                tm.SetTile(new Vector3Int(x, 4, 0), ruleTile);
            }

            // 8. Plataforma 7 (Aproximación, x = 38 a 42, y = 3, 2)
            for (int x = 38; x <= 42; x++)
            {
                tm.SetTile(new Vector3Int(x, 3, 0), ruleTile);
                tm.SetTile(new Vector3Int(x, 2, 0), ruleTile);
            }

            // 9. Plataforma 8 (Plataforma de Meta, x = 45 a 50, y = 1, 0)
            for (int x = 45; x <= 50; x++)
            {
                tm.SetTile(new Vector3Int(x, 1, 0), ruleTile);
                tm.SetTile(new Vector3Int(x, 0, 0), ruleTile);
            }

            // Eliminar legacy Victory_Goal_Summit si existe en la escena
            var legacyVictory = GameObject.Find("Victory_Goal_Summit");
            if (legacyVictory != null)
            {
                Object.DestroyImmediate(legacyVictory);
                Debug.Log($"[ReplacePlatforms] Legacy Victory_Goal_Summit eliminado de '{Path.GetFileName(scenePath)}'.");
            }

            // Asegurar que LevelGoal repose sobre Plataforma 8
            var goalObj = GameObject.Find("LevelGoal_LaderasBajas") ?? GameObject.Find("LevelGoal_RutaHelada") ?? GameObject.Find("LevelGoal");
            if (goalObj != null)
            {
                goalObj.transform.position = new Vector3(47.5f, 1.0f, 0f);
                var col = goalObj.GetComponent<Collider2D>();
                if (col != null) col.isTrigger = true;
                EditorUtility.SetDirty(goalObj);
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
