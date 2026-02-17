using System.Collections.Generic;
using UnityEngine;

namespace CounterSiege
{
    public class ScoreManager : MonoBehaviour
    {
        public class PlayerStats
        {
            public string playerName;
            public Team team;
            public int kills;
            public int deaths;
            public int assists;
            public int money;
        }

        Dictionary<GameObject, PlayerStats> stats = new();

        void Start()
        {
            EventBus.OnKill += HandleKill;
        }

        void OnDestroy()
        {
            EventBus.OnKill -= HandleKill;
        }

        public void RegisterPlayer(GameObject player, string name, Team team)
        {
            stats[player] = new PlayerStats
            {
                playerName = name,
                team = team
            };
        }

        void HandleKill(GameObject victim, GameObject killer, string weapon, HitZone hitZone)
        {
            if (stats.ContainsKey(victim))
                stats[victim].deaths++;

            if (killer != null && stats.ContainsKey(killer))
                stats[killer].kills++;

            EventBus.OnScoreChanged?.Invoke();
        }

        public PlayerStats GetStats(GameObject player)
        {
            return stats.GetValueOrDefault(player);
        }

        public List<PlayerStats> GetTeamStats(Team team)
        {
            var list = new List<PlayerStats>();
            foreach (var kvp in stats)
            {
                if (kvp.Value.team == team)
                    list.Add(kvp.Value);
            }
            list.Sort((a, b) => b.kills.CompareTo(a.kills));
            return list;
        }

        public List<PlayerStats> GetAllStats()
        {
            var list = new List<PlayerStats>(stats.Values);
            list.Sort((a, b) => b.kills.CompareTo(a.kills));
            return list;
        }
    }
}
