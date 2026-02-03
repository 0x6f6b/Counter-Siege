// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Implement a Source engine style movement controller in Unity with air strafing,
//          bunny hopping, crouch, and friction matching CS:GO values."
// Modifications: Adjusted movement constants to match documented Source engine values,
//                added duck-jump mechanic, integrated with PlayerInputHandler callback system,
//                added landing penalty and bhop speed clamping.

using UnityEngine;
using UnityEngine.InputSystem;

namespace CounterSiege
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Source Movement")]
        public float maxSpeed = 6.94f;           // 250 u/s
        public float walkSpeedCap = 3.61f;        // 130 u/s (shift = walk)
        public float crouchSpeedCap = 2.36f;      // 85 u/s
        public float groundAccelerate = 5.5f;     // sv_accelerate
        public float airAccelerate = 12f;         // sv_airaccelerate
        public float friction = 4f;               // sv_friction
        public float stopSpeed = 2.22f;           // sv_stopspeed (80 u/s)
        public float airSpeedCap = 0.83f;         // 30 u/s wishspeed cap in air

        [Header("Jump & Gravity")]
        public float jumpSpeed = 8.39f;           // 302 u/s
        public float sourceGravity = 22.22f;      // 800 u/s²

        [Header("Crouch")]
        public float standHeight = 2f;
        public float crouchHeight = 1.7f;
        public float crouchTransitionTime = 0.4f;

        [Header("Landing")]
        public float landingPenaltyThreshold = 9.72f;  // 350 u/s fall speed
        public float landingPenaltyMultiplier = 0.5f;

        [Header("Bhop")]
        public float bhopSpeedTolerance = 1.1f;   // 10% over max

        [Header("Ground Check")]
        public float groundCheckRadius = 0.3f;
        public float groundCheckOffset = 0.1f;
        public LayerMask groundMask = ~(1 << 6);

        [HideInInspector] public bool isFrozen;
        [HideInInspector] public float currentSpeedMultiplier = 1f;

        CharacterController cc;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        bool isGrounded;
        bool wasGrounded;
        bool isCrouching;
        bool wantsCrouch;
        bool isWalking; // shift key = walk (slower), not sprint
        bool wantsJump;
        Vector2 moveInput;
        float crouchFraction; // 0 = standing, 1 = crouched
        float previousVerticalVelocity;
        Transform cameraHolder;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            cc.stepOffset = 0.5f;       // 18 source units
            cc.slopeLimit = 45.57f;     // Source ramp max
            cameraHolder = transform.Find("CameraHolder");
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            wasGrounded = isGrounded;
            GroundCheck();
            HandleLanding();

            if (isFrozen)
            {
                horizontalVelocity = Vector3.zero;
                verticalVelocity -= sourceGravity * dt;
                cc.Move(new Vector3(0f, verticalVelocity * dt, 0f));
                return;
            }

            // Build wish direction and speed
            Vector3 wishDir = Vector3.zero;
            float wishSpeed = 0f;

            if (moveInput.sqrMagnitude > 0.001f)
            {
                wishDir = transform.right * moveInput.x + transform.forward * moveInput.y;
                wishDir.Normalize();
                wishSpeed = GetWishSpeed();
            }

            // Process jump before ground/air move decision
            if (wantsJump)
            {
                if (isGrounded)
                {
                    // Bhop speed clamp: prevent exponential speed gain
                    float maxAllowed = GetWishSpeed() * bhopSpeedTolerance;
                    float currentHSpeed = horizontalVelocity.magnitude;
                    if (currentHSpeed > maxAllowed)
                        horizontalVelocity = horizontalVelocity.normalized * maxAllowed;

                    verticalVelocity = jumpSpeed;
                    isGrounded = false; // treat as airborne this frame
                }
                wantsJump = false; // always consume — no queuing
            }

            if (isGrounded)
                GroundMove(wishDir, wishSpeed, dt);
            else
                AirMove(wishDir, wishSpeed, dt);

            ApplyCrouch(dt);

            // Combine and move
            Vector3 finalMove = horizontalVelocity * dt;
            finalMove.y = verticalVelocity * dt;
            cc.Move(finalMove);
        }

        void GroundCheck()
        {
            // If moving upward (jumping), skip ground check entirely
            if (verticalVelocity > 0.1f)
            {
                isGrounded = false;
                cc.stepOffset = 0f;
                return;
            }

            isGrounded = cc.isGrounded;

            if (!isGrounded)
            {
                Vector3 origin = transform.position + Vector3.up * (groundCheckRadius + 0.05f);
                isGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
                    out _, groundCheckRadius + 0.1f, groundMask, QueryTriggerInteraction.Ignore);
            }

            // Restore step offset when grounded, disable when airborne
            cc.stepOffset = isGrounded ? 0.5f : 0f;

            if (isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
        }

        void HandleLanding()
        {
            if (isGrounded && !wasGrounded)
            {
                // Landing penalty (CS:GO stamina)
                if (Mathf.Abs(previousVerticalVelocity) > landingPenaltyThreshold)
                {
                    horizontalVelocity *= landingPenaltyMultiplier;
                }
            }

            // Track vertical velocity for next frame's landing check
            previousVerticalVelocity = verticalVelocity;
        }

        void GroundMove(Vector3 wishDir, float wishSpeed, float dt)
        {
            // Apply friction BEFORE acceleration (Source order)
            ApplyFriction(dt);

            // Accelerate
            Accelerate(wishDir, wishSpeed, groundAccelerate, dt);

            // Gravity: keep small downward to stay grounded
            verticalVelocity = -2f;
        }

        void AirMove(Vector3 wishDir, float wishSpeed, float dt)
        {
            // Source PM_AirAccelerate: cap addspeed but use full wishspeed for accel rate
            AirAccelerate(wishDir, wishSpeed, airAccelerate, dt);

            // Gravity
            verticalVelocity -= sourceGravity * dt;
        }

        void Accelerate(Vector3 wishDir, float wishSpeed, float accel, float dt)
        {
            if (wishDir.sqrMagnitude < 0.001f) return;

            float currentSpeed = Vector3.Dot(horizontalVelocity, wishDir);
            float addSpeed = wishSpeed - currentSpeed;

            if (addSpeed <= 0f) return;

            float accelSpeed = Mathf.Min(accel * wishSpeed * dt, addSpeed);
            horizontalVelocity += accelSpeed * wishDir;
        }

        void AirAccelerate(Vector3 wishDir, float wishSpeed, float accel, float dt)
        {
            if (wishDir.sqrMagnitude < 0.001f) return;

            // Cap for addspeed calculation only
            float cappedWishSpeed = Mathf.Min(wishSpeed, airSpeedCap);

            float currentSpeed = Vector3.Dot(horizontalVelocity, wishDir);
            float addSpeed = cappedWishSpeed - currentSpeed;

            if (addSpeed <= 0f) return;

            // Use FULL (uncapped) wishspeed for acceleration rate — this is what makes air strafing work
            float accelSpeed = Mathf.Min(accel * wishSpeed * dt, addSpeed);
            horizontalVelocity += accelSpeed * wishDir;
        }

        void ApplyFriction(float dt)
        {
            float speed = horizontalVelocity.magnitude;
            if (speed < 0.01f)
            {
                horizontalVelocity = Vector3.zero;
                return;
            }

            float control = Mathf.Max(speed, stopSpeed);
            float drop = control * friction * dt;
            float newSpeed = Mathf.Max(speed - drop, 0f);

            horizontalVelocity *= newSpeed / speed;
        }

        float GetWishSpeed()
        {
            float baseSpeed = maxSpeed;

            if (isCrouching)
                baseSpeed = crouchSpeedCap;
            else if (isWalking)
                baseSpeed = walkSpeedCap;

            return baseSpeed * currentSpeedMultiplier;
        }

        void ApplyCrouch(float dt)
        {
            // Handle crouch intent
            if (wantsCrouch)
            {
                isCrouching = true;
            }
            else if (isCrouching)
            {
                // Check if we can uncrouch
                if (CanUncrouch())
                    isCrouching = false;
            }

            // Animate crouch transition
            float targetFraction = isCrouching ? 1f : 0f;
            float transitionSpeed = 1f / Mathf.Max(crouchTransitionTime, 0.01f);
            crouchFraction = Mathf.MoveTowards(crouchFraction, targetFraction, transitionSpeed * dt);

            float newHeight = Mathf.Lerp(standHeight, crouchHeight, crouchFraction);
            float heightDiff = newHeight - cc.height;

            cc.height = newHeight;
            cc.center = Vector3.up * (newHeight / 2f);

            // Duck-jump: crouching in air shifts center up to raise feet
            if (!isGrounded && isCrouching && heightDiff < 0f)
            {
                cc.Move(Vector3.up * (-heightDiff * 0.5f));
            }

            if (cameraHolder != null)
                cameraHolder.localPosition = new Vector3(0f, newHeight - 0.4f, 0f);
        }

        bool CanUncrouch()
        {
            float extraHeight = standHeight - cc.height;
            if (extraHeight <= 0.01f) return true;

            // SphereCast upward from head to check clearance
            Vector3 origin = transform.position + Vector3.up * cc.height;
            float radius = cc.radius * 0.9f;
            return !Physics.SphereCast(origin, radius, Vector3.up, out _, extraHeight, groundMask, QueryTriggerInteraction.Ignore);
        }

        // --- Input callbacks (same signatures) ---

        public void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();

        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && !isFrozen)
                wantsJump = true;
        }

        public void OnCrouch(InputAction.CallbackContext ctx)
        {
            wantsCrouch = ctx.performed || ctx.started;
            if (ctx.canceled) wantsCrouch = false;
        }

        public void OnSprint(InputAction.CallbackContext ctx)
        {
            // Sprint key = walk in CS:GO (shift slows you down)
            isWalking = ctx.performed || ctx.started;
            if (ctx.canceled) isWalking = false;
        }

        // --- Public properties (backward compatible) ---

        public bool IsGrounded => isGrounded;
        public bool IsCrouching => isCrouching;
        // IsSprinting = running at full speed (not walking) — maps to loud/fast footsteps
        public bool IsSprinting => !isWalking && !isCrouching;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
        public float CurrentSpeed => cc.velocity.magnitude;
    }
}
