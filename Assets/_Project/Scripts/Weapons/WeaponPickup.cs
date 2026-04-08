using UnityEngine;

namespace CounterSiege
{
    public class WeaponPickup : MonoBehaviour
    {
        public WeaponData weaponData;
        public int currentAmmo;
        public int currentReserve;

        public static GameObject Create(WeaponData data, Vector3 position, int ammo, int reserve)
        {
            var go = new GameObject(data.weaponName + "_Pickup");
            go.transform.position = position;
            go.tag = "WeaponPickup";

            // Visual: instantiate the weapon's view model if available;
            // fallback to a tinted box if there isn't one.
            if (data.viewModelPrefab != null)
            {
                var model = Object.Instantiate(data.viewModelPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

                // Strip first-person-only behaviour so the world model just sits there.
                foreach (var vma in model.GetComponentsInChildren<ViewModelAlignment>(true))
                    Object.Destroy(vma);
                foreach (var cam in model.GetComponentsInChildren<Camera>(true))
                    cam.enabled = false;
                foreach (var light in model.GetComponentsInChildren<Light>(true))
                    light.enabled = false;

                // ViewModels are typically on the WeaponViewModel layer (7) which
                // only the FPS camera sees. Switch to Default so the world cam
                // and shadows render them.
                SetLayerRecursively(model, 0);
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(go.transform, false);
                cube.transform.localScale = new Vector3(0.3f, 0.15f, 0.8f);
                Object.Destroy(cube.GetComponent<Collider>()); // we add our own below
                var rend = cube.GetComponent<Renderer>();
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                rend.material.color = data.viewModelColor;
            }

            // Physics: a simple box so the pickup can be hit / picked up reliably
            // regardless of viewmodel mesh details.
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.4f, 0.2f, 1.0f);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var pickup = go.AddComponent<WeaponPickup>();
            pickup.weaponData = data;
            pickup.currentAmmo = ammo;
            pickup.currentReserve = reserve;

            return go;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }
    }
}
