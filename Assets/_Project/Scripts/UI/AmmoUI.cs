using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class AmmoUI : MonoBehaviour
    {
        public Text ammoText;

        void Start()
        {
            EventBus.OnAmmoChanged += UpdateAmmo;
        }

        void OnDestroy()
        {
            EventBus.OnAmmoChanged -= UpdateAmmo;
        }

        void UpdateAmmo(GameObject entity, int magazine, int reserve)
        {
            if (entity != GameManager.Instance?.playerObject) return;

            if (ammoText != null)
                ammoText.text = $"{magazine} / {reserve}";
        }
    }
}
