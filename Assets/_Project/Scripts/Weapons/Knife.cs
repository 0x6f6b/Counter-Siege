using UnityEngine;

namespace CounterSiege
{
    public class Knife : WeaponBase
    {
        float attackCooldown;
        float primaryCooldown = 0.4f;
        float secondaryCooldown = 1f;

        public override void Tick()
        {
            if (attackCooldown > 0) attackCooldown -= Time.deltaTime;
        }

        public override void PrimaryFire()
        {
            if (attackCooldown > 0) return;
            attackCooldown = primaryCooldown;
            DoAttack(weaponData.damage, 2f);
        }

        public override void SecondaryFire()
        {
            if (attackCooldown > 0) return;
            attackCooldown = secondaryCooldown;
            DoAttack(55f, 2f);
        }

        void DoAttack(float damage, float range)
        {
            Transform cam = GetCameraTransform();
            if (cam == null) return;

            // Swing sound
            if (weaponData.fireSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(weaponData.fireSound, transform.position, 0.35f);

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, ~(1 << 7), QueryTriggerInteraction.Ignore))
            {
                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (targetHealth != null && targetHealth.gameObject != owner)
                {
                    // Check backstab
                    float dot = Vector3.Dot(owner.transform.forward, targetHealth.transform.forward);
                    float finalDamage = dot > 0.5f ? 180f : damage; // backstab

                    HitZone zone = hit.collider.CompareTag("Head") ? HitZone.Head : HitZone.Chest;
                    var dmgInfo = new DamageInfo(finalDamage, owner, zone, weaponData.weaponName, 1f);
                    targetHealth.TakeDamage(dmgInfo);

                    // Hit sound
                    if (weaponData.impactSounds != null && weaponData.impactSounds.Length > 0 && AudioManager.Instance != null)
                    {
                        var clip = weaponData.impactSounds[Random.Range(0, weaponData.impactSounds.Length)];
                        AudioManager.Instance.PlaySFX(clip, hit.point, 0.4f);
                    }
                }
            }

            EventBus.OnWeaponFired?.Invoke(owner, transform.position);
        }

        Transform GetCameraTransform()
        {
            var look = owner?.GetComponent<PlayerLook>();
            if (look != null && look.CameraTransform != null)
                return look.CameraTransform;
            return owner?.transform;
        }
    }
}
