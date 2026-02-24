using UnityEngine;

namespace CounterSiege
{
    public class BotDefuseState : IBotState
    {
        BotController bot;
        BombController bomb;
        bool defusing;

        public BotDefuseState(BotController bot, BombController bomb)
        {
            this.bot = bot;
            this.bomb = bomb;
        }

        public void Enter()
        {
            bot.StopMoving();
            defusing = false;
        }

        public void Tick()
        {
            if (bomb == null || bomb.bombState == BombState.Defused || bomb.bombState == BombState.Exploded)
            {
                bot.stateMachine.ChangeState(new BotNavigateState(bot));
                return;
            }

            if (bot.Sensors.HasVisibleEnemies)
            {
                bomb.CancelDefuse();
                bot.stateMachine.ChangeState(new BotCombatState(bot));
                return;
            }

            if (!defusing && bomb.bombState == BombState.Planted)
            {
                defusing = true;
                bomb.StartDefuse(bot.gameObject);
            }
        }

        public void Exit()
        {
            if (bomb != null && bomb.bombState == BombState.Defusing)
                bomb.CancelDefuse();
        }
    }
}
