// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Frag grenade that counts down a fuse then does damage in a radius,
//          less damage further out, blocked by walls, and pushes loose physics
//          objects away from the blast."
// Modifications: Added the aftershock pass for chunks that spawn mid-explosion,
//                added the maxLaunchSpeed clamp so tiny chunks don't fly off
//                forever, added the fallback emissive flash when no VFX prefab
//                is wired.

using System.Collections.Generic;
using UnityEngine;

namespace CounterSiege
{
    public class GrenadeProjectile : MonoBehaviour
    {
        public float fuse = 2.4f;
        public float damage = 110f;
        public float radius = 6f;
        public float armorPen = 0.5f;
        // Keep low: RubbleDestructible chunks have a 0.1 kg mass floor, so 100
        // here gives a chunk Δv of 1000 m/s and it vanishes next frame.
        public float rigidbodyForce = 30f;
        public float aftershockMultiplier = 1.5f;
        // Cap to stop 0.1 kg mass-floor chunks reaching escape velocity.
        public float maxLaunchSpeed = 25f;
        public GameObject attacker;
        public string weaponName = "Frag";
        public GameObject explosionVFX;
        public AudioClip explosionSound;

        bool detonated;

        void Update()
        {
            if (detonated) return;
            fuse -= Time.deltaTime;
            if (fuse <= 0f) Detonate();
        }

        void Detonate()
        {
            detonated = true;

            if (explosionVFX != null)
                Instantiate(explosionVFX, transform.position, Quaternion.identity);
            else
                SpawnFallbackFlash(transform.position, radius);

            if (explosionSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(explosionSound, transform.position, 1f);

            var hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
            // Dedup by component instance, not transform.root: sibling
            // destructibles share a root and would only get damaged once.
            var damagedTargets = new HashSet<int>();
            int playersHit = 0;

            foreach (var h in hits)
            {
                Vector3 closest = h.ClosestPoint(transform.position);
                float dist = Vector3.Distance(transform.position, closest);
                float falloff = Mathf.Clamp01(1f - dist / radius);
                float dmg = damage * falloff;

                if (dmg > 0f)
                {
                    Vector3 dir = closest - transform.position;
                    float rayLen = dir.magnitude;
                    if (rayLen > 0.05f &&
                        Physics.Raycast(transform.position, dir.normalized, out RaycastHit los,
                            rayLen - 0.05f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (los.collider.transform.root != h.transform.root)
                            dmg = 0f;
                    }
                }

                if (dmg > 0f)
                {
                    var ph = h.GetComponentInParent<PlayerHealth>();
                    if (ph != null)
                    {
                        if (ph.gameObject != attacker && damagedTargets.Add(ph.GetInstanceID()))
                        {
                            HitZone zone = h.CompareTag("Head") ? HitZone.Head : HitZone.Chest;
                            ph.TakeDamage(new DamageInfo(dmg, attacker, zone, weaponName, armorPen));
                            playersHit++;
                        }
                    }
                    else
                    {
                        var dmgable = h.GetComponentInParent<IDamageable>();
                        if (dmgable is UnityEngine.Object o && damagedTargets.Add(o.GetInstanceID()))
                            dmgable.TakeDamage(new DamageInfo(dmg, attacker, HitZone.Chest, weaponName, armorPen));
                    }
                }

                var rb = h.attachedRigidbody;
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddExplosionForce(rigidbodyForce, transform.position, radius, 0.3f, ForceMode.Impulse);
                    ClampSpeed(rb, maxLaunchSpeed);
                }
            }

            Debug.Log($"[Grenade] detonated at {transform.position}: {hits.Length} colliders in radius, {playersHit} players damaged.");

            // Chunks spawned by destructibles killed in the first pass don't
            // exist yet for our OverlapSphere; aftershock fires next frame.
            SpawnAftershock(transform.position, radius * 1.2f, rigidbodyForce * aftershockMultiplier, maxLaunchSpeed);

            Destroy(gameObject);
        }

        static void SpawnAftershock(Vector3 pos, float radius, float force, float maxSpeed)
        {
            var go = new GameObject("GrenadeAftershock");
            go.transform.position = pos;
            var s = go.AddComponent<GrenadeAftershock>();
            s.center = pos;
            s.radius = radius;
            s.force = force;
            s.maxSpeed = maxSpeed;
        }

        internal static void ClampSpeed(Rigidbody rb, float maxSpeed)
        {
            if (maxSpeed <= 0f) return;
            Vector3 v = rb.linearVelocity;
            float sqr = v.sqrMagnitude;
            if (sqr > maxSpeed * maxSpeed)
                rb.linearVelocity = v * (maxSpeed / Mathf.Sqrt(sqr));
        }

        // Used when no explosionVFX prefab is wired.
        static void SpawnFallbackFlash(Vector3 pos, float radius)
        {
            var flash = new GameObject("GrenadeFlash");
            flash.transform.position = pos;

            // Sphere visual
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "FlashSphere";
            sphere.transform.SetParent(flash.transform, false);
            sphere.transform.localScale = Vector3.one * (radius * 0.3f);
            Object.Destroy(sphere.GetComponent<Collider>());
            var r = sphere.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_BaseColor", new Color(1f, 0.7f, 0.25f, 1f));
            mat.SetColor("_EmissionColor", new Color(8f, 5f, 1.5f, 1f));
            r.sharedMaterial = mat;

            // Flash point light
            var lightGO = new GameObject("FlashLight");
            lightGO.transform.SetParent(flash.transform, false);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.7f, 0.3f);
            light.intensity = 15f;
            light.range = radius * 3f;

            flash.AddComponent<GrenadeFlashAnimator>().Init(radius);
        }
    }

    public class GrenadeAftershock : MonoBehaviour
    {
        public Vector3 center;
        public float radius;
        public float force;
        public float maxSpeed = 25f;
        bool fired;

        void Update()
        {
            if (fired) { Destroy(gameObject); return; }
            fired = true;

            int launched = 0;
            var hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                var rb = h.attachedRigidbody;
                if (rb == null || rb.isKinematic) continue;
                rb.AddExplosionForce(force, center, radius, 0.6f, ForceMode.Impulse);
                GrenadeProjectile.ClampSpeed(rb, maxSpeed);
                launched++;
            }
            if (launched > 0)
                Debug.Log($"[Grenade] aftershock launched {launched} loose rigidbodies.");
        }
    }

    public class GrenadeFlashAnimator : MonoBehaviour
    {
        float radius;
        float age;
        const float duration = 0.45f;
        Renderer sphereRenderer;
        Light flashLight;

        public void Init(float r)
        {
            radius = r;
            sphereRenderer = GetComponentInChildren<Renderer>();
            flashLight = GetComponentInChildren<Light>();
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);

            if (sphereRenderer != null)
            {
                var s = sphereRenderer.transform;
                s.localScale = Vector3.one * Mathf.Lerp(radius * 0.3f, radius * 1.0f, t);
                if (sphereRenderer.sharedMaterial != null)
                {
                    var c = Color.Lerp(new Color(8f, 5f, 1.5f, 1f), new Color(0f, 0f, 0f, 0f), t);
                    sphereRenderer.sharedMaterial.SetColor("_EmissionColor", c);
                }
            }

            if (flashLight != null)
                flashLight.intensity = Mathf.Lerp(15f, 0f, t);

            if (age >= duration) Destroy(gameObject);
        }
    }
}
