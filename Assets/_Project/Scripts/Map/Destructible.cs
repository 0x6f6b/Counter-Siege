// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Wrap the OpenFracture asset so a prop has health and breaks apart
//          when you shoot it dead, then the chunks clean themselves up after
//          a few seconds."
// Modifications: Added impact and destroy SFX hooks, added the FragmentCleanup
//                helper that survives the original object's destruction.

using System.Collections;
using UnityEngine;

namespace CounterSiege
{
    [RequireComponent(typeof(Fracture))]
    public class Destructible : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        public int maxHealth = 60;
        [HideInInspector] public int currentHealth;

        [Header("Cleanup")]
        [Tooltip("Seconds the chunks remain after fracturing before being destroyed.")]
        public float fragmentLifetime = 6f;

        [Header("Effects")]
        public AudioClip[] impactSounds;
        public AudioClip destroySound;
        public GameObject destroyVFX;

        Fracture fracture;
        bool isDead;

        void Awake()
        {
            fracture = GetComponent<Fracture>();
            currentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0, currentHealth - Mathf.CeilToInt(info.damage));

            if (impactSounds != null && impactSounds.Length > 0 && AudioManager.Instance != null)
            {
                var clip = impactSounds[Random.Range(0, impactSounds.Length)];
                AudioManager.Instance.PlaySFX(clip, transform.position, 0.25f);
            }

            if (currentHealth <= 0)
                Die();
        }

        void Die()
        {
            isDead = true;

            if (destroySound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(destroySound, transform.position, 0.6f);

            if (destroyVFX != null)
                Instantiate(destroyVFX, transform.position, Quaternion.identity);

            var cleanupGO = new GameObject($"{name}_FragCleanup");
            if (transform.parent != null) cleanupGO.transform.SetParent(transform.parent);
            cleanupGO.AddComponent<FragmentCleanup>().Init(transform.parent, $"{name}Fragments", fragmentLifetime);

            if (fracture != null)
            {
                fracture.CauseFracture();
            }
            else
            {
                Debug.LogWarning($"[Destructible {name}] No Fracture component, falling back to Destroy()");
                Destroy(gameObject);
            }
        }
    }

    public class FragmentCleanup : MonoBehaviour
    {
        string fragmentName;
        Transform searchParent;
        float lifetime;

        public void Init(Transform parent, string nameToFind, float lifetimeSeconds)
        {
            searchParent = parent;
            fragmentName = nameToFind;
            lifetime = lifetimeSeconds;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return null; // give Fragmenter a frame to create the root
            // Async fracturing may take a few frames; poll briefly
            Transform root = null;
            float timeout = 2f;
            while (timeout > 0f && root == null)
            {
                root = FindRoot();
                if (root != null) break;
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (root != null)
            {
                yield return new WaitForSeconds(lifetime);
                if (root != null) Destroy(root.gameObject);
            }
            Destroy(gameObject);
        }

        Transform FindRoot()
        {
            if (searchParent != null)
            {
                for (int i = 0; i < searchParent.childCount; i++)
                {
                    var c = searchParent.GetChild(i);
                    if (c.name == fragmentName) return c;
                }
                return null;
            }
            var found = GameObject.Find(fragmentName);
            return found != null ? found.transform : null;
        }
    }
}
