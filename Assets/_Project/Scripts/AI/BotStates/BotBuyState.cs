using UnityEngine;

namespace CounterSiege
{
    public class BotBuyState : IBotState
    {
        BotController bot;
        bool bought;

        public BotBuyState(BotController bot) { this.bot = bot; }

        public void Enter()
        {
            bot.StopMoving();
            bought = false;
        }

        public void Tick()
        {
            if (bought) return;
            bought = true;

            var economy = bot.Economy;
            var inventory = bot.Inventory;
            var db = GameManager.Instance?.weaponDatabase;
            if (db == null || economy == null) return;

            // Try buy primary (AK for T, M4 for CT)
            string primaryName = bot.Health.team == Team.Terrorist ? "AK-47" : "M4A4";
            if (!inventory.HasWeaponInSlot(WeaponSlot.Primary))
            {
                var primary = db.GetWeapon(primaryName);
                if (primary != null && economy.CanAfford(primary.cost))
                    economy.TryBuy(primary);
            }

            // Try buy armor
            if (economy.CanAfford(1000))
                economy.TryBuyArmor(true);
            else if (economy.CanAfford(650))
                economy.TryBuyArmor(false);
        }

        public void Exit() { }
    }
}
