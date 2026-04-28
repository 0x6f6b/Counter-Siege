// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Make a CS:GO-style buy menu that lists weapons sorted into pistols,
//          rifles, snipers and grenades plus armor, only shows what my team
//          can buy, and grays out stuff I can't afford."
// Modifications: Tuned the dark colour palette, switched to 2-cards-per-row
//                layout, added the close-on-B hint and the resize fix that
//                stopped armor clipping on short aspect ratios.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class BuyMenuUI : MonoBehaviour
    {
        public GameObject buyMenuPanel;
        public Transform weaponButtonParent;
        public Text moneyDisplay;

        // Palette
        static readonly Color Bg            = new Color(0.05f, 0.06f, 0.09f, 0.96f);
        static readonly Color HeaderBg      = new Color(0.09f, 0.11f, 0.16f, 1f);
        static readonly Color SectionColor  = new Color(0.55f, 0.58f, 0.65f, 1f);
        static readonly Color TextMain      = new Color(0.96f, 0.96f, 0.94f, 1f);
        static readonly Color TextSub       = new Color(0.65f, 0.68f, 0.74f, 1f);
        static readonly Color MoneyColor    = new Color(0.45f, 0.85f, 0.45f, 1f);
        static readonly Color Accent        = new Color(0.95f, 0.78f, 0.35f, 1f);

        static readonly Color CardFill      = new Color(0.13f, 0.15f, 0.20f, 1f);
        static readonly Color CardFillAlt   = new Color(0.18f, 0.21f, 0.27f, 1f);
        static readonly Color CardLocked    = new Color(0.10f, 0.10f, 0.13f, 1f);

        static readonly Color CatPistol     = new Color(0.32f, 0.58f, 0.92f, 1f);
        static readonly Color CatRifle      = new Color(0.92f, 0.35f, 0.28f, 1f);
        static readonly Color CatSniper     = new Color(0.72f, 0.42f, 0.92f, 1f);
        static readonly Color CatGrenade    = new Color(0.45f, 0.78f, 0.40f, 1f);
        static readonly Color CatArmor      = new Color(0.70f, 0.72f, 0.78f, 1f);

        bool isOpen;
        bool chromeBuilt;
        TMP_Text moneyLabel;
        RectTransform listContainer;

        void Start()
        {
            if (buyMenuPanel != null)
                buyMenuPanel.SetActive(false);
        }

        public void Toggle() { if (isOpen) Close(); else Open(); }

        public void Open()
        {
            var rm = GameManager.Instance?.roundManager;
            if (rm == null || !rm.IsBuyTime) return;

            EnsureChrome();

            isOpen = true;
            if (buyMenuPanel != null) buyMenuPanel.SetActive(true);

            var player = GameManager.Instance?.playerObject;
            var look = player?.GetComponent<PlayerLook>();
            if (look != null) look.SetCursorLock(false);

            PopulateWeapons();
        }

        public void Close()
        {
            isOpen = false;
            if (buyMenuPanel != null) buyMenuPanel.SetActive(false);

            var player = GameManager.Instance?.playerObject;
            var look = player?.GetComponent<PlayerLook>();
            if (look != null) look.SetCursorLock(true);
        }

        public bool IsOpen => isOpen;

        void EnsureChrome()
        {
            if (chromeBuilt || buyMenuPanel == null) return;
            chromeBuilt = true;

            for (int i = buyMenuPanel.transform.childCount - 1; i >= 0; i--)
                Destroy(buyMenuPanel.transform.GetChild(i).gameObject);

            if (moneyDisplay != null) moneyDisplay.enabled = false;

            var bgImg = buyMenuPanel.GetComponent<Image>();
            if (bgImg != null) bgImg.color = Bg;

            // Resize so armor row doesn't clip on short aspect ratios.
            var panelRT = buyMenuPanel.GetComponent<RectTransform>();
            if (panelRT != null)
            {
                panelRT.anchorMin = new Vector2(0.22f, 0.05f);
                panelRT.anchorMax = new Vector2(0.78f, 0.95f);
                panelRT.offsetMin = Vector2.zero;
                panelRT.offsetMax = Vector2.zero;
            }

            // Header
            var header = NewRect("Header", buyMenuPanel.transform, new Vector2(0f, 0.9f), new Vector2(1f, 1f));
            var headerImg = header.gameObject.AddComponent<Image>();
            headerImg.color = HeaderBg;

            // Title
            var title = NewRect("Title", header, new Vector2(0f, 0f), new Vector2(0.5f, 1f));
            title.offsetMin = new Vector2(28, 0);
            title.offsetMax = new Vector2(-10, 0);
            var titleText = title.gameObject.AddComponent<TextMeshProUGUI>();
            titleText.text = "BUY MENU";
            titleText.font = TMP_Settings.defaultFontAsset;
            titleText.fontSize = 34;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextMain;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.characterSpacing = 10f;

            // Money pill
            var moneyHolder = NewRect("Money", header, new Vector2(0.5f, 0f), new Vector2(1f, 1f));
            moneyHolder.offsetMin = new Vector2(10, 10);
            moneyHolder.offsetMax = new Vector2(-28, -10);
            var money = moneyHolder.gameObject.AddComponent<TextMeshProUGUI>();
            money.text = "$0";
            money.font = TMP_Settings.defaultFontAsset;
            money.fontSize = 30;
            money.fontStyle = FontStyles.Bold;
            money.color = MoneyColor;
            money.alignment = TextAlignmentOptions.MidlineRight;
            moneyLabel = money;

            // Close hint
            var hint = NewRect("CloseHint", buyMenuPanel.transform, new Vector2(0f, 0.86f), new Vector2(1f, 0.9f));
            hint.offsetMin = new Vector2(28, 0);
            hint.offsetMax = new Vector2(-28, 0);
            var hintText = hint.gameObject.AddComponent<TextMeshProUGUI>();
            hintText.text = "Press  <b>B</b>  to close";
            hintText.font = TMP_Settings.defaultFontAsset;
            hintText.fontSize = 14;
            hintText.color = TextSub;
            hintText.alignment = TextAlignmentOptions.MidlineLeft;

            // Accent stripe under header
            var stripe = NewRect("AccentStripe", buyMenuPanel.transform, new Vector2(0f, 0.898f), new Vector2(1f, 0.902f));
            stripe.gameObject.AddComponent<Image>().color = Accent;

            // Scroll-free list container (single screen, no overflow expected)
            listContainer = NewRect("List", buyMenuPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 0.86f));
            listContainer.offsetMin = new Vector2(24, 18);
            listContainer.offsetMax = new Vector2(-24, -6);
            var listVlg = listContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            listVlg.spacing = 6;
            listVlg.childAlignment = TextAnchor.UpperCenter;
            listVlg.childControlWidth = true;
            listVlg.childControlHeight = false;
            listVlg.childForceExpandWidth = true;
            listVlg.childForceExpandHeight = false;

            weaponButtonParent = listContainer;
        }

        void PopulateWeapons()
        {
            if (listContainer == null) return;

            for (int i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);

            var db = GameManager.Instance?.weaponDatabase;
            var player = GameManager.Instance?.playerObject;
            var economy = player?.GetComponent<PlayerEconomy>();
            if (db == null || economy == null) return;

            if (moneyLabel != null)
                moneyLabel.text = $"${economy.money}";

            var playerHealth = player.GetComponent<PlayerHealth>();
            Team playerTeam = playerHealth != null ? playerHealth.team : GameManager.PlayerTeam;

            var sections = new List<(string title, Color color, List<WeaponData> items)>
            {
                ("PISTOLS",   CatPistol,   new List<WeaponData>()),
                ("RIFLES",    CatRifle,    new List<WeaponData>()),
                ("SNIPERS",   CatSniper,   new List<WeaponData>()),
                ("GRENADES",  CatGrenade,  new List<WeaponData>()),
            };

            foreach (var w in db.GetBuyableWeapons())
            {
                if (w.teamRestriction == TeamRestriction.TerroristOnly && playerTeam != Team.Terrorist) continue;
                if (w.teamRestriction == TeamRestriction.CounterTerroristOnly && playerTeam != Team.CounterTerrorist) continue;

                int idx = CategoryIndex(w);
                if (idx >= 0) sections[idx].items.Add(w);
            }

            foreach (var section in sections)
            {
                if (section.items.Count == 0) continue;
                BuildSectionHeader(section.title, section.color);
                BuildWeaponRows(section.items, section.color, economy);
            }

            // Armor section
            BuildSectionHeader("ARMOR", CatArmor);
            BuildArmorRow(economy);
        }

        int CategoryIndex(WeaponData w)
        {
            if (w.weaponType == WeaponType.Pistol) return 0;
            if (w.weaponType == WeaponType.Rifle) return 1;
            if (w.weaponType == WeaponType.Sniper) return 2;
            if (w.weaponType == WeaponType.Grenade) return 3;
            return -1;
        }

        void BuildSectionHeader(string title, Color color)
        {
            var row = new GameObject($"Section_{title}");
            row.AddComponent<RectTransform>();
            row.transform.SetParent(listContainer, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 22;
            le.minHeight = 22;

            // Margin instead of Image child to avoid a stretch-anchored
            // RectTransform escaping the layout group as a giant colour block.
            var t = row.AddComponent<TextMeshProUGUI>();
            t.text = title;
            t.font = TMP_Settings.defaultFontAsset;
            t.fontSize = 14;
            t.color = color;
            t.alignment = TextAlignmentOptions.BottomLeft;
            t.characterSpacing = 12f;
            t.fontStyle = FontStyles.Bold;
            t.margin = new Vector4(6, 4, 6, 0);
            t.raycastTarget = false;
        }

        void BuildWeaponRows(List<WeaponData> items, Color color, PlayerEconomy economy)
        {
            if (items.Count == 1)
            {
                var row = NewRow();
                BuildWeaponCard(row, items[0], color, economy);
                return;
            }

            for (int i = 0; i < items.Count; i += 2)
            {
                var row = NewRow();
                BuildWeaponCard(row, items[i], color, economy);
                if (i + 1 < items.Count) BuildWeaponCard(row, items[i + 1], color, economy);
                else BuildSpacer(row);
            }
        }

        Transform NewRow()
        {
            var go = new GameObject("Row");
            go.AddComponent<RectTransform>();
            go.transform.SetParent(listContainer, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 44;
            le.minHeight = 44;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            return go.transform;
        }

        void BuildSpacer(Transform row)
        {
            var go = new GameObject("Spacer");
            go.AddComponent<RectTransform>();
            go.transform.SetParent(row, false);
        }

        void BuildWeaponCard(Transform row, WeaponData weapon, Color color, PlayerEconomy economy)
        {
            bool canAfford = economy.CanAfford(weapon.cost);

            var go = new GameObject(weapon.weaponName);
            go.AddComponent<RectTransform>();
            go.transform.SetParent(row, false);

            var img = go.AddComponent<Image>();
            img.color = canAfford ? CardFill : CardLocked;

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor      = canAfford ? CardFill : CardLocked;
            cb.highlightedColor = canAfford ? CardFillAlt : CardLocked;
            cb.pressedColor     = canAfford ? new Color(CardFillAlt.r * 0.85f, CardFillAlt.g * 0.85f, CardFillAlt.b * 0.85f, 1f) : CardLocked;
            cb.disabledColor    = CardLocked;
            cb.fadeDuration     = 0.08f;
            btn.colors = cb;
            btn.targetGraphic = img;
            btn.interactable = canAfford;

            // Left accent stripe
            var stripe = NewRect("Accent", go.transform, new Vector2(0f, 0f), new Vector2(0f, 1f));
            stripe.sizeDelta = new Vector2(5, 0);
            stripe.anchoredPosition = new Vector2(2.5f, 0);
            stripe.gameObject.AddComponent<Image>().color = canAfford ? color : new Color(color.r, color.g, color.b, 0.35f);

            // Name
            var name = NewRect("Name", go.transform, new Vector2(0f, 0.5f), new Vector2(0.65f, 1f));
            name.offsetMin = new Vector2(16, 0);
            name.offsetMax = new Vector2(-4, -2);
            var nameText = name.gameObject.AddComponent<TextMeshProUGUI>();
            nameText.text = weapon.weaponName.ToUpperInvariant();
            nameText.font = TMP_Settings.defaultFontAsset;
            nameText.fontSize = 15;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = canAfford ? TextMain : new Color(TextMain.r, TextMain.g, TextMain.b, 0.5f);
            nameText.alignment = TextAlignmentOptions.BottomLeft;
            nameText.characterSpacing = 5f;
            nameText.raycastTarget = false;

            // Stats line
            var stats = NewRect("Stats", go.transform, new Vector2(0f, 0f), new Vector2(0.65f, 0.5f));
            stats.offsetMin = new Vector2(16, 2);
            stats.offsetMax = new Vector2(-4, 0);
            var statsText = stats.gameObject.AddComponent<TextMeshProUGUI>();
            statsText.text = BuildStatsString(weapon);
            statsText.font = TMP_Settings.defaultFontAsset;
            statsText.fontSize = 10;
            statsText.color = TextSub;
            statsText.alignment = TextAlignmentOptions.TopLeft;
            statsText.raycastTarget = false;

            // Price
            var price = NewRect("Price", go.transform, new Vector2(0.65f, 0f), new Vector2(1f, 1f));
            price.offsetMin = new Vector2(0, 0);
            price.offsetMax = new Vector2(-12, 0);
            var priceText = price.gameObject.AddComponent<TextMeshProUGUI>();
            priceText.text = $"${weapon.cost}";
            priceText.font = TMP_Settings.defaultFontAsset;
            priceText.fontSize = 18;
            priceText.fontStyle = FontStyles.Bold;
            priceText.color = canAfford ? MoneyColor : new Color(0.5f, 0.3f, 0.3f, 1f);
            priceText.alignment = TextAlignmentOptions.MidlineRight;
            priceText.raycastTarget = false;

            var captured = weapon;
            btn.onClick.AddListener(() =>
            {
                economy.TryBuy(captured);
                PopulateWeapons();
            });
        }

        void BuildArmorRow(PlayerEconomy economy)
        {
            var row = NewRow();
            BuildArmorCard(row, "Kevlar", 650, false, economy);
            BuildArmorCard(row, "Kevlar + Helmet", 1000, true, economy);
        }

        void BuildArmorCard(Transform row, string label, int cost, bool withHelmet, PlayerEconomy economy)
        {
            bool canAfford = economy.CanAfford(cost);

            var go = new GameObject(label);
            go.AddComponent<RectTransform>();
            go.transform.SetParent(row, false);

            var img = go.AddComponent<Image>();
            img.color = canAfford ? CardFill : CardLocked;

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor      = canAfford ? CardFill : CardLocked;
            cb.highlightedColor = canAfford ? CardFillAlt : CardLocked;
            cb.pressedColor     = canAfford ? new Color(CardFillAlt.r * 0.85f, CardFillAlt.g * 0.85f, CardFillAlt.b * 0.85f, 1f) : CardLocked;
            cb.fadeDuration     = 0.08f;
            btn.colors = cb;
            btn.targetGraphic = img;
            btn.interactable = canAfford;

            var stripe = NewRect("Accent", go.transform, new Vector2(0f, 0f), new Vector2(0f, 1f));
            stripe.sizeDelta = new Vector2(5, 0);
            stripe.anchoredPosition = new Vector2(2.5f, 0);
            stripe.gameObject.AddComponent<Image>().color = canAfford ? CatArmor : new Color(CatArmor.r, CatArmor.g, CatArmor.b, 0.35f);

            var name = NewRect("Name", go.transform, new Vector2(0f, 0f), new Vector2(0.65f, 1f));
            name.offsetMin = new Vector2(16, 0);
            name.offsetMax = new Vector2(-4, 0);
            var nameText = name.gameObject.AddComponent<TextMeshProUGUI>();
            nameText.text = label.ToUpperInvariant();
            nameText.font = TMP_Settings.defaultFontAsset;
            nameText.fontSize = 15;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = canAfford ? TextMain : new Color(TextMain.r, TextMain.g, TextMain.b, 0.5f);
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.characterSpacing = 5f;
            nameText.raycastTarget = false;

            var price = NewRect("Price", go.transform, new Vector2(0.65f, 0f), new Vector2(1f, 1f));
            price.offsetMax = new Vector2(-12, 0);
            var priceText = price.gameObject.AddComponent<TextMeshProUGUI>();
            priceText.text = $"${cost}";
            priceText.font = TMP_Settings.defaultFontAsset;
            priceText.fontSize = 18;
            priceText.fontStyle = FontStyles.Bold;
            priceText.color = canAfford ? MoneyColor : new Color(0.5f, 0.3f, 0.3f, 1f);
            priceText.alignment = TextAlignmentOptions.MidlineRight;
            priceText.raycastTarget = false;

            btn.onClick.AddListener(() =>
            {
                economy.TryBuyArmor(withHelmet);
                PopulateWeapons();
            });
        }

        string BuildStatsString(WeaponData w)
        {
            if (w.weaponType == WeaponType.Grenade)
                return $"DMG {(int)w.damage}  ·  AOE";
            if (w.weaponType == WeaponType.Knife)
                return "MELEE";
            // Guns
            return $"DMG {(int)w.damage}  ·  RPM {(int)w.fireRate}  ·  MAG {w.magazineSize}";
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
    }
}
