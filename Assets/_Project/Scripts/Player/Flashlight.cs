using UnityEngine;
using UnityEngine.InputSystem;

namespace CounterSiege
{
    public class Flashlight : MonoBehaviour
    {
        [Header("Light")]
        public float intensity = 4f;
        public float range = 30f;
        public float spotAngle = 55f;
        public float innerSpotAngle = 30f;
        public Color color = new Color(1f, 0.97f, 0.85f, 1f);
        public bool startOn = false;

        [Header("Mount")]
        [Tooltip("Local offset relative to the camera transform (slight downward + forward feels right in first person).")]
        public Vector3 localOffset = new Vector3(0f, -0.05f, 0.1f);

        Light spotLight;

        void Start()
        {
            var look = GetComponent<PlayerLook>();
            Transform mount = look != null && look.CameraTransform != null ? look.CameraTransform : transform;

            var go = new GameObject("Flashlight");
            go.transform.SetParent(mount, false);
            go.transform.localPosition = localOffset;
            go.transform.localRotation = Quaternion.identity;

            spotLight = go.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = color;
            spotLight.intensity = intensity;
            spotLight.range = range;
            spotLight.spotAngle = spotAngle;
            spotLight.innerSpotAngle = innerSpotAngle;
            // Shadows from a player flashlight are expensive in URP and add
            // little perceived quality. Keep off by default.
            spotLight.shadows = LightShadows.None;
            spotLight.enabled = startOn;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.tKey.wasPressedThisFrame)
                Toggle();
        }

        public void Toggle()
        {
            if (spotLight != null) spotLight.enabled = !spotLight.enabled;
        }
    }
}
