using UnityEngine;

namespace CounterSiege
{
    public class BotNavigateState : IBotState
    {
        BotController bot;
        Vector3 destination;
        bool hasDestination;
        float retargetTimer;

        public BotNavigateState(BotController bot) { this.bot = bot; }

        public void Enter()
        {
            hasDestination = false;
            retargetTimer = 0f;
            ChooseDestination();
        }

        public void Tick()
        {
            if (bot.isFrozen) return;

            // Drop into combat the moment a target enters our FOV.
            if (bot.Sensors.HasVisibleEnemies)
            {
                bot.StopMoving();
                var stateMachine = GetStateMachine();
                stateMachine?.ChangeState(new BotCombatState(bot));
                return;
            }

            // Reroute toward gunshot we just heard.
            if (bot.Sensors.TryGetHeardPosition(out Vector3 heardPos))
            {
                destination = heardPos;
                hasDestination = true;
                bot.MoveTo(destination);
            }

            // T with the bomb close to a site: switch into planting.
            if (bot.Health.team == Team.Terrorist && bot.Inventory.HasBomb)
            {
                var sites = Object.FindObjectsByType<BombSite>(FindObjectsSortMode.None);
                foreach (var site in sites)
                {
                    if (Vector3.Distance(bot.transform.position, site.transform.position) < 3f)
                    {
                        var stateMachine = GetStateMachine();
                        stateMachine?.ChangeState(new BotPlantState(bot, site));
                        return;
                    }
                }
            }

            // CT close to a planted bomb: switch into defusing.
            if (bot.Health.team == Team.CounterTerrorist)
            {
                var bombs = Object.FindObjectsByType<BombController>(FindObjectsSortMode.None);
                foreach (var bomb in bombs)
                {
                    if (bomb.bombState == BombState.Planted &&
                        Vector3.Distance(bot.transform.position, bomb.transform.position) < 5f)
                    {
                        var stateMachine = GetStateMachine();
                        stateMachine?.ChangeState(new BotDefuseState(bot, bomb));
                        return;
                    }
                }
            }

            // Pick a new destination if we've arrived or the timer expired.
            retargetTimer -= Time.deltaTime;
            if (retargetTimer <= 0 || (!hasDestination) ||
                (hasDestination && Vector3.Distance(bot.transform.position, destination) < 2f))
            {
                ChooseDestination();
                retargetTimer = Random.Range(5f, 10f);
            }
        }

        void ChooseDestination()
        {
            if (bot.Health.team == Team.Terrorist)
            {
                // Go to a bomb site
                var sites = Object.FindObjectsByType<BombSite>(FindObjectsSortMode.None);
                if (sites.Length > 0)
                {
                    var site = sites[Random.Range(0, sites.Length)];
                    destination = site.transform.position + Random.insideUnitSphere * 3f;
                    destination.y = bot.transform.position.y;
                }
                else
                {
                    destination = bot.transform.position + Random.insideUnitSphere * 20f;
                    destination.y = bot.transform.position.y;
                }
            }
            else
            {
                // CT: if bomb planted, go to bomb
                var bombs = Object.FindObjectsByType<BombController>(FindObjectsSortMode.None);
                bool bombPlanted = false;
                foreach (var bomb in bombs)
                {
                    if (bomb.bombState == BombState.Planted)
                    {
                        destination = bomb.transform.position;
                        bombPlanted = true;
                        break;
                    }
                }

                if (!bombPlanted)
                {
                    // Patrol near a bomb site
                    var sites = Object.FindObjectsByType<BombSite>(FindObjectsSortMode.None);
                    if (sites.Length > 0)
                    {
                        var site = sites[Random.Range(0, sites.Length)];
                        destination = site.transform.position + Random.insideUnitSphere * 5f;
                        destination.y = bot.transform.position.y;
                    }
                    else
                    {
                        destination = bot.transform.position + Random.insideUnitSphere * 20f;
                        destination.y = bot.transform.position.y;
                    }
                }
            }

            hasDestination = true;
            bot.MoveTo(destination);
        }

        BotStateMachine GetStateMachine()
        {
            return bot.stateMachine;
        }

        public void Exit() { }
    }
}
