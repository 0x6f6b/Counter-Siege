using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class HealthArmorUI : MonoBehaviour
    {
        public Text healthText;
        public Text armorText;

        void Start()
        {
            EventBus.OnHealthChanged += UpdateDisplay;
        }

        void OnDestroy()
        {
            EventBus.OnHealthChanged -= UpdateDisplay;
        }

        void UpdateDisplay(int health, int armor)
        {
            if (healthText != null)
                healthText.text = health.ToString();
            if (armorText != null)
                armorText.text = armor.ToString();
        }
    }
}
