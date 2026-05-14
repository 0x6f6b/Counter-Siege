// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Add a basic in-game pause menu that opens on Escape with a Resume
//          and a Back to Main Menu button."
// Modifications: Wired through PlayerLook.SetCursorLock so the existing
//                cursor lock pattern keeps working, paused via Time.timeScale.

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CounterSiege
{
    public class EscapeMenuUI : MonoBehaviour
    {
        Canvas canvas;
        GameObject panel;
        bool isOpen;

        void Awake()
        {
            BuildUI();
            Hide();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.pKey.wasPressedThisFrame)
            {
                if (isOpen) Resume();
                else Open();
            }
        }

        void Open()
        {
            isOpen = true;
            panel.SetActive(true);
            Time.timeScale = 0f;

            var look = GameManager.Instance?.playerObject?.GetComponent<PlayerLook>();
            if (look != null) look.SetCursorLock(false);
            else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        void Resume()
        {
            isOpen = false;
            panel.SetActive(false);
            Time.timeScale = 1f;

            var look = GameManager.Instance?.playerObject?.GetComponent<PlayerLook>();
            if (look != null) look.SetCursorLock(true);
        }

        void BackToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        void Hide()
        {
            isOpen = false;
            if (panel != null) panel.SetActive(false);
        }

        void BuildUI()
        {
            var canvasGO = new GameObject("EscapeMenuCanvas");
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            panel = NewRect("Panel", canvasGO.transform, Vector2.zero, Vector2.one).gameObject;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            var card = NewRect("Card", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            card.sizeDelta = new Vector2(420, 280);
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.color = new Color(0.09f, 0.11f, 0.16f, 1f);

            var title = NewRect("Title", card, new Vector2(0f, 0.7f), new Vector2(1f, 1f));
            var titleText = title.gameObject.AddComponent<TextMeshProUGUI>();
            titleText.text = "PAUSED";
            titleText.font = TMP_Settings.defaultFontAsset;
            titleText.fontSize = 42;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.96f, 0.96f, 0.94f, 1f);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.characterSpacing = 12f;

            var btnRow = NewRect("Buttons", card, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.65f));
            var vlg = btnRow.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            MakeButton(btnRow, "RESUME", new Color(0.95f, 0.78f, 0.35f, 1f), Resume);
            MakeButton(btnRow, "MAIN MENU", new Color(0.55f, 0.18f, 0.18f, 1f), BackToMainMenu);
        }

        void MakeButton(RectTransform parent, string label, Color accent, System.Action onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            le.minHeight = 36;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.13f, 0.15f, 0.20f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.highlightedColor = new Color(0.18f, 0.21f, 0.27f, 1f);
            cb.pressedColor = new Color(0.10f, 0.12f, 0.16f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick());

            var stripe = NewRect("Accent", go.transform, new Vector2(0f, 0f), new Vector2(0f, 1f));
            stripe.sizeDelta = new Vector2(5, 0);
            stripe.anchoredPosition = new Vector2(2.5f, 0);
            stripe.gameObject.AddComponent<Image>().color = accent;

            var labelRect = NewRect("Label", go.transform, Vector2.zero, Vector2.one);
            var labelText = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.font = TMP_Settings.defaultFontAsset;
            labelText.fontSize = 16;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = new Color(0.96f, 0.96f, 0.94f, 1f);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.characterSpacing = 6f;
            labelText.raycastTarget = false;
        }

        static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        void OnDestroy()
        {
            // Restore time scale in case scene is unloaded while paused.
            Time.timeScale = 1f;
        }
    }
}
