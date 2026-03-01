using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CounterSiege
{
    public class MainMenuBuilder : MonoBehaviour
    {
        void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            if (Camera.main == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
                camGO.transform.position = new Vector3(0, 5, -10);
                camGO.transform.rotation = Quaternion.Euler(20, 0, 0);
            }

            BuildUI();
        }

        void BuildUI()
        {
            var canvasGO = new GameObject("MenuCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGO.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.12f, 1f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(canvasGO.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.2f, 0.7f);
            titleRect.anchorMax = new Vector2(0.8f, 0.85f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = "COUNTER SIEGE";
            titleText.fontSize = 72;
            titleText.color = new Color(0.9f, 0.85f, 0.7f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.font = GetFont();

            // Subtitle
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(canvasGO.transform, false);
            var subRect = subGO.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.3f, 0.62f);
            subRect.anchorMax = new Vector2(0.7f, 0.7f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            var subText = subGO.AddComponent<Text>();
            subText.text = "Select Your Team";
            subText.fontSize = 24;
            subText.color = Color.gray;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.font = GetFont();

            // Main Menu UI
            var menuUI = canvasGO.AddComponent<MainMenuUI>();

            var tBtn = CreateMenuButton(canvasGO.transform, "Play as Terrorist",
                new Color(0.8f, 0.6f, 0.2f), new Vector2(0.5f, 0.48f), new Vector2(300, 60));
            menuUI.playTButton = tBtn.GetComponent<Button>();

            var ctBtn = CreateMenuButton(canvasGO.transform, "Play as Counter-Terrorist",
                new Color(0.2f, 0.4f, 0.8f), new Vector2(0.5f, 0.36f), new Vector2(300, 60));
            menuUI.playCTButton = ctBtn.GetComponent<Button>();

            var quitBtn = CreateMenuButton(canvasGO.transform, "Quit",
                new Color(0.5f, 0.2f, 0.2f), new Vector2(0.5f, 0.14f), new Vector2(200, 50));
            menuUI.quitButton = quitBtn.GetComponent<Button>();

            // Volume slider
            CreateVolumeSlider(canvasGO.transform, menuUI);
        }

        void CreateVolumeSlider(Transform parent, MainMenuUI menuUI)
        {
            // Container
            var container = new GameObject("VolumeControl");
            container.transform.SetParent(parent, false);
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.05f);
            containerRect.anchorMax = new Vector2(0.5f, 0.05f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(300, 40);

            // Label
            var labelGO = new GameObject("VolumeLabel");
            labelGO.transform.SetParent(container.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(-80, 0);
            labelRect.sizeDelta = new Vector2(120, 30);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = "Volume: 30%";
            labelText.fontSize = 16;
            labelText.color = Color.gray;
            labelText.alignment = TextAnchor.MiddleRight;
            labelText.font = GetFont();
            menuUI.volumeLabel = labelText;

            // Slider
            var sliderGO = new GameObject("VolumeSlider");
            sliderGO.transform.SetParent(container.transform, false);
            var sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(50, 0);
            sliderRect.sizeDelta = new Vector2(180, 20);

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            // Fill area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = new Color(0.4f, 0.6f, 0.8f, 1f);

            // Handle slide area
            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16, 24);
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = Color.white;

            // Wire up slider component
            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = PlayerPrefs.GetFloat("MasterVolume", 0.3f);

            menuUI.volumeSlider = slider;
        }

        GameObject CreateMenuButton(Transform parent, string label, Color color, Vector2 anchorPos, Vector2 size)
        {
            var go = new GameObject(label.Replace(" ", ""));
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;

            go.AddComponent<Button>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var txt = textGO.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 20;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
            txt.font = GetFont();

            return go;
        }

        static Font GetFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            return font;
        }
    }
}
