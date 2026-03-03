using UnityEngine;

namespace CounterSiege
{
    public class SniperRifle : GunBase
    {
        AudioClip scopeSound;

        int scopeLevel; // 0 = unscoped, 1 = first zoom, 2 = second zoom
        float boltDelay;
        Camera mainCamera;
        float defaultFOV = 60f;

        // Scope animation
        float scopeAnimProgress = 1f; // 0 = start, 1 = complete
        float scopeAnimStartFOV;
        float scopeAnimTargetFOV;
        float scopeAnimDuration;
        bool isScopingIn;

        const float FIRST_SCOPE_TIME = 0.3f;
        const float SECOND_SCOPE_TIME = 0.4f;

        public override void OnEquip(GameObject owner)
        {
            base.OnEquip(owner);
            scopeLevel = 0;
            scopeAnimProgress = 1f;
            isScopingIn = false;
            mainCamera = Camera.main;
            if (mainCamera != null) defaultFOV = mainCamera.fieldOfView;

            if (scopeSound == null)
                scopeSound = Resources.Load<AudioClip>("Audio/zoom");
        }

        public override void OnUnequip()
        {
            if (scopeLevel > 0) Unscope();
            base.OnUnequip();
        }

        public override void Tick()
        {
            base.Tick();

            if (boltDelay > 0)
                boltDelay -= Time.deltaTime;

            // Animate FOV during scope-in
            if (scopeAnimProgress < 1f)
            {
                scopeAnimProgress += Time.deltaTime / scopeAnimDuration;
                if (scopeAnimProgress >= 1f)
                    scopeAnimProgress = 1f;

                float t = SmoothStep(scopeAnimProgress);
                float fov = Mathf.Lerp(scopeAnimStartFOV, scopeAnimTargetFOV, t);
                if (mainCamera != null) mainCamera.fieldOfView = fov;

                UpdateSensitivity(fov);
            }
        }

        public override void PrimaryFire()
        {
            if (boltDelay > 0) return;
            base.PrimaryFire();
            boltDelay = 1.5f;
            if (scopeLevel > 0) Unscope();
        }

        public override void SecondaryFire()
        {
            if (weaponData.scopeZoomFOV <= 0) return;

            // Cycle: unscoped -> first zoom -> second zoom -> unscoped
            if (scopeLevel == 0)
                EnterScope(1);
            else if (scopeLevel == 1 && weaponData.scopeSecondZoomFOV > 0)
                EnterScope(2);
            else
                Unscope();
        }

        void EnterScope(int level)
        {
            float targetFOV = level == 1 ? weaponData.scopeZoomFOV : weaponData.scopeSecondZoomFOV;
            float currentFOV = mainCamera != null ? mainCamera.fieldOfView : defaultFOV;

            scopeLevel = level;
            scopeAnimStartFOV = currentFOV;
            scopeAnimTargetFOV = targetFOV;
            scopeAnimDuration = level == 1 ? FIRST_SCOPE_TIME : SECOND_SCOPE_TIME;
            scopeAnimProgress = 0f;
            isScopingIn = true;

            if (scopeSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX2D(scopeSound, 0.3f);

            EventBus.OnScopeChanged?.Invoke(true, level);
        }

        void Unscope()
        {
            scopeLevel = 0;
            isScopingIn = false;
            scopeAnimProgress = 1f;

            // Instant FOV reset (matches CS:GO behavior)
            if (mainCamera != null) mainCamera.fieldOfView = defaultFOV;
            UpdateSensitivity(defaultFOV);

            if (scopeSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX2D(scopeSound, 0.3f);

            EventBus.OnScopeChanged?.Invoke(false, 0);
        }

        void UpdateSensitivity(float currentFOV)
        {
            var look = owner?.GetComponent<PlayerLook>();
            if (look != null)
                look.aimSensitivityMultiplier = scopeLevel > 0
                    ? currentFOV / defaultFOV
                    : 1f;
        }

        protected override float GetMovementInaccuracy()
        {
            var pc = owner?.GetComponent<PlayerController>();
            if (pc != null && !pc.IsGrounded)
                return weaponData.inaccuracyJump;

            if (scopeLevel > 0)
            {
                float scopedAccuracy = GetScopedAccuracy();

                // During scope-in animation, interpolate between unscoped and scoped accuracy
                if (scopeAnimProgress < 1f && isScopingIn)
                {
                    float unscopedAccuracy = base.GetMovementInaccuracy();
                    return Mathf.Lerp(unscopedAccuracy, scopedAccuracy, scopeAnimProgress);
                }

                return scopedAccuracy;
            }

            return base.GetMovementInaccuracy();
        }

        float GetScopedAccuracy()
        {
            bool moving = false;
            var pc = owner?.GetComponent<PlayerController>();
            if (pc != null)
                moving = pc.IsMoving;
            else
            {
                var bot = owner?.GetComponent<BotController>();
                if (bot != null && bot.Agent != null)
                    moving = bot.Agent.velocity.sqrMagnitude > 0.25f;
            }

            if (moving && weaponData.inaccuracyScopedMove >= 0f)
                return weaponData.inaccuracyScopedMove;
            if (!moving && weaponData.inaccuracyScopedStand >= 0f)
                return weaponData.inaccuracyScopedStand;

            return base.GetMovementInaccuracy();
        }

        static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public bool IsScoped => scopeLevel > 0;
        public int ScopeLevel => scopeLevel;
        public float ScopeAnimProgress => scopeAnimProgress;
    }
}
