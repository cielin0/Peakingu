using System.IO;
using UnityEngine;
using UnityEditor;

namespace Peakingu.EditorTools
{
    public static class NewSpritesImportSetup
    {
        private const string ANIM_DIR = "Assets/Sprites/Clean/Anim";

        [MenuItem("Tools/Peakingu/Configurar Sprites Nuevos")]
        public static void ConfigureNewSprites()
        {
            int configuredCount = 0;

            try
            {
                // 1. Decor: casa (128 PPU, Point, BottomCenter)
                if (ConfigureSprite(
                    "Assets/Sprites/Clean/Decor/casa.png",
                    128f,
                    FilterMode.Point,
                    SpriteAlignment.BottomCenter,
                    new Vector2(0.5f, 0f),
                    2048))
                {
                    configuredCount++;
                }

                // 2. Decor: pescados (128 PPU, Point, Center)
                string[] fishPaths =
                {
                    "Assets/Sprites/Clean/Decor/pescado_bueno.png",
                    "Assets/Sprites/Clean/Decor/pescado_quemado.png"
                };
                foreach (var path in fishPaths)
                {
                    if (ConfigureSprite(
                        path,
                        128f,
                        FilterMode.Point,
                        SpriteAlignment.Center,
                        new Vector2(0.5f, 0.5f),
                        2048))
                    {
                        configuredCount++;
                    }
                }

                // 3. Anim: humo_negro_* y vapor_blanco_* (128 PPU, Point, BottomCenter)
                if (Directory.Exists(ANIM_DIR))
                {
                    string[] animFiles = Directory.GetFiles(ANIM_DIR, "*.png");
                    if (animFiles.Length == 0)
                    {
                        Debug.LogWarning($"[NewSpritesImportSetup] No se encontraron archivos .png en '{ANIM_DIR}'.");
                    }

                    foreach (var file in animFiles)
                    {
                        string assetPath = file.Replace("\\", "/");
                        if (ConfigureSprite(
                            assetPath,
                            128f,
                            FilterMode.Point,
                            SpriteAlignment.BottomCenter,
                            new Vector2(0.5f, 0f),
                            2048))
                        {
                            configuredCount++;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[NewSpritesImportSetup] No existe la carpeta '{ANIM_DIR}'.");
                }

                // 4. Background: cima e interior_casa (100 PPU, Bilinear, Center, Max Size 4096)
                string[] bgPaths =
                {
                    "Assets/Sprites/Clean/Background/cima.png",
                    "Assets/Sprites/Clean/Background/interior_casa.png"
                };
                foreach (var path in bgPaths)
                {
                    if (ConfigureSprite(
                        path,
                        100f,
                        FilterMode.Bilinear,
                        SpriteAlignment.Center,
                        new Vector2(0.5f, 0.5f),
                        4096))
                    {
                        configuredCount++;
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[NewSpritesImportSetup] SUCCESS: Se configuraron y reimportaron {configuredCount} sprites nuevos.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NewSpritesImportSetup] Error al configurar sprites nuevos: {ex}");
            }
        }

        private static bool ConfigureSprite(
            string assetPath,
            float pixelsPerUnit,
            FilterMode filterMode,
            SpriteAlignment alignment,
            Vector2 pivot,
            int maxTextureSize)
        {
            if (!File.Exists(assetPath))
            {
                Debug.LogWarning($"[NewSpritesImportSetup] Archivo no encontrado (se omite): {assetPath}");
                return false;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[NewSpritesImportSetup] No se pudo obtener TextureImporter para: {assetPath}");
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxTextureSize;
            importer.alphaIsTransparency = true;
            importer.filterMode = filterMode;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = pixelsPerUnit;
            settings.spriteAlignment = (int)alignment;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.alphaIsTransparency = true;
            settings.filterMode = filterMode;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
            return true;
        }
    }
}
