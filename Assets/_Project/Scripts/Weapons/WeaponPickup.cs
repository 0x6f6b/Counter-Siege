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
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = data.weaponName + "_Pickup";
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.3f, 0.15f, 0.8f);
            go.tag = "WeaponPickup";

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;

            var pickup = go.AddComponent<WeaponPickup>();
            pickup.weaponData = data;
            pickup.currentAmmo = ammo;
            pickup.currentReserve = reserve;

            var renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = data.viewModelColor;

            return go;
        }
    }
}
