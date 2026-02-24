using UnityEngine;

namespace CounterSiege
{
    public class BotPlantState : IBotState
    {
        BotController bot;
        BombSite site;
        BombController bomb;
        bool planting;

        public BotPlantState(BotController bot, BombSite site)
        {
            this.bot = bot;
            this.site = site;
        }

        public void Enter()
        {
            bot.StopMoving();
            planting = false;
        }

        public void Tick()
        {
            if (bot.Sensors.HasVisibleEnemies)
            {
                if (bomb != null) bomb.CancelPlant();
                bot.stateMachine.ChangeState(new BotCombatState(bot));
                return;
            }

            if (!planting)
            {
                planting = true;
                var bombObj = bot.Inventory.DropBomb();
                if (bombObj != null)
                {
                    bomb = bombObj.GetComponent<BombController>();
                    bomb?.StartPlant(bot.gameObject, site);
                }
            }

            if (bomb != null && bomb.bombState == BombState.Planted)
            {
                bot.stateMachine.ChangeState(new BotNavigateState(bot));
            }
        }

        public void Exit() { }
    }
}
