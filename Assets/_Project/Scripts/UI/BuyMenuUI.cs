using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class BuyMenuUI : MonoBehaviour
    {
        public GameObject buyMenuPanel;
        public Transform weaponButtonParent;
        public Text moneyDisplay;

        bool isOpen;

        void Start()
        {
            if (buyMenuPanel != null)
                buyMenuPanel.SetActive(false);
        }

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        public void Open()
        {
            var rm = GameManager.Instance?.roundManager;
            if (rm == null || !rm.IsBuyTime) return;

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

        void PopulateWeapons()
        {
            if (weaponButtonParent == null) return;

            foreach (Transform child in weaponButtonParent)
                Destroy(child.gameObject);

            var db = GameManager.Instance?.weaponDatabase;
            var player = GameManager.Instance?.playerObject;
            var economy = player?.GetComponent<PlayerEconomy>();
            if (db == null || economy == null) return;

            if (moneyDisplay != null)
                moneyDisplay.text = $"${economy.money}";

            var playerHealth = player.GetComponent<PlayerHealth>();
            Team playerTeam = playerHealth != null ? playerHealth.team : GameManager.PlayerTeam;

            foreach (var weapon in db.GetBuyableWeapons())
            {
                if (weapon.teamRestriction == TeamRestriction.TerroristOnly && playerTeam != Team.Terrorist)
                    continue;
                if (weapon.teamRestriction == TeamRestriction.CounterTerroristOnly && playerTeam != Team.CounterTerrorist)
                    continue;

                CreateWeaponButton(weapon, economy);
            }

            // Armor buttons
            CreateArmorButton("Kevlar  $650", 650, false, economy);
            CreateArmorButton("Kevlar + Helmet  $1000", 1000, true, economy);
        }

        void CreateWeaponButton(WeaponData weapon, PlayerEconomy economy)
        {
            var buttonGO = new GameObject(weapon.weaponName);
            buttonGO.transform.SetParent(weaponButtonParent, false);

            var rect = buttonGO.AddComponent<RectTransform>();
            var le = buttonGO.AddComponent<LayoutElement>();
            le.preferredHeight = 40;

            bool canAfford = economy.CanAfford(weapon.cost);

            var image = buttonGO.AddComponent<Image>();
            image.color = canAfford ? new Color(0.2f, 0.2f, 0.2f, 0.8f) : new Color(0.3f, 0.1f, 0.1f, 0.8f);

            var button = buttonGO.AddComponent<Button>();
            button.interactable = canAfford;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            var text = textGO.AddComponent<Text>();
            text.text = $"{weapon.weaponName}  ${weapon.cost}";
            text.fontSize = 16;
            text.color = canAfford ? Color.white : Color.gray;
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (text.font == null)
                text.font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            var capturedWeapon = weapon;
            button.onClick.AddListener(() =>
            {
                economy.TryBuy(capturedWeapon);
                PopulateWeapons();
            });
        }

        void CreateArmorButton(string label, int cost, bool withHelmet, PlayerEconomy economy)
        {
            var buttonGO = new GameObject(label);
            buttonGO.transform.SetParent(weaponButtonParent, false);

            var le = buttonGO.AddComponent<LayoutElement>();
            le.preferredHeight = 40;

            bool canAfford = economy.CanAfford(cost);

            var image = buttonGO.AddComponent<Image>();
            image.color = canAfford ? new Color(0.15f, 0.2f, 0.3f, 0.8f) : new Color(0.2f, 0.1f, 0.1f, 0.8f);

            var button = buttonGO.AddComponent<Button>();
            button.interactable = canAfford;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            var text = textGO.AddComponent<Text>();
            text.text = label;
            text.fontSize = 16;
            text.color = canAfford ? Color.white : Color.gray;
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (text.font == null)
                text.font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            button.onClick.AddListener(() =>
            {
                economy.TryBuyArmor(withHelmet);
                PopulateWeapons();
            });
        }

        public bool IsOpen => isOpen;
    }
}
