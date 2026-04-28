// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Implement first-person mouse look with sensitivity control, pitch clamping,
//          recoil offset with automatic recovery, and cursor lock management."
// Modifications: Added aim sensitivity multiplier for scope zoom, separated camera holder
//                rotation from body rotation for proper FPS camera behavior.
using UnityEngine;
using UnityEngine.InputSystem;

namespace CounterSiege
{
    public class PlayerLook : MonoBehaviour
    {
        [Header("Sensitivity")]
        public float sensitivity = 2f;
        public float aimSensitivityMultiplier = 1f;

        [Header("Clamp")]
        public float minPitch = -89f;
        public float maxPitch = 89f;

        [Header("Recoil Recovery")]
        public float recoilRecoverySpeed = 5f;

        [Header("Hit Kick")]
        public float hitKickMagnitude = 0.06f;
        public float hitKickRecoverSpeed = 12f;
        public float hitKickMaxOffset = 0.15f;

        Transform cameraHolder;
        Transform mainCamera;
        Vector3 mainCameraBasePos;
        Vector3 hitKickOffset;
        float xRotation;
        float yRotation;
        Vector2 recoilOffset;
        Vector2 currentRecoil;
        bool cursorLocked = true;

        void Awake()
        {
            cameraHolder = transform.Find("CameraHolder");
            if (cameraHolder != null)
            {
                var cam = cameraHolder.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    mainCamera = cam.transform;
                    mainCameraBasePos = mainCamera.localPosition;
                }
            }
        }

        void Start()
        {
            SetCursorLock(true);
        }

        void Update()
        {
            RecoverRecoil();
            RecoverHitKick();
            ApplyRotation();
            ApplyHitKickPosition();
        }

        void RecoverHitKick()
        {
            if (hitKickOffset.sqrMagnitude > 0.00001f)
                hitKickOffset = Vector3.Lerp(hitKickOffset, Vector3.zero, hitKickRecoverSpeed * Time.deltaTime);
            else
                hitKickOffset = Vector3.zero;
        }

        void ApplyHitKickPosition()
        {
            if (mainCamera != null)
                mainCamera.localPosition = mainCameraBasePos + hitKickOffset;
        }

        public void AddHitKick(float magnitudeMultiplier = 1f)
        {
            // Camera-only back kick; cameraHolder (raycast origin) is untouched
            // so aim and crosshair stay accurate.
            hitKickOffset += Vector3.back * hitKickMagnitude * magnitudeMultiplier;
            hitKickOffset = Vector3.ClampMagnitude(hitKickOffset, hitKickMaxOffset);
        }

        void RecoverRecoil()
        {
            if (currentRecoil.sqrMagnitude > 0.001f)
            {
                currentRecoil = Vector2.Lerp(currentRecoil, Vector2.zero, recoilRecoverySpeed * Time.deltaTime);
            }
            else
            {
                currentRecoil = Vector2.zero;
            }
        }

        void ApplyRotation()
        {
            float finalPitch = Mathf.Clamp(xRotation - currentRecoil.y, minPitch, maxPitch);
            if (cameraHolder != null)
                cameraHolder.localRotation = Quaternion.Euler(finalPitch, 0, 0);
            transform.localRotation = Quaternion.Euler(0, yRotation + currentRecoil.x, 0);
        }

        public void OnLook(InputAction.CallbackContext ctx)
        {
            if (!cursorLocked) return;
            Vector2 delta = ctx.ReadValue<Vector2>();
            float sens = sensitivity * aimSensitivityMultiplier;
            yRotation += delta.x * sens * 0.1f;
            xRotation -= delta.y * sens * 0.1f;
            xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);
        }

        public void AddRecoil(Vector2 recoil)
        {
            currentRecoil += recoil;
        }

        public void SetCursorLock(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public Transform CameraTransform => cameraHolder;
    }
}
