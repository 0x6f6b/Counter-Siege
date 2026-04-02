using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class DamageVignetteUI : MonoBehaviour
    {
        public Image vignette;
        public float flashAlpha = 0.55f;
        public float fadeSpeed = 3.5f;

        int prevHealth = 100;
        float currentAlpha;

        void Start()
        {
            EventBus.OnHealthChanged += OnHealthChanged;
            SetAlpha(0f);
        }

        void OnDestroy()
        {
            EventBus.OnHealthChanged -= OnHealthChanged;
        }

        void Update()
        {
            if (currentAlpha > 0f)
            {
                currentAlpha = Mathf.Max(0f, currentAlpha - fadeSpeed * Time.deltaTime);
                SetAlpha(currentAlpha);
            }
        }

        void OnHealthChanged(GameObject entity, int health, int armor)
        {
            if (entity != GameManager.Instance?.playerObject) return;

            if (health < prevHealth)
            {
                float damage = prevHealth - health;
                currentAlpha = Mathf.Min(flashAlpha, 0.2f + damage / 100f * flashAlpha);
                SetAlpha(currentAlpha);
            }
            prevHealth = health;
        }

        void SetAlpha(float a)
        {
            if (vignette == null) return;
            var c = vignette.color;
            c.a = a;
            vignette.color = c;
        }
    }
}
