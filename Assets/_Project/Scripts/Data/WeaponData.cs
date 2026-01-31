using UnityEngine;

namespace CounterSiege
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Counter Siege/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Info")]
        public string weaponName;
        public WeaponType weaponType;
        public WeaponSlot slot;
        public int cost;
        public int killReward = 300;
        public TeamRestriction teamRestriction = TeamRestriction.Both;

        [Header("Stats")]
        public float damage = 30;
        public float fireRate = 600f; // RPM
        public bool isAutomatic;
        public int magazineSize = 30;
        public int reserveAmmo = 90;
        public float reloadTime = 2.2f;
        public float range = 200f;
        public float armorPenetration = 0.5f;
        public float moveSpeedMultiplier = 1f;
        public float drawTime = 0.5f;

        [Header("Accuracy (degrees)")]
        public float spreadBase = 0.04f;              // Fixed mechanical imprecision
        public float inaccuracyStand = 0.25f;         // Standing still on ground
        public float inaccuracyMove = 2.0f;           // Moving on ground
        public float inaccuracyCrouch = 0.15f;        // Crouching still
        public float inaccuracyCrouchMove = 1.2f;     // Crouching + moving
        public float inaccuracyJump = 7.0f;           // Airborne
        public float inaccuracyFire = 3.0f;           // Per-shot spike
        public float inaccuracyRecoveryTime = 0.35f;  // Exponential decay time constant (seconds)

        [Header("Accuracy - Scope Override")]
        public float inaccuracyScopedStand = -1f;     // -1 = no override; >=0 = scoped standing value
        public float inaccuracyScopedMove = -1f;      // -1 = no override; >=0 = scoped moving value

        [Header("Scope")]
        public float scopeZoomFOV = 0f; // 0 = no scope
        public float scopeSecondZoomFOV = 0f; // 0 = no second zoom (e.g. AWP uses 8)

        [Header("Visuals")]
        public GameObject viewModelPrefab;
        public Color viewModelColor = Color.gray;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip equipSound;
        public AudioClip[] impactSounds;

        public float FireInterval => 60f / fireRate;
    }
}
