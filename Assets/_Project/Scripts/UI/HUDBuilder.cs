using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class HUDBuilder : MonoBehaviour
    {
        public static HUDController Build()
        {
            var canvasGO = new GameObject("HUD_Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            var hud = canvasGO.AddComponent<HUDController>();

            // ===== CROSSHAIR =====
            // Crosshair builds itself in Awake()
            var crosshairGO = CreateUIElement("Crosshair", canvasGO.transform);
            var crosshair = crosshairGO.AddComponent<CrosshairUI>();
            hud.crosshair = crosshair;

            // ===== HEALTH & ARMOR (Bottom Left) =====
            var healthPanel = CreateUIElement("HealthPanel", canvasGO.transform);
            SetAnchors(healthPanel, new Vector2(0, 0), new Vector2(0.25f, 0.07f), new Vector2(10, 10), Vector2.zero);
            AddBackground(healthPanel, new Color(0, 0, 0, 0.5f));

            var healthArmor = healthPanel.AddComponent<HealthArmorUI>();

            var hpIcon = CreateText("HP_Icon", healthPanel.transform, "+", 28, Color.red);
            SetAnchors(hpIcon, new Vector2(0.02f, 0), new Vector2(0.12f, 1), Vector2.zero, Vector2.zero);

            var hpText = CreateText("HP_Text", healthPanel.transform, "100", 26, Color.white);
            SetAnchors(hpText, new Vector2(0.12f, 0), new Vector2(0.45f, 1), Vector2.zero, Vector2.zero);
            healthArmor.healthText = hpText.GetComponent<Text>();

            var armorIcon = CreateText("Armor_Icon", healthPanel.transform, "A", 28, new Color(0.3f, 0.5f, 1f));
            SetAnchors(armorIcon, new Vector2(0.5f, 0), new Vector2(0.6f, 1), Vector2.zero, Vector2.zero);

            var armorText = CreateText("Armor_Text", healthPanel.transform, "0", 26, Color.white);
            SetAnchors(armorText, new Vector2(0.6f, 0), new Vector2(0.95f, 1), Vector2.zero, Vector2.zero);
            healthArmor.armorText = armorText.GetComponent<Text>();

            hud.healthArmor = healthArmor;

            // ===== AMMO (Bottom Right) =====
            var ammoPanel = CreateUIElement("AmmoPanel", canvasGO.transform);
            SetAnchors(ammoPanel, new Vector2(0.78f, 0), new Vector2(1, 0.07f), Vector2.zero, new Vector2(-10, 10));
            AddBackground(ammoPanel, new Color(0, 0, 0, 0.5f));

            var ammoUI = ammoPanel.AddComponent<AmmoUI>();
            var ammoText = CreateText("AmmoText", ammoPanel.transform, "30 / 90", 26, Color.white, TextAnchor.MiddleRight);
            SetAnchors(ammoText, Vector2.zero, Vector2.one, new Vector2(5, 0), new Vector2(-10, 0));
            ammoUI.ammoText = ammoText.GetComponent<Text>();

            hud.ammo = ammoUI;

            // ===== MONEY (Bottom Left, above health) =====
            var moneyText = CreateText("MoneyText", canvasGO.transform, "$800", 22, new Color(0.3f, 0.9f, 0.3f));
            var moneyRect = moneyText.GetComponent<RectTransform>();
            moneyRect.anchorMin = new Vector2(0, 0.08f);
            moneyRect.anchorMax = new Vector2(0.15f, 0.13f);
            moneyRect.offsetMin = new Vector2(15, 0);
            moneyRect.offsetMax = Vector2.zero;
            hud.moneyText = moneyText.GetComponent<Text>();

            // ===== ROUND TIMER (Top Center) =====
            var timerPanel = CreateUIElement("TimerPanel", canvasGO.transform);
            SetAnchors(timerPanel, new Vector2(0.35f, 0.91f), new Vector2(0.65f, 1), Vector2.zero, Vector2.zero);
            AddBackground(timerPanel, new Color(0, 0, 0, 0.5f));

            var timerUI = timerPanel.AddComponent<RoundTimerUI>();

            var scoreT = CreateText("Score", timerPanel.transform, "T 0 : 0 CT", 20, Color.white, TextAnchor.MiddleCenter);
            SetAnchors(scoreT, new Vector2(0, 0.5f), Vector2.one, Vector2.zero, Vector2.zero);
            timerUI.scoreText = scoreT.GetComponent<Text>();

            var timerText = CreateText("Timer", timerPanel.transform, "1:55", 32, Color.white, TextAnchor.MiddleCenter);
            SetAnchors(timerText, Vector2.zero, new Vector2(1, 0.55f), Vector2.zero, Vector2.zero);
            timerUI.timerText = timerText.GetComponent<Text>();

            var phaseText = CreateText("Phase", timerPanel.transform, "", 16, Color.yellow, TextAnchor.MiddleCenter);
            var phaseRect = phaseText.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0.1f, -0.5f);
            phaseRect.anchorMax = new Vector2(0.9f, 0f);
            phaseRect.offsetMin = Vector2.zero;
            phaseRect.offsetMax = Vector2.zero;
            timerUI.phaseText = phaseText.GetComponent<Text>();

            hud.roundTimer = timerUI;

            // ===== KILLFEED (Top Right) =====
            var killfeedPanel = CreateUIElement("KillfeedPanel", canvasGO.transform);
            SetAnchors(killfeedPanel, new Vector2(0.55f, 0.78f), new Vector2(1, 0.95f), Vector2.zero, new Vector2(-10, 0));
            var vlg = killfeedPanel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperRight;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2;

            var killfeed = killfeedPanel.AddComponent<KillfeedUI>();
            killfeed.killfeedParent = killfeedPanel.transform;
            hud.killfeed = killfeed;

            // ===== BOMB STATUS (Center) =====
            var bombText = CreateText("BombStatus", canvasGO.transform, "", 28, new Color(1, 0.3f, 0.3f), TextAnchor.MiddleCenter);
            var bombRect = bombText.GetComponent<RectTransform>();
            bombRect.anchorMin = new Vector2(0.3f, 0.14f);
            bombRect.anchorMax = new Vector2(0.7f, 0.2f);
            bombRect.offsetMin = Vector2.zero;
            bombRect.offsetMax = Vector2.zero;
            hud.bombStatusText = bombText.GetComponent<Text>();

            // ===== CONTEXT PROMPT (Center Bottom) =====
            var ctxText = CreateText("ContextPrompt", canvasGO.transform, "", 20, Color.white, TextAnchor.MiddleCenter);
            var ctxRect = ctxText.GetComponent<RectTransform>();
            ctxRect.anchorMin = new Vector2(0.3f, 0.22f);
            ctxRect.anchorMax = new Vector2(0.7f, 0.28f);
            ctxRect.offsetMin = Vector2.zero;
            ctxRect.offsetMax = Vector2.zero;
            hud.contextText = ctxText.GetComponent<Text>();

            // ===== SCOREBOARD (Full Overlay) =====
            var scoreboardPanel = CreateUIElement("ScoreboardPanel", canvasGO.transform);
            SetAnchors(scoreboardPanel, new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.9f), Vector2.zero, Vector2.zero);
            AddBackground(scoreboardPanel, new Color(0, 0, 0, 0.85f));
            scoreboardPanel.SetActive(false);

            var sbUI = scoreboardPanel.AddComponent<ScoreboardUI>();
            sbUI.scoreboardPanel = scoreboardPanel;

            var sbText = CreateText("ScoreboardText", scoreboardPanel.transform, "", 16, Color.white, TextAnchor.UpperLeft);
            SetAnchors(sbText, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -20));
            sbUI.scoreboardText = sbText.GetComponent<Text>();

            hud.scoreboard = sbUI;

            // ===== ROUND END =====
            var roundEndPanel = CreateUIElement("RoundEndPanel", canvasGO.transform);
            SetAnchors(roundEndPanel, new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.6f), Vector2.zero, Vector2.zero);
            AddBackground(roundEndPanel, new Color(0, 0, 0, 0.6f));

            var reUI = roundEndPanel.AddComponent<RoundEndUI>();
            reUI.panel = roundEndPanel;

            var reText = CreateText("ResultText", roundEndPanel.transform, "", 42, Color.white, TextAnchor.MiddleCenter);
            SetAnchors(reText, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            reUI.resultText = reText.GetComponent<Text>();

            hud.roundEnd = reUI;

            // ===== BUY MENU =====
            var buyPanel = CreateUIElement("BuyMenuPanel", canvasGO.transform);
            SetAnchors(buyPanel, new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.9f), Vector2.zero, Vector2.zero);
            AddBackground(buyPanel, new Color(0.1f, 0.1f, 0.15f, 0.95f));
            buyPanel.SetActive(false);

            var buyUI = buyPanel.AddComponent<BuyMenuUI>();
            buyUI.buyMenuPanel = buyPanel;

            var buyTitle = CreateText("BuyTitle", buyPanel.transform, "BUY MENU", 28, Color.white, TextAnchor.MiddleCenter);
            SetAnchors(buyTitle, new Vector2(0, 0.9f), Vector2.one, Vector2.zero, Vector2.zero);

            var buyMoney = CreateText("BuyMoney", buyPanel.transform, "$800", 22, new Color(0.3f, 0.8f, 0.3f), TextAnchor.MiddleCenter);
            SetAnchors(buyMoney, new Vector2(0, 0.82f), new Vector2(1, 0.9f), Vector2.zero, Vector2.zero);
            buyUI.moneyDisplay = buyMoney.GetComponent<Text>();

            var weaponContainer = CreateUIElement("WeaponButtons", buyPanel.transform);
            SetAnchors(weaponContainer, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.8f), Vector2.zero, Vector2.zero);
            var layout = weaponContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            buyUI.weaponButtonParent = weaponContainer.transform;
            hud.buyMenu = buyUI;

            // ===== GAME OVER =====
            var gameOverPanel = CreateUIElement("GameOverPanel", canvasGO.transform);
            SetAnchors(gameOverPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddBackground(gameOverPanel, new Color(0, 0, 0, 0.8f));
            gameOverPanel.SetActive(false);

            var goUI = gameOverPanel.AddComponent<GameOverUI>();
            goUI.panel = gameOverPanel;

            var goResult = CreateText("GameOverResult", gameOverPanel.transform, "VICTORY", 54, Color.white, TextAnchor.MiddleCenter);
            SetAnchors(goResult, new Vector2(0, 0.5f), new Vector2(1, 0.7f), Vector2.zero, Vector2.zero);
            goUI.resultText = goResult.GetComponent<Text>();

            var goScore = CreateText("GameOverScore", gameOverPanel.transform, "", 30, Color.white, TextAnchor.MiddleCenter);
            SetAnchors(goScore, new Vector2(0, 0.35f), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
            goUI.scoreText = goScore.GetComponent<Text>();

            var returnBtn = CreateButton("ReturnButton", gameOverPanel.transform, "Return to Menu", new Vector2(250, 55));
            var returnRect = returnBtn.GetComponent<RectTransform>();
            returnRect.anchorMin = new Vector2(0.5f, 0.2f);
            returnRect.anchorMax = new Vector2(0.5f, 0.2f);
            returnRect.anchoredPosition = Vector2.zero;
            goUI.returnButton = returnBtn.GetComponent<Button>();

            hud.gameOver = goUI;

            return hud;
        }

        static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        static void AddBackground(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
        }

        static Font _cachedFont;
        static Font GetFont()
        {
            if (_cachedFont != null) return _cachedFont;
            _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_cachedFont == null)
                _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_cachedFont == null)
                _cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
            return _cachedFont;
        }

        static GameObject CreateText(string name, Transform parent, string text, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.font = GetFont();
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            return go;
        }

        static GameObject CreateButton(string name, Transform parent, string label, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            go.AddComponent<Button>();

            var textGO = CreateText("Text", go.transform, label, 18, Color.white, TextAnchor.MiddleCenter);
            SetAnchors(textGO, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return go;
        }
    }
}
