// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Implement a CS:GO-style weapon accuracy system with base spread, movement
//          inaccuracy, fire inaccuracy with exponential recovery, and circular gaussian
//          spread distribution."
// Modifications: Added hit zone detection by height ratio, integrated with WeaponData
//                ScriptableObject for data-driven configuration, added bot movement
//                inaccuracy path via NavMeshAgent velocity check.

using UnityEngine;

namespace CounterSiege
{
    public class GunBase : WeaponBase
    {
        [HideInInspector] public int currentAmmo;
        [HideInInspector] public int currentReserve;
        [HideInInspector] public RecoilPattern recoilPattern;

        float fireCooldown;
        int consecutiveShotIndex;
        bool isReloading;
        float reloadTimer;
        float drawTimer;

        // Fire inaccuracy tracking
        float fireInaccuracy;
        float timeSinceLastShot;

        // Tracer
        static GameObject tracerPrefab;
        static GameObject impactPrefab;

        public void Initialize(int ammo = -1, int reserve = -1)
        {
            currentAmmo = ammo >= 0 ? ammo : weaponData.magazineSize;
            currentReserve = reserve >= 0 ? reserve : weaponData.reserveAmmo;
        }

        public override void OnEquip(GameObject owner)
        {
            base.OnEquip(owner);
            drawTimer = weaponData.drawTime;
            UpdateAmmoUI();

            if (weaponData.equipSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(weaponData.equipSound, transform.position, 0.3f);
        }

        public override void Tick()
        {
            if (fireCooldown > 0) fireCooldown -= Time.deltaTime;
            if (drawTimer > 0) drawTimer -= Time.deltaTime;

            if (isReloading)
            {
                reloadTimer -= Time.deltaTime;
                if (reloadTimer <= 0)
                    FinishReload();
            }

            // Decay fire inaccuracy exponentially
            if (fireInaccuracy > 0f)
            {
                timeSinceLastShot += Time.deltaTime;
                fireInaccuracy *= Mathf.Exp(-Time.deltaTime / weaponData.inaccuracyRecoveryTime);
                if (fireInaccuracy < 0.001f) fireInaccuracy = 0f;
            }

            // Reset recoil index after not firing for a bit
            if (fireCooldown <= -0.3f)
                consecutiveShotIndex = 0;
        }

        public override void PrimaryFire()
        {
            if (drawTimer > 0 || isReloading) return;
            if (fireCooldown > 0) return;
            if (currentAmmo <= 0)
            {
                if (currentReserve > 0) Reload();
                return;
            }

            currentAmmo--;
            fireCooldown = weaponData.FireInterval;
            timeSinceLastShot = 0f;

            // Get camera transform for raycast
            Transform cam = GetCameraTransform();
            if (cam == null) return;

            // Calculate spread BEFORE adding fire inaccuracy so first shot is accurate
            float spread = CalculateSpread();
            fireInaccuracy += weaponData.inaccuracyFire;
            Vector3 direction = cam.forward;
            if (spread > 0.0001f)
            {
                float theta = Random.Range(0f, 2f * Mathf.PI);
                float r = Random.Range(0f, 1f) + Random.Range(0f, 1f);
                if (r > 1f) r = 2f - r;
                direction += cam.right * (r * spread * Mathf.Cos(theta));
                direction += cam.up * (r * spread * Mathf.Sin(theta));
            }
            direction.Normalize();

            // Raycast
            if (Physics.Raycast(cam.position, direction, out RaycastHit hit, weaponData.range, ~(1 << 7), QueryTriggerInteraction.Ignore))
            {
                // Check hit target
                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (targetHealth != null && targetHealth.gameObject != owner)
                {
                    HitZone zone = DetermineHitZone(hit);
                    var dmgInfo = new DamageInfo(
                        weaponData.damage, owner, zone,
                        weaponData.weaponName, weaponData.armorPenetration
                    );
                    targetHealth.TakeDamage(dmgInfo);
                }

                SpawnImpact(hit.point, hit.normal);
                SpawnTracer(GetMuzzlePosition(), hit.point);

                // Impact sound
                if (weaponData.impactSounds != null && weaponData.impactSounds.Length > 0 && AudioManager.Instance != null)
                {
                    var clip = weaponData.impactSounds[Random.Range(0, weaponData.impactSounds.Length)];
                    AudioManager.Instance.PlaySFX(clip, hit.point, 0.25f);
                }
            }
            else
            {
                SpawnTracer(GetMuzzlePosition(), cam.position + direction * weaponData.range);
            }

            // Recoil
            ApplyRecoil();

            // Fire sound
            if (weaponData.fireSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(weaponData.fireSound, transform.position, 0.5f);

            // Fire event
            EventBus.OnWeaponFired?.Invoke(owner, transform.position);
            consecutiveShotIndex++;
            UpdateAmmoUI();
        }

        public override void Reload()
        {
            if (isReloading) return;
            if (currentAmmo >= weaponData.magazineSize) return;
            if (currentReserve <= 0) return;

            isReloading = true;
            reloadTimer = weaponData.reloadTime;

            if (weaponData.reloadSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(weaponData.reloadSound, transform.position, 0.35f);
        }

        void FinishReload()
        {
            isReloading = false;
            int needed = weaponData.magazineSize - currentAmmo;
            int transfer = Mathf.Min(needed, currentReserve);
            currentAmmo += transfer;
            currentReserve -= transfer;
            UpdateAmmoUI();
        }

        protected virtual float CalculateSpread()
        {
            float totalDegrees = weaponData.spreadBase + GetMovementInaccuracy() + fireInaccuracy;
            return totalDegrees * Mathf.Deg2Rad;
        }

        protected virtual float GetMovementInaccuracy()
        {
            var pc = owner?.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (!pc.IsGrounded)
                    return weaponData.inaccuracyJump;
                if (pc.IsCrouching && pc.IsMoving)
                    return weaponData.inaccuracyCrouchMove;
                if (pc.IsCrouching)
                    return weaponData.inaccuracyCrouch;
                if (pc.IsMoving)
                    return weaponData.inaccuracyMove;
                return weaponData.inaccuracyStand;
            }

            // Bot path: check NavMeshAgent velocity
            var bot = owner?.GetComponent<BotController>();
            if (bot != null && bot.Agent != null)
            {
                if (bot.Agent.velocity.sqrMagnitude > 0.25f)
                    return weaponData.inaccuracyMove;
                return weaponData.inaccuracyStand;
            }

            return weaponData.inaccuracyStand;
        }

        public float CurrentInaccuracyDegrees =>
            weaponData.spreadBase + GetMovementInaccuracy() + fireInaccuracy;

        HitZone DetermineHitZone(RaycastHit hit)
        {
            if (hit.collider.CompareTag("Head")) return HitZone.Head;

            // Estimate from hit point relative to target
            var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            if (targetHealth != null)
            {
                float relativeHeight = hit.point.y - targetHealth.transform.position.y;
                float charHeight = 2f;
                float ratio = relativeHeight / charHeight;

                if (ratio > 0.8f) return HitZone.Head;
                if (ratio > 0.5f) return HitZone.Chest;
                if (ratio > 0.3f) return HitZone.Stomach;
                return HitZone.Legs;
            }

            return HitZone.Chest;
        }

        void ApplyRecoil()
        {
            var look = owner?.GetComponent<PlayerLook>();
            if (look == null) return;

            Vector2 recoil;
            if (recoilPattern != null)
                recoil = recoilPattern.GetOffset(consecutiveShotIndex);
            else
                recoil = new Vector2(Random.Range(-0.2f, 0.2f), 0.5f);

            look.AddRecoil(recoil);
        }

        Transform GetCameraTransform()
        {
            var look = owner?.GetComponent<PlayerLook>();
            if (look != null && look.CameraTransform != null)
                return look.CameraTransform;

            // Bot: use owner transform
            return owner?.transform;
        }

        Vector3 GetMuzzlePosition()
        {
            Transform cam = GetCameraTransform();
            if (cam != null) return cam.position + cam.forward * 0.5f;
            return transform.position;
        }

        void SpawnTracer(Vector3 start, Vector3 end)
        {
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPositions(new[] { start, end });
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.9f, 0.5f, 1f);
            lr.endColor = new Color(1f, 0.9f, 0.5f, 0f);
            Destroy(go, 0.1f);
        }

        void SpawnImpact(Vector3 position, Vector3 normal)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.1f;
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Sprites/Default"));
            r.material.color = new Color(1f, 0.8f, 0f, 1f);
            Destroy(go, 0.3f);
        }

        void UpdateAmmoUI()
        {
            EventBus.OnAmmoChanged?.Invoke(currentAmmo, currentReserve);
        }

        public void RefillReserve()
        {
            currentReserve = weaponData.reserveAmmo;
        }

        public override int CurrentAmmo => currentAmmo;
        public override int CurrentReserve => currentReserve;
        public bool IsReloading => isReloading;
    }
}
