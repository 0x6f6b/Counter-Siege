using UnityEngine;

namespace CounterSiege
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public int maxHealth = 100;
        public int maxArmor = 100;

        [HideInInspector] public int currentHealth;
        [HideInInspector] public int currentArmor;
        [HideInInspector] public bool hasHelmet;
        [HideInInspector] public Team team;
        [HideInInspector] public bool isDead;
        [HideInInspector] public string playerName;

        public void Initialize(Team team, string name)
        {
            this.team = team;
            playerName = name;
            ResetHealth();
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isDead = false;
            EventBus.OnHealthChanged?.Invoke(gameObject, currentHealth, currentArmor);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (isDead) return;

            float multiplier = GetHitZoneMultiplier(info.hitZone);
            // Helmet reduces head multiplier
            if (info.hitZone == HitZone.Head && hasHelmet)
                multiplier *= 0.5f;

            float rawDamage = info.damage * multiplier;

            // Armor absorption
            float finalDamage = rawDamage;
            if (currentArmor > 0 && info.hitZone != HitZone.Legs)
            {
                float absorbed = rawDamage * (1f - info.armorPenetration) * 0.66f;
                float armorDamage = absorbed * 0.5f;
                currentArmor = Mathf.Max(0, currentArmor - Mathf.CeilToInt(armorDamage));
                finalDamage = rawDamage - absorbed;
            }

            currentHealth = Mathf.Max(0, currentHealth - Mathf.CeilToInt(finalDamage));
            EventBus.OnHealthChanged?.Invoke(gameObject, currentHealth, currentArmor);

            var look = GetComponent<PlayerLook>();
            if (look != null) look.AddHitKick(Mathf.Clamp01(finalDamage / 50f));

            if (currentHealth <= 0)
                Die(info);
        }

        float GetHitZoneMultiplier(HitZone zone)
        {
            return zone switch
            {
                HitZone.Head => 4f,
                HitZone.Chest => 1f,
                HitZone.Stomach => 1.25f,
                HitZone.Legs => 0.75f,
                _ => 1f
            };
        }

        void Die(DamageInfo info)
        {
            isDead = true;
            EventBus.OnPlayerDied?.Invoke(gameObject, info);
            EventBus.OnKill?.Invoke(gameObject, info.attacker, info.weaponName, info.hitZone);

            // Disable movement
            var pc = GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            var pl = GetComponent<PlayerLook>();
            if (pl != null) pl.enabled = false;

            // Drop weapons
            var inv = GetComponent<PlayerInventory>();
            if (inv != null) inv.DropAllWeapons();

            // Disable colliders
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Disable renderer (simple death - just hide)
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        public void GiveArmor(bool withHelmet)
        {
            currentArmor = maxArmor;
            if (withHelmet) hasHelmet = true;
            EventBus.OnHealthChanged?.Invoke(gameObject, currentHealth, currentArmor);
        }
    }
}
