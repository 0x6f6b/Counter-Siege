// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Create a team manager that handles T/CT team rosters, spawn point assignment,
//          alive player counting, half-time side swaps, and bomb carrier selection."
// Modifications: Added round-robin spawn index cycling, fallback spawn positions,
//                and team reference updates on side swap.
using System.Collections.Generic;
using UnityEngine;

namespace CounterSiege
{
    public class TeamManager : MonoBehaviour
    {
        List<GameObject> terrorists = new();
        List<GameObject> counterTerrorists = new();
        int tSpawnIndex, ctSpawnIndex;

        public void RegisterPlayer(GameObject player, Team team)
        {
            if (team == Team.Terrorist)
                terrorists.Add(player);
            else
                counterTerrorists.Add(player);
        }

        public Vector3 GetSpawnPosition(Team team)
        {
            var spawns = FindSpawnPoints(team);
            if (spawns.Length == 0)
            {
                // Fallback positions
                return team == Team.Terrorist
                    ? new Vector3(0, 1, -60)
                    : new Vector3(0, 1, 60);
            }

            if (team == Team.Terrorist)
            {
                var pos = spawns[tSpawnIndex % spawns.Length].transform.position;
                tSpawnIndex++;
                return pos;
            }
            else
            {
                var pos = spawns[ctSpawnIndex % spawns.Length].transform.position;
                ctSpawnIndex++;
                return pos;
            }
        }

        SpawnPoint[] FindSpawnPoints(Team team)
        {
            var all = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            var list = new List<SpawnPoint>();
            foreach (var sp in all)
            {
                if (sp.team == team) list.Add(sp);
            }
            return list.ToArray();
        }

        public void ResetSpawnIndices()
        {
            tSpawnIndex = 0;
            ctSpawnIndex = 0;
        }

        public int GetAliveCount(Team team)
        {
            int count = 0;
            var list = team == Team.Terrorist ? terrorists : counterTerrorists;
            foreach (var p in list)
            {
                if (p == null) continue;
                var ph = p.GetComponent<PlayerHealth>();
                if (ph != null && !ph.isDead) count++;
            }
            return count;
        }

        public List<GameObject> GetTeam(Team team) =>
            team == Team.Terrorist ? terrorists : counterTerrorists;

        public List<GameObject> GetAllPlayers()
        {
            var all = new List<GameObject>();
            all.AddRange(terrorists);
            all.AddRange(counterTerrorists);
            return all;
        }

        public void SwapSides()
        {
            // Swap team lists
            (terrorists, counterTerrorists) = (counterTerrorists, terrorists);

            // Update health team references
            foreach (var p in terrorists)
            {
                var ph = p.GetComponent<PlayerHealth>();
                if (ph != null) ph.team = Team.Terrorist;
            }
            foreach (var p in counterTerrorists)
            {
                var ph = p.GetComponent<PlayerHealth>();
                if (ph != null) ph.team = Team.CounterTerrorist;
            }

            tSpawnIndex = 0;
            ctSpawnIndex = 0;
        }

        public void AssignBombCarrier()
        {
            var aliveTs = new List<GameObject>();
            GameObject humanCarrier = null;
            foreach (var p in terrorists)
            {
                var ph = p.GetComponent<PlayerHealth>();
                if (ph != null && !ph.isDead)
                {
                    aliveTs.Add(p);
                    // Human player has no BotController
                    if (p.GetComponent<BotController>() == null) humanCarrier = p;
                }
            }

            if (aliveTs.Count == 0) return;

            // Always give the bomb to the human player if they're on T.
            var carrier = humanCarrier ?? aliveTs[Random.Range(0, aliveTs.Count)];
            var inv = carrier.GetComponent<PlayerInventory>();
            if (inv != null) inv.GiveBomb();
        }
    }
}
