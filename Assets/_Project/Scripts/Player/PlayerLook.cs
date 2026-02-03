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

        Transform cameraHolder;
        float xRotation;
        float yRotation;
        Vector2 recoilOffset;
        Vector2 currentRecoil;
        bool cursorLocked = true;

        void Awake()
        {
            cameraHolder = transform.Find("CameraHolder");
        }

        void Start()
        {
            SetCursorLock(true);
        }

        void Update()
        {
            RecoverRecoil();
            ApplyRotation();
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
