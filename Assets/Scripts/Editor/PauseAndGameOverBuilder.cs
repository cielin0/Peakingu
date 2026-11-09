using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;

namespace Peakingu.EditorTools
{
    public static class PauseAndGameOverBuilder
    {
        [MenuItem("Tools/Peakingu/Build Pause, GameOver and Victory UI")]
        public static void Execute()
        {
            try
            {
                BuildHUDWithPauseGameOverVictory();
                UpdateNivel1Scene();
                Debug.Log("[Peakingu] SUCCESS: Menú de Pausa (ESC), Game Over (Reiniciar) y Pantalla de Victoria integrados exitosamente.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Peakingu] Error al armar UI de Pausa y Victoria: {ex}");
            }
        }

        public static void BuildHUDWithPauseGameOverVictory()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var heartFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Heart_Full.png");
            var heartEmpty = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Heart_Empty.png");
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Coin.png");
            var heightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Height_Icon.png");
            var buttonBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Button_Bg.png");
            var panelBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Panel_Bg.png");
            var sliderTrack = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Slider_Track.png");
            var sliderFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Slider_Fill.png");

            var existingHUD = GameObject.Find("HUD_Temp");
            if (existingHUD != null) Object.DestroyImmediate(existingHUD);

            // 1. Root Canvas
            GameObject hudCanvas = new GameObject("HUD_Temp");
            var canvas = hudCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;

            var scaler = hudCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            hudCanvas.AddComponent<GraphicRaycaster>();
            var hudManager = hudCanvas.AddComponent<HUDManager>();
            var pauseManager = hudCanvas.AddComponent<PauseManager>();

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
                GameObject hObj = new GameObject($"Heart_{i + 1}");
                hObj.transform.SetParent(heartsParent.transform, false);
                var img = hObj.AddComponent<Image>();
                img.sprite = heartFull;
                img.preserveAspect = true;
                var rtH = hObj.GetComponent<RectTransform>();
                rtH.sizeDelta = new Vector2(38f, 38f);
                heartImages.Add(img);
            }

            // Temporizador (TIME: 20:00)
            GameObject timerObj = new GameObject("Timer_Text");
            timerObj.transform.SetParent(topLeft.transform, false);
            var rtTimer = timerObj.AddComponent<RectTransform>();
            rtTimer.anchorMin = new Vector2(0f, 0.5f);
            rtTimer.anchorMax = new Vector2(0f, 0.5f);
            rtTimer.pivot = new Vector2(0f, 0.5f);
            rtTimer.anchoredPosition = new Vector2(245f, 0f);
            rtTimer.sizeDelta = new Vector2(320f, 50f);

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
            rtTR.sizeDelta = new Vector2(400f, 60f);

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
            rtHGroup.anchorMin = new Vector2(1f, 0f);
            rtHGroup.anchorMax = new Vector2(1f, 0f);
            rtHGroup.pivot = new Vector2(1f, 0f);
            rtHGroup.anchoredPosition = new Vector2(0f, -50f);
            rtHGroup.sizeDelta = new Vector2(240f, 44f);

            GameObject heightTextObj = new GameObject("Height_Text");
            heightTextObj.transform.SetParent(heightObj.transform, false);
            var rtHT = heightTextObj.AddComponent<RectTransform>();
            rtHT.anchorMin = new Vector2(0f, 0.5f);
            rtHT.anchorMax = new Vector2(1f, 0.5f);
            rtHT.pivot = new Vector2(0f, 0.5f);
            rtHT.anchoredPosition = new Vector2(0f, 0f);
            rtHT.sizeDelta = new Vector2(190f, 40f);

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

            // -------------------------------------------------------------
            // 4. MENÚ DE PAUSA (Pause_Panel)
            // -------------------------------------------------------------
            GameObject pausePanel = CreateOverlayPanel(hudCanvas.transform, "Pause_Panel", new Color(0.04f, 0.08f, 0.15f, 0.85f));
            GameObject pauseModal = CreateModalBox(pausePanel.transform, "Modal_Pause", panelBg, 540f, 480f);

            CreateTitle(pauseModal.transform, "Title_Pausa", "PAUSA", font, 48f, new Color(0.36f, 0.88f, 0.90f), 180f);

            Button btnResume = CreateButton(pauseModal.transform, "Btn_Reanudar", "REANUDAR", buttonBg, font, 320f, 60f, 24f, 60f);
            Button btnSettings = CreateButton(pauseModal.transform, "Btn_Configuracion", "CONFIGURACIÓN", buttonBg, font, 320f, 60f, 24f, -20f);
            Button btnQuitPause = CreateButton(pauseModal.transform, "Btn_Volver_Menu", "VOLVER AL MENÚ", buttonBg, font, 320f, 60f, 24f, -100f);

            UnityEventTools.AddPersistentListener(btnResume.onClick, pauseManager.ResumeGame);
            UnityEventTools.AddPersistentListener(btnSettings.onClick, pauseManager.OpenSettings);
            UnityEventTools.AddPersistentListener(btnQuitPause.onClick, pauseManager.QuitToMainMenu);

            // -------------------------------------------------------------
            // 5. CONFIGURACIÓN EN PAUSA (Settings_Panel)
            // -------------------------------------------------------------
            GameObject settingsPanel = CreateOverlayPanel(hudCanvas.transform, "Settings_Panel", new Color(0.04f, 0.08f, 0.15f, 0.85f));
            GameObject settingsModal = CreateModalBox(settingsPanel.transform, "Modal_Settings", panelBg, 560f, 420f);

            CreateTitle(settingsModal.transform, "Title_Settings", "CONFIGURACIÓN", font, 44f, Color.white, 150f);

            // Slider de Volumen
            GameObject volLabelObj = new GameObject("Label_Volumen");
            volLabelObj.transform.SetParent(settingsModal.transform, false);
            var rtVolLabel = volLabelObj.AddComponent<RectTransform>();
            rtVolLabel.anchoredPosition = new Vector2(0f, 60f);
            rtVolLabel.sizeDelta = new Vector2(400f, 40f);
            var tmpVolLabel = volLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpVolLabel.font = font;
            tmpVolLabel.fontSize = 24f;
            tmpVolLabel.fontStyle = FontStyles.Bold;
            tmpVolLabel.color = new Color(0.36f, 0.88f, 0.90f);
            tmpVolLabel.text = "VOLUMEN MAESTRO";
            tmpVolLabel.alignment = TextAlignmentOptions.Center;

            // Slider Component
            GameObject sliderObj = new GameObject("Slider_Volumen");
            sliderObj.transform.SetParent(settingsModal.transform, false);
            var rtSlider = sliderObj.AddComponent<RectTransform>();
            rtSlider.anchoredPosition = new Vector2(0f, 10f);
            rtSlider.sizeDelta = new Vector2(360f, 28f);

            var imgTrack = sliderObj.AddComponent<Image>();
            imgTrack.sprite = sliderTrack;
            imgTrack.type = Image.Type.Sliced;
            imgTrack.color = new Color(0.15f, 0.2f, 0.3f);

            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var rtFillArea = fillArea.AddComponent<RectTransform>();
            rtFillArea.anchorMin = Vector2.zero;
            rtFillArea.anchorMax = Vector2.one;
            rtFillArea.offsetMin = new Vector2(4f, 4f);
            rtFillArea.offsetMax = new Vector2(-4f, -4f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var rtFill = fill.AddComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.offsetMin = Vector2.zero;
            rtFill.offsetMax = Vector2.zero;

            var imgFill = fill.AddComponent<Image>();
            imgFill.sprite = sliderFill;
            imgFill.type = Image.Type.Sliced;
            imgFill.color = new Color(0.12f, 0.65f, 0.95f);

            slider.targetGraphic = imgFill;
            slider.fillRect = rtFill;

            // Texto Porcentaje Volumen
            GameObject volValObj = new GameObject("Text_Volumen_Valor");
            volValObj.transform.SetParent(settingsModal.transform, false);
            var rtVolVal = volValObj.AddComponent<RectTransform>();
            rtVolVal.anchoredPosition = new Vector2(0f, -30f);
            rtVolVal.sizeDelta = new Vector2(200f, 35f);
            var tmpVolVal = volValObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpVolVal.font = font;
            tmpVolVal.fontSize = 24f;
            tmpVolVal.fontStyle = FontStyles.Bold;
            tmpVolVal.color = Color.white;
            tmpVolVal.text = "100%";
            tmpVolVal.alignment = TextAlignmentOptions.Center;

            Button btnCloseSettings = CreateButton(settingsModal.transform, "Btn_Volver_Settings", "VOLVER", buttonBg, font, 260f, 55f, 22f, -120f);
            UnityEventTools.AddPersistentListener(btnCloseSettings.onClick, pauseManager.CloseSettings);

            // -------------------------------------------------------------
            // 6. GAME OVER (GameOver_Panel)
            // -------------------------------------------------------------
            GameObject goPanel = CreateOverlayPanel(hudCanvas.transform, "GameOver_Panel", new Color(0.12f, 0.03f, 0.05f, 0.92f));
            GameObject goModal = CreateModalBox(goPanel.transform, "Modal_GameOver", panelBg, 640f, 460f);

            CreateTitle(goModal.transform, "Title_GameOver", "GAME OVER", font, 56f, new Color(1f, 0.28f, 0.34f), 160f);

            GameObject goReasonObj = new GameObject("GameOver_Reason");
            goReasonObj.transform.SetParent(goModal.transform, false);
            var rtGOReason = goReasonObj.AddComponent<RectTransform>();
            rtGOReason.anchoredPosition = new Vector2(0f, 65f);
            rtGOReason.sizeDelta = new Vector2(560f, 90f);

            var tmpGOReason = goReasonObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpGOReason.font = font;
            tmpGOReason.fontSize = 24f;
            tmpGOReason.color = Color.white;
            tmpGOReason.text = "¡PINGU SE HA QUEDADO SIN VIDAS!\nHas caído de la montaña.";
            tmpGOReason.alignment = TextAlignmentOptions.Center;

            Button btnRestartGO = CreateButton(goModal.transform, "Btn_Reiniciar_GO", "REINICIAR", buttonBg, font, 300f, 58f, 24f, -30f);
            Button btnQuitGO = CreateButton(goModal.transform, "Btn_Volver_GO", "VOLVER AL MENÚ", buttonBg, font, 300f, 58f, 24f, -110f);

            UnityEventTools.AddPersistentListener(btnRestartGO.onClick, pauseManager.RestartGame);
            UnityEventTools.AddPersistentListener(btnQuitGO.onClick, pauseManager.QuitToMainMenu);

            // -------------------------------------------------------------
            // 7. PANTALLA DE VICTORIA (Victory_Panel)
            // -------------------------------------------------------------
            GameObject victoryPanel = CreateOverlayPanel(hudCanvas.transform, "Victory_Panel", new Color(0.02f, 0.08f, 0.14f, 0.92f));
            GameObject victoryModal = CreateModalBox(victoryPanel.transform, "Modal_Victory", panelBg, 660f, 480f);

            CreateTitle(victoryModal.transform, "Title_Victoria", "¡VICTORIA!", font, 56f, new Color(1f, 0.83f, 0.16f), 170f);

            GameObject vicSubObj = new GameObject("Victory_Subtitle");
            vicSubObj.transform.SetParent(victoryModal.transform, false);
            var rtVicSub = vicSubObj.AddComponent<RectTransform>();
            rtVicSub.anchoredPosition = new Vector2(0f, 95f);
            rtVicSub.sizeDelta = new Vector2(580f, 50f);
            var tmpVicSub = vicSubObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpVicSub.font = font;
            tmpVicSub.fontSize = 24f;
            tmpVicSub.fontStyle = FontStyles.Bold;
            tmpVicSub.color = new Color(0.36f, 0.88f, 0.90f);
            tmpVicSub.text = "¡HAS LLEGADO A LA CIMA!\n¡El pescado en el horno está a salvo!";
            tmpVicSub.alignment = TextAlignmentOptions.Center;

            GameObject vicStatsObj = new GameObject("Victory_Stats");
            vicStatsObj.transform.SetParent(victoryModal.transform, false);
            var rtVicStats = vicStatsObj.AddComponent<RectTransform>();
            rtVicStats.anchoredPosition = new Vector2(0f, 25f);
            rtVicStats.sizeDelta = new Vector2(560f, 70f);
            var tmpVicStats = vicStatsObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpVicStats.font = font;
            tmpVicStats.fontSize = 22f;
            tmpVicStats.color = Color.white;
            tmpVicStats.text = "TIEMPO RESTANTE: 18:42\nMONEDAS RECOLECTADAS: 35";
            tmpVicStats.alignment = TextAlignmentOptions.Center;

            Button btnRestartV = CreateButton(victoryModal.transform, "Btn_Reiniciar_V", "JUGAR DE NUEVO", buttonBg, font, 300f, 58f, 24f, -60f);
            Button btnQuitV = CreateButton(victoryModal.transform, "Btn_Volver_V", "VOLVER AL MENÚ", buttonBg, font, 300f, 58f, 24f, -135f);

            UnityEventTools.AddPersistentListener(btnRestartV.onClick, pauseManager.RestartGame);
            UnityEventTools.AddPersistentListener(btnQuitV.onClick, pauseManager.QuitToMainMenu);

            // -------------------------------------------------------------
            // 8. Ocultar paneles y vincular referencias
            // -------------------------------------------------------------
            pausePanel.SetActive(false);
            settingsPanel.SetActive(false);
            goPanel.SetActive(false);
            victoryPanel.SetActive(false);

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

            pauseManager.SetupInEditor(
                pausePanel,
                btnResume,
                btnSettings,
                btnQuitPause,
                settingsPanel,
                slider,
                tmpVolVal,
                btnCloseSettings,
                goPanel,
                tmpGOReason,
                btnRestartGO,
                btnQuitGO,
                victoryPanel,
                tmpVicSub,
                tmpVicStats,
                btnRestartV,
                btnQuitV
            );

            hudCanvas.name = "HUD";
            PrefabUtility.SaveAsPrefabAsset(hudCanvas, "Assets/Prefabs/HUD.prefab");
            Object.DestroyImmediate(hudCanvas);
            AssetDatabase.SaveAssets();
            Debug.Log("[Peakingu] Prefab HUD actualizado con PauseManager, GameOver y Victoria.");
        }

        public static void UpdateNivel1Scene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Nivel1.unity", OpenSceneMode.Single);

            // 1. Reinstanciar HUD.prefab actualizado
            var existingHUD = GameObject.Find("HUD");
            if (existingHUD != null) Object.DestroyImmediate(existingHUD);

            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HUD.prefab");
            if (hudPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(hudPrefab);
            }

            // 2. Colocar metas de Victoria (Victory_Goal)
            var heightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Height_Icon.png");

            // Meta en Cumbre de la montaña (Plataforma E: x = 24, y = 10.5)
            var existingGoal1 = GameObject.Find("Victory_Goal_Summit");
            if (existingGoal1 != null) Object.DestroyImmediate(existingGoal1);

            GameObject goalSummit = new GameObject("Victory_Goal_Summit");
            goalSummit.transform.position = new Vector3(24.5f, 10.5f, 0f);

            var srSummit = goalSummit.AddComponent<SpriteRenderer>();
            srSummit.sprite = heightSprite;
            srSummit.color = new Color(1f, 0.85f, 0.2f); // Dorado brillante
            srSummit.sortingLayerName = "Collectibles";
            goalSummit.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            var colSummit = goalSummit.AddComponent<BoxCollider2D>();
            colSummit.isTrigger = true;
            colSummit.size = new Vector2(2.5f, 2.5f);

            goalSummit.AddComponent<VictoryTrigger>();

            // Meta en Extremo derecho del recorrido (Plataforma H: x = 47, y = 1.5)
            var existingGoal2 = GameObject.Find("Victory_Goal_End");
            if (existingGoal2 != null) Object.DestroyImmediate(existingGoal2);

            GameObject goalEnd = new GameObject("Victory_Goal_End");
            goalEnd.transform.position = new Vector3(47f, 1.5f, 0f);

            var srEnd = goalEnd.AddComponent<SpriteRenderer>();
            srEnd.sprite = heightSprite;
            srEnd.color = new Color(0.36f, 0.88f, 0.90f); // Celeste brillante
            srEnd.sortingLayerName = "Collectibles";
            goalEnd.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            var colEnd = goalEnd.AddComponent<BoxCollider2D>();
            colEnd.isTrigger = true;
            colEnd.size = new Vector2(2.5f, 2.5f);

            goalEnd.AddComponent<VictoryTrigger>();

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Peakingu] Escena Nivel1 actualizada con HUD completo y Metas de Victoria (Cumbre y Final).");
        }

        private static GameObject CreateOverlayPanel(Transform parent, string name, Color bgColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = panel.AddComponent<Image>();
            img.color = bgColor;

            return panel;
        }

        private static GameObject CreateModalBox(Transform parent, string name, Sprite modalSprite, float width, float height)
        {
            GameObject modal = new GameObject(name);
            modal.transform.SetParent(parent, false);

            var rt = modal.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, height);

            var img = modal.AddComponent<Image>();
            img.sprite = modalSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.92f, 0.96f, 1f, 0.98f);

            return modal;
        }

        private static void CreateTitle(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color, float posY)
        {
            GameObject titleObj = new GameObject(name);
            titleObj.transform.SetParent(parent, false);

            var rt = titleObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, posY);
            rt.sizeDelta = new Vector2(500f, 60f);

            var tmp = titleObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private static Button CreateButton(Transform parent, string name, string label, Sprite bgSprite, TMP_FontAsset font, float width, float height, float fontSize, float posY)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, posY);
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
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.05f, 0.15f, 0.3f);
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}
