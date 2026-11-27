using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace Peakingu.EditorTools
{
    public static class FinalSceneBuilder
    {
        private const string FINAL_SCENE_PATH = "Assets/Scenes/Final.unity";
        private const string PINGU_PREFAB_PATH = "Assets/Prefabs/Pingu.prefab";
        private const string GAMEMANAGER_PREFAB_PATH = "Assets/Prefabs/GameManager.prefab";
        private const string HUD_PREFAB_PATH = "Assets/Prefabs/HUD.prefab";

        private const string INTERIOR_BG_PATH = "Assets/Sprites/Clean/Background/interior_casa.png";
        private const string GOOD_FISH_PATH = "Assets/Sprites/Clean/Decor/pescado_bueno.png";
        private const string BURNT_FISH_PATH = "Assets/Sprites/Clean/Decor/pescado_quemado.png";

        private static readonly Vector3 PINGU_START_POS = new Vector3(-7.5f, -2.5f, 0f);
        private static readonly Vector3 FISH_POS = new Vector3(1.8f, -1.2f, 0f);
        private static readonly Vector3 FX_LOCAL_OFFSET = new Vector3(0f, 0.35f, 0f);

        [MenuItem("Tools/Peakingu/Construir Escena Final")]
        public static void BuildFinalScene()
        {
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // 1. Main Camera estática (sin Cinemachine)
                var camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                camObj.transform.position = new Vector3(0f, 0f, -10f);
                var cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.06f, 0.10f, 1f);
                camObj.AddComponent<AudioListener>();
                camObj.AddComponent<UniversalAdditionalCameraData>();

                // 2. Global Light 2D
                var lightObj = new GameObject("Global Light 2D");
                var light2d = lightObj.AddComponent<Light2D>();
                light2d.lightType = Light2D.LightType.Global;
                light2d.intensity = 1f;

                // 3. Fondo interior_casa.png
                var bgObj = new GameObject("Background_InteriorCasa");
                bgObj.transform.position = new Vector3(0f, 0f, 5f);
                var bgSr = bgObj.AddComponent<SpriteRenderer>();
                bgSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(INTERIOR_BG_PATH);
                bgSr.sortingLayerName = "Background";
                bgSr.sortingOrder = 0;

                // 4. Prefab GameManager (el singleton evita duplicados si viene de Nivel5)
                var gmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GAMEMANAGER_PREFAB_PATH);
                if (gmPrefab != null)
                {
                    PrefabUtility.InstantiatePrefab(gmPrefab);
                }

                // 5. Prefab Pingu (EndingController desactiva PlayerController y PinguAnimationController en runtime)
                GameObject pinguInstance = null;
                var pinguPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PINGU_PREFAB_PATH);
                if (pinguPrefab != null)
                {
                    pinguInstance = (GameObject)PrefabUtility.InstantiatePrefab(pinguPrefab);
                    pinguInstance.transform.position = PINGU_START_POS;
                }

                // 6. PauseManager + Victory_Panel desde HUD.prefab (SIN HUD de juego)
                var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH);
                if (hudPrefab != null)
                {
                    var hudObj = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
                    PrefabUtility.UnpackPrefabInstance(hudObj, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                    hudObj.name = "Pause_Canvas";

                    var hudMgr = hudObj.GetComponent<HUDManager>();
                    if (hudMgr != null) Object.DestroyImmediate(hudMgr);

                    Transform topLeft = hudObj.transform.Find("TopLeft_Group");
                    if (topLeft != null) Object.DestroyImmediate(topLeft.gameObject);

                    Transform topRight = hudObj.transform.Find("TopRight_Group");
                    if (topRight != null) Object.DestroyImmediate(topRight.gameObject);
                }

                // 7. Pescado en la parrilla + FrameAnimator de vapor/humo
                var fishObj = new GameObject("Pescado_Parrilla");
                fishObj.transform.position = FISH_POS;
                var fishSr = fishObj.AddComponent<SpriteRenderer>();
                fishSr.sortingLayerName = "Environment";
                fishSr.sortingOrder = 3;

                var fxObj = new GameObject("Humo_Vapor_FX");
                fxObj.transform.SetParent(fishObj.transform, false);
                fxObj.transform.localPosition = FX_LOCAL_OFFSET;
                var fxSr = fxObj.AddComponent<SpriteRenderer>();
                fxSr.sortingLayerName = "Environment";
                fxSr.sortingOrder = 4;
                var frameAnim = fxObj.AddComponent<FrameAnimator>();

                // 8. Canvas_Ending (FadeOverlay + DialogueText)
                var endingCanvasObj = new GameObject("Canvas_Ending");
                var endingCanvas = endingCanvasObj.AddComponent<Canvas>();
                endingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                endingCanvas.sortingOrder = 50;
                var scaler = endingCanvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                endingCanvasObj.AddComponent<GraphicRaycaster>();

                var fadeObj = new GameObject("FadeOverlay");
                fadeObj.transform.SetParent(endingCanvasObj.transform, false);
                var fadeRt = fadeObj.AddComponent<RectTransform>();
                fadeRt.anchorMin = Vector2.zero;
                fadeRt.anchorMax = Vector2.one;
                fadeRt.offsetMin = Vector2.zero;
                fadeRt.offsetMax = Vector2.zero;
                var fadeImg = fadeObj.AddComponent<Image>();
                fadeImg.color = Color.black;
                var fadeGroup = fadeObj.AddComponent<CanvasGroup>();
                fadeGroup.alpha = 1f;

                var textObj = new GameObject("DialogueText");
                textObj.transform.SetParent(endingCanvasObj.transform, false);
                var textRt = textObj.AddComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0.5f, 0.12f);
                textRt.anchorMax = new Vector2(0.5f, 0.12f);
                textRt.pivot = new Vector2(0.5f, 0.5f);
                textRt.sizeDelta = new Vector2(1200f, 120f);
                var dialogueTmp = textObj.AddComponent<TextMeshProUGUI>();
                dialogueTmp.alignment = TextAlignmentOptions.Center;
                dialogueTmp.fontSize = 36f;
                dialogueTmp.fontStyle = FontStyles.Bold;
                dialogueTmp.color = Color.white;
                dialogueTmp.textWrappingMode = TextWrappingModes.Normal;
                textObj.SetActive(false);

                // 9. EndingController
                var endingObj = new GameObject("EndingManager");
                var endingCtrl = endingObj.AddComponent<EndingController>();
                endingCtrl.Configure(
                    pinguInstance,
                    fadeGroup,
                    dialogueTmp,
                    fishSr,
                    AssetDatabase.LoadAssetAtPath<Sprite>(GOOD_FISH_PATH),
                    AssetDatabase.LoadAssetAtPath<Sprite>(BURNT_FISH_PATH),
                    frameAnim,
                    LoadFrames("vapor_blanco"),
                    LoadFrames("humo_negro")
                );

                // 10. EventSystem
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();

                // 11. Guardar escena y asegurarla al final del Build Settings
                EditorSceneManager.SaveScene(scene, FINAL_SCENE_PATH);
                EnsureFinalSceneAtEndOfBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[FinalSceneBuilder] SUCCESS: Escena Final construida y añadida al final de Build Settings.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FinalSceneBuilder] Error al construir escena Final: {ex}");
            }
        }

        private static Sprite[] LoadFrames(string prefix)
        {
            var list = new List<Sprite>();
            for (int i = 1; i <= 3; i++)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Clean/Anim/{prefix}_{i:D2}.png");
                if (sp != null) list.Add(sp);
            }
            return list.ToArray();
        }

        private static void EnsureFinalSceneAtEndOfBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == FINAL_SCENE_PATH);
            if (File.Exists(FINAL_SCENE_PATH))
            {
                scenes.Add(new EditorBuildSettingsScene(FINAL_SCENE_PATH, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
