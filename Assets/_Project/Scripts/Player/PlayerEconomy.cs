using UnityEngine;

namespace CounterSiege
{
    public class PlayerEconomy : MonoBehaviour
    {
        public int money { get; private set; }

        PlayerInventory inventory;
        PlayerHealth health;

        void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            health = GetComponent<PlayerHealth>();
        }

        public void SetMoney(int amount)
        {
            money = Mathf.Clamp(amount, 0, GameManager.Instance?.settings?.maxMoney ?? 16000);
            EventBus.OnMoneyChanged?.Invoke(gameObject, money);
        }

        public void AddMoney(int amount)
        {
            SetMoney(money + amount);
        }

        public bool TryBuy(WeaponData weaponData)
        {
            if (weaponData.cost > money) return false;

            SetMoney(money - weaponData.cost);
            inventory.AddWeapon(weaponData);
            return true;
        }

        public bool TryBuyArmor(bool withHelmet)
        {
            int cost = withHelmet ? 1000 : 650;

            // Discount if already have partial armor
            if (health.currentArmor >= 100 && (!withHelmet || health.hasHelmet))
                return false;
            if (health.currentArmor >= 100 && withHelmet && !health.hasHelmet)
                cost = 350;

            if (cost > money) return false;

            SetMoney(money - cost);
            health.GiveArmor(withHelmet);
            return true;
        }

        public bool CanAfford(int cost) => money >= cost;
    }
}
