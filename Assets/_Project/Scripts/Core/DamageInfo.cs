using UnityEngine;

namespace CounterSiege
{
    public struct DamageInfo
    {
        public float damage;
        public GameObject attacker;
        public HitZone hitZone;
        public string weaponName;
        public float armorPenetration;

        public DamageInfo(float damage, GameObject attacker, HitZone hitZone, string weaponName, float armorPenetration = 0.5f)
        {
            this.damage = damage;
            this.attacker = attacker;
            this.hitZone = hitZone;
            this.weaponName = weaponName;
            this.armorPenetration = armorPenetration;
        }
    }
}
