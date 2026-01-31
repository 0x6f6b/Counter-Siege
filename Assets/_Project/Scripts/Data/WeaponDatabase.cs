using UnityEngine;

namespace CounterSiege
{
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Counter Siege/Weapon Database")]
    public class WeaponDatabase : ScriptableObject
    {
        public WeaponData[] allWeapons;

        public WeaponData GetWeapon(string weaponName)
        {
            foreach (var w in allWeapons)
            {
                if (w.weaponName == weaponName) return w;
            }
            return null;
        }

        public WeaponData[] GetWeaponsBySlot(WeaponSlot slot)
        {
            var list = new System.Collections.Generic.List<WeaponData>();
            foreach (var w in allWeapons)
            {
                if (w.slot == slot) list.Add(w);
            }
            return list.ToArray();
        }

        public WeaponData[] GetBuyableWeapons()
        {
            var list = new System.Collections.Generic.List<WeaponData>();
            foreach (var w in allWeapons)
            {
                if (w.cost > 0) list.Add(w);
            }
            return list.ToArray();
        }
    }
}
