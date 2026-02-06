using UnityEngine;

namespace CounterSiege
{
    public abstract class WeaponBase : MonoBehaviour
    {
        public WeaponData weaponData;
        [HideInInspector] public GameObject owner;
        [HideInInspector] public bool isEquipped;

        public virtual void OnEquip(GameObject owner)
        {
            this.owner = owner;
            isEquipped = true;
            gameObject.SetActive(true);
        }

        public virtual void OnUnequip()
        {
            isEquipped = false;
            gameObject.SetActive(false);
        }

        public abstract void PrimaryFire();
        public virtual void SecondaryFire() { }
        public virtual void Tick() { }
        public virtual void Reload() { }

        public virtual int CurrentAmmo => -1;
        public virtual int CurrentReserve => -1;
    }
}
