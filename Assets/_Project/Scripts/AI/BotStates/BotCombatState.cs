using UnityEngine;

namespace CounterSiege
{
    public class BotCombatState : IBotState
    {
        const float TargetMemoryDuration = 1.5f;

        BotController bot;
        GameObject target;
        Vector3 lastKnownPos;
        float lastSeenTime;
        float fireTimer;
        float strafeTimer;
        float strafeDir;

        public BotCombatState(BotController bot) { this.bot = bot; }

        public void Enter()
        {
            bot.StopMoving();
            fireTimer = 0.3f;
            strafeTimer = 0f;
            lastSeenTime = Time.time;
        }

        public void Tick()
        {
            var visibleTarget = bot.Sensors.GetClosestVisibleEnemy();

            if (visibleTarget != null)
            {
                target = visibleTarget;
                lastKnownPos = target.transform.position + Vector3.up * 1.2f;
                lastSeenTime = Time.time;
            }
            else
            {
                target = null;
                if (Time.time - lastSeenTime > TargetMemoryDuration)
                {
                    bot.stateMachine.ChangeState(new BotNavigateState(bot));
                    return;
                }
            }

            Vector3 aimAt = target != null
                ? target.transform.position + Vector3.up * 1.2f
                : lastKnownPos;

            bot.AimController.AimAt(aimAt);

            if (target != null)
            {
                // Visible: strafe to make the bot harder to hit
                strafeTimer -= Time.deltaTime;
                if (strafeTimer <= 0)
                {
                    strafeTimer = Random.Range(0.5f, 1.5f);
                    strafeDir = Random.value > 0.5f ? 1f : -1f;
                }

                Vector3 strafeMove = bot.transform.right * strafeDir * 2f + bot.transform.position;
                bot.MoveTo(strafeMove);
            }
            else
            {
                // Lost sight: advance on last known position to flush them out
                bot.MoveTo(lastKnownPos);
            }

            // Fire only when target currently visible AND aimed
            fireTimer -= Time.deltaTime;
            if (target != null && fireTimer <= 0 && bot.AimController.IsAimedAt(aimAt, 15f))
            {
                bot.FireWeapon();
                fireTimer = 0.15f;

                var weapon = bot.Inventory.CurrentWeapon;
                if (weapon != null && weapon.CurrentAmmo == 0)
                    bot.ReloadWeapon();
            }
        }

        public void Exit() { }
    }
}
