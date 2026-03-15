using UnityEngine;

namespace CounterSiege
{
    public class Grenade : WeaponBase
    {
        [Header("Throw")]
        public float overhandSpeed = 22f;
        public float underhandSpeed = 8f;
        public float spawnOffset = 0.4f;

        [Header("Detonation")]
        public float fuseTime = 2.4f;
        public float damage = 200f;
        public float radius = 12f;
        public float armorPen = 0.5f;
        // RubbleDestructible chunks have a 0.1 kg mass floor. >100 sends
        // tiny chunks supersonic; ~40 launches typical 2-3 kg chunks well.
        public float rigidbodyForce = 40f;
        public float aftershockMultiplier = 1.5f;

        [Header("FX")]
        public GameObject explosionVFX;
        public AudioClip explosionSound;

        bool thrown;
        float drawTimer;

        public override void OnEquip(GameObject newOwner)
        {
            base.OnEquip(newOwner);
            drawTimer = weaponData != null ? weaponData.drawTime : 0.4f;
            thrown = false;

            if (weaponData != null && weaponData.equipSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(weaponData.equipSound, transform.position, 0.3f);
        }

        public override void Tick()
        {
            if (drawTimer > 0f) drawTimer -= Time.deltaTime;
        }

        public override void PrimaryFire() => Throw(overhandSpeed);
        public override void SecondaryFire() => Throw(underhandSpeed);

        void Throw(float speed)
        {
            if (thrown || drawTimer > 0f || owner == null) return;
            thrown = true;

            Transform cam = GetCamera();
            if (cam == null) { thrown = false; return; }

            Vector3 spawnPos = cam.position + cam.forward * spawnOffset;
            var go = new GameObject("FragGrenade_Projectile");
            go.transform.position = spawnPos;
            go.transform.rotation = Quaternion.LookRotation(cam.forward);

            // Reuse the view-model mesh as the projectile visual. The hand
            // alignment prefab has its mesh pivot offset above the wrapper
            // origin (so it sits in the hand); for a free-flying grenade we
            // need to re-center the visual on the physics sphere or it will
            // swing wildly around the rolling axis on the floor.
            if (weaponData != null && weaponData.viewModelPrefab != null)
            {
                var visual = Instantiate(weaponData.viewModelPrefab, go.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                StripVisual(visual);
                CenterVisualOnPhysicsOrigin(visual, go.transform.position);
            }
            else
            {
                var v = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                v.transform.SetParent(go.transform, false);
                v.transform.localScale = Vector3.one * 0.1f;
                Destroy(v.GetComponent<Collider>());
                v.GetComponent<Renderer>().material.color = new Color(0.25f, 0.3f, 0.18f);
            }

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.06f;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            // Continuous detection so the grenade doesn't tunnel through thin
            // floor colliders at 22 m/s throw speed.
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            // Damping so the grenade settles when it lands instead of
            // sliding/spinning forever (the default 0.05 is way too low).
            rb.linearDamping = 0.5f;
            rb.angularDamping = 4f;
            rb.linearVelocity = cam.forward * speed;
            rb.angularVelocity = Random.insideUnitSphere * 8f;

            // Don't collide with the thrower for the first few frames so the
            // grenade doesn't bounce off the player capsule on spawn.
            var ownerCol = owner.GetComponentInChildren<Collider>();
            if (ownerCol != null) Physics.IgnoreCollision(col, ownerCol, true);

            var proj = go.AddComponent<GrenadeProjectile>();
            proj.fuse = fuseTime;
            proj.damage = damage;
            proj.radius = radius;
            proj.armorPen = armorPen;
            proj.rigidbodyForce = rigidbodyForce;
            proj.aftershockMultiplier = aftershockMultiplier;
            proj.attacker = owner;
            proj.weaponName = weaponData != null ? weaponData.weaponName : "Frag";
            proj.explosionVFX = explosionVFX;
            proj.explosionSound = explosionSound != null
                ? explosionSound
                : (weaponData != null ? weaponData.fireSound : null);

            EventBus.OnWeaponFired?.Invoke(owner, transform.position);

            // Single-use: remove from inventory; PlayerInventory will switch us
            // off and destroy this WeaponBase's GameObject.
            var inv = owner.GetComponent<PlayerInventory>();
            if (inv != null) inv.ConsumeCurrentWeapon();
        }

        Transform GetCamera()
        {
            var look = owner != null ? owner.GetComponent<PlayerLook>() : null;
            if (look != null && look.CameraTransform != null) return look.CameraTransform;
            return owner != null ? owner.transform : null;
        }

        static void StripVisual(GameObject g)
        {
            foreach (var c in g.GetComponentsInChildren<Collider>(true)) Destroy(c);
            SetLayerRecursive(g, 0);
        }

        static void SetLayerRecursive(GameObject g, int layer)
        {
            g.layer = layer;
            foreach (Transform c in g.transform) SetLayerRecursive(c.gameObject, layer);
        }

        // Renderers in the view-model prefab are pivoted for in-hand display
        // (the mesh centre sits ~0.18 m above the wrapper origin). For a
        // projectile we want the mesh centre at the physics sphere origin so
        // the visual rolls in place. Shift the visual root by whatever offset
        // brings its combined bounds centre onto the physics origin.
        static void CenterVisualOnPhysicsOrigin(GameObject visual, Vector3 physicsOrigin)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            visual.transform.position += physicsOrigin - b.center;
        }
    }
}
