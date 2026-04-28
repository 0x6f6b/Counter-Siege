using UnityEngine;
using UnityEngine.AI;

namespace CounterSiege
{
    // Drives Animator parameters from movement and weapon state.
    // Works with both CharacterController (player) and NavMeshAgent (bots).
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("References")]
        public Animator animator;
        public Transform modelRoot;

        // Cached components
        NavMeshAgent navAgent;
        CharacterController charController;
        PlayerController playerController;
        PlayerInventory inventory;

        // Animator parameter hashes
        static readonly int VelXHash = Animator.StringToHash("VelX");
        static readonly int VelZHash = Animator.StringToHash("VelZ");
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        static readonly int IsReloadingHash = Animator.StringToHash("IsReloading");
        static readonly int FireHash = Animator.StringToHash("Fire");
        static readonly int WeaponTypeHash = Animator.StringToHash("WeaponType");

        float smoothVelX;
        float smoothVelZ;
        const float SmoothRate = 10f;

        void Start()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator != null)
                animator.applyRootMotion = false;

            navAgent = GetComponent<NavMeshAgent>();
            charController = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();
            inventory = GetComponent<PlayerInventory>();

            EventBus.OnWeaponFired += OnWeaponFired;
        }

        void OnDestroy()
        {
            EventBus.OnWeaponFired -= OnWeaponFired;
        }

        void Update()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            if (animator.runtimeAnimatorController == null) return;

            // Get world velocity (horizontal only)
            Vector3 worldVel = Vector3.zero;
            float maxSpeed = 6.94f;

            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                worldVel = navAgent.velocity;
                worldVel.y = 0f;
                if (navAgent.speed > 0f) maxSpeed = navAgent.speed;
            }
            else if (charController != null)
            {
                worldVel = charController.velocity;
                worldVel.y = 0f;
            }

            // Convert to local space relative to character facing
            Vector3 localVel = transform.InverseTransformDirection(worldVel);

            // Normalize to -1..1 range
            float targetX = Mathf.Clamp(localVel.x / maxSpeed, -1f, 1f);
            float targetZ = Mathf.Clamp(localVel.z / maxSpeed, -1f, 1f);

            // Smooth
            smoothVelX = Mathf.Lerp(smoothVelX, targetX, Time.deltaTime * SmoothRate);
            smoothVelZ = Mathf.Lerp(smoothVelZ, targetZ, Time.deltaTime * SmoothRate);

            // Drive blend tree
            animator.SetFloat(VelXHash, smoothVelX);
            animator.SetFloat(VelZHash, smoothVelZ);
            animator.SetFloat(SpeedHash, worldVel.magnitude);

            // Grounded
            bool grounded = true;
            if (playerController != null)
                grounded = playerController.IsGrounded;
            animator.SetBool(IsGroundedHash, grounded);

            // Reload
            bool reloading = false;
            if (inventory != null && inventory.CurrentWeapon is GunBase gun)
                reloading = gun.IsReloading;
            animator.SetBool(IsReloadingHash, reloading);

            // 0=Knife, 1=Pistol, 2=Rifle, 3=Sniper. Grenades remap to Pistol
            // because the animator controller has no Grenade state.
            int weaponType = 2;
            if (inventory != null && inventory.CurrentWeapon != null && inventory.CurrentWeapon.weaponData != null)
            {
                var wt = inventory.CurrentWeapon.weaponData.weaponType;
                weaponType = wt == WeaponType.Grenade ? 1 : (int)wt;
            }
            animator.SetInteger(WeaponTypeHash, weaponType);
        }

        void OnWeaponFired(GameObject shooter, Vector3 position)
        {
            if (shooter == gameObject && animator != null)
            {
                // Skip fire animation for pistols
                if (inventory != null && inventory.CurrentWeapon != null &&
                    inventory.CurrentWeapon.weaponData != null &&
                    inventory.CurrentWeapon.weaponData.weaponType == WeaponType.Pistol)
                    return;

                animator.SetTrigger(FireHash);
            }
        }
    }
}
