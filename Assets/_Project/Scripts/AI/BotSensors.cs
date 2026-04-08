// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Bot sensors for vision (FOV plus raycast LOS) and hearing
//          (gunshots in range). Should track visible enemies and remember
//          where it last heard something."
// Modifications: Wired hearing to EventBus.OnWeaponFired so only gunshots
//                count, added the friendly-fire team check, masked the Player
//                layer out of the LOS linecast.

using System.Collections.Generic;
using UnityEngine;

namespace CounterSiege
{
    public class BotSensors : MonoBehaviour
    {
        public float visionRange = 30f;
        public float visionAngle = 110f;
        public float hearingRange = 40f;
        public float updateInterval = 0.2f;

        PlayerHealth myHealth;
        List<GameObject> visibleEnemies = new();
        float updateTimer;
        Vector3 lastHeardPosition;
        bool heardSomething;

        void Awake()
        {
            myHealth = GetComponent<PlayerHealth>();
        }

        void Start()
        {
            EventBus.OnWeaponFired += OnWeaponFired;
        }

        void OnDestroy()
        {
            EventBus.OnWeaponFired -= OnWeaponFired;
        }

        public void UpdateSensors()
        {
            updateTimer -= Time.deltaTime;
            if (updateTimer <= 0)
            {
                updateTimer = updateInterval;
                ScanForEnemies();
            }
        }

        void ScanForEnemies()
        {
            visibleEnemies.Clear();

            var allPlayers = GameManager.Instance?.teamManager?.GetAllPlayers();
            if (allPlayers == null) return;

            foreach (var player in allPlayers)
            {
                if (player == gameObject) continue;

                var ph = player.GetComponent<PlayerHealth>();
                if (ph == null || ph.isDead) continue;
                if (ph.team == myHealth.team) continue;

                // Range check
                Vector3 toTarget = player.transform.position - transform.position;
                float dist = toTarget.magnitude;
                if (dist > visionRange) continue;

                // Angle check
                float angle = Vector3.Angle(transform.forward, toTarget.normalized);
                if (angle > visionAngle * 0.5f) continue;

                // LOS check
                Vector3 eyePos = transform.position + Vector3.up * 1.6f;
                Vector3 targetPos = player.transform.position + Vector3.up * 1f;
                if (Physics.Linecast(eyePos, targetPos, ~(1 << LayerMask.NameToLayer("Player")), QueryTriggerInteraction.Ignore))
                    continue;

                visibleEnemies.Add(player);
            }
        }

        void OnWeaponFired(GameObject shooter, Vector3 position)
        {
            if (shooter == gameObject) return;
            var ph = shooter.GetComponent<PlayerHealth>();
            if (ph != null && ph.team == myHealth.team) return;

            float dist = Vector3.Distance(transform.position, position);
            if (dist < hearingRange)
            {
                lastHeardPosition = position;
                heardSomething = true;
            }
        }

        public GameObject GetClosestVisibleEnemy()
        {
            if (visibleEnemies.Count == 0) return null;

            GameObject closest = null;
            float closestDist = float.MaxValue;
            foreach (var enemy in visibleEnemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy;
                }
            }
            return closest;
        }

        public bool HasVisibleEnemies => visibleEnemies.Count > 0;
        public List<GameObject> VisibleEnemies => visibleEnemies;

        public bool TryGetHeardPosition(out Vector3 pos)
        {
            pos = lastHeardPosition;
            if (heardSomething)
            {
                heardSomething = false;
                return true;
            }
            return false;
        }
    }
}
