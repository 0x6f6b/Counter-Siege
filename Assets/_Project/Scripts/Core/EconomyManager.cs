using System.Collections.Generic;
using UnityEngine;

namespace CounterSiege
{
    public class EconomyManager : MonoBehaviour
    {
        List<GameObject> players = new();
        Dictionary<Team, int> consecutiveLosses = new();

        void Awake()
        {
            consecutiveLosses[Team.Terrorist] = 0;
            consecutiveLosses[Team.CounterTerrorist] = 0;
        }

        public void RegisterPlayer(GameObject player)
        {
            players.Add(player);
            var eco = player.GetComponent<PlayerEconomy>();
            if (eco != null)
                eco.SetMoney(GameManager.Instance?.settings?.startMoney ?? 800);
        }

        public void AwardRoundEnd(Team winner)
        {
            var settings = GameManager.Instance.settings;
            Team loser = winner == Team.Terrorist ? Team.CounterTerrorist : Team.Terrorist;

            consecutiveLosses[winner] = 0;
            consecutiveLosses[loser]++;

            int lossBonus = Mathf.Min(
                settings.lossBase + (consecutiveLosses[loser] - 1) * settings.lossIncrement,
                settings.maxLossBonus
            );

            foreach (var p in players)
            {
                var ph = p.GetComponent<PlayerHealth>();
                var eco = p.GetComponent<PlayerEconomy>();
                if (ph == null || eco == null) continue;

                if (ph.team == winner)
                    eco.AddMoney(settings.winReward);
                else
                    eco.AddMoney(lossBonus);
            }
        }

        public void ResetEconomy()
        {
            consecutiveLosses[Team.Terrorist] = 0;
            consecutiveLosses[Team.CounterTerrorist] = 0;

            foreach (var p in players)
            {
                var eco = p.GetComponent<PlayerEconomy>();
                if (eco != null) eco.SetMoney(GameManager.Instance?.settings?.startMoney ?? 800);
            }
        }
    }
}
