using UnityEngine;

namespace CounterSiege
{
    public class BotCombatState : IBotState
    {
        BotController bot;
        GameObject target;
        float fireTimer;
        float strafeTimer;
        float strafeDir;

        public BotCombatState(BotController bot) { this.bot = bot; }

        public void Enter()
        {
            bot.StopMoving();
            fireTimer = 0.3f; // Small reaction delay
            strafeTimer = 0;
        }

        public void Tick()
        {
            target = bot.Sensors.GetClosestVisibleEnemy();

            if (target == null)
            {
                // Lost target, go back to navigation
                bot.stateMachine.ChangeState(new BotNavigateState(bot));
                return;
            }

            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;

            // Aim at target
            bot.AimController.AimAt(targetPos);

            // Strafe
            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0)
            {
                strafeTimer = Random.Range(0.5f, 1.5f);
                strafeDir = Random.value > 0.5f ? 1f : -1f;
            }

            Vector3 strafeMove = bot.transform.right * strafeDir * 2f + bot.transform.position;
            bot.MoveTo(strafeMove);

            // Fire when aimed
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0 && bot.AimController.IsAimedAt(targetPos, 15f))
            {
                bot.FireWeapon();
                fireTimer = 0.15f;

                // Check if need reload
                var weapon = bot.Inventory.CurrentWeapon;
                if (weapon != null && weapon.CurrentAmmo == 0)
                    bot.ReloadWeapon();
            }
        }

        public void Exit() { }
    }
}
