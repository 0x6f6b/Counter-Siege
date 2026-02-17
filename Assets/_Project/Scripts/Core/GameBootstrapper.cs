using UnityEngine;
using UnityEngine.EventSystems;

namespace CounterSiege
{
    public class GameBootstrapper : MonoBehaviour
    {
        void Awake()
        {
            // Ensure EventSystem exists
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Create GameSettings if not assigned
            var settings = ScriptableObject.CreateInstance<GameSettings>();

            // Create WeaponDatabase
            var weaponDB = CreateWeaponDatabase();

            // Build map
            gameObject.AddComponent<MapBuilder>();

            // Audio manager
            var audioGO = new GameObject("AudioManager");
            audioGO.AddComponent<AudioManager>();

            // Load input actions
            var inputAsset = GameSceneSetup._inputActions;
            if (inputAsset == null)
                inputAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("InputSystem_Actions");
            if (inputAsset == null)
                inputAsset = FindInputAsset();

            // Game Manager setup
            var gmGO = new GameObject("GameManager");
            var gm = gmGO.AddComponent<GameManager>();
            gm.settings = settings;
            gm.weaponDatabase = weaponDB;
            gm.inputActions = inputAsset;
            gmGO.AddComponent<TeamManager>();
            gmGO.AddComponent<RoundManager>();
            gmGO.AddComponent<EconomyManager>();
            gmGO.AddComponent<ScoreManager>();

            // Build HUD
            HUDBuilder.Build();

            // Add lighting if needed
            if (FindAnyObjectByType<Light>() == null)
            {
                var lightGO = new GameObject("Directional Light");
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.shadows = LightShadows.Soft;
                lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
        }

        UnityEngine.InputSystem.InputActionAsset FindInputAsset()
        {
            // At edit time the asset is at Assets/InputSystem_Actions.inputactions
            // At runtime we need it in Resources. This is a fallback.
            #if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
            }
            #endif
            return null;
        }

        WeaponDatabase CreateWeaponDatabase()
        {
            var db = ScriptableObject.CreateInstance<WeaponDatabase>();

            db.allWeapons = new WeaponData[]
            {
                CreateWeaponData("Knife", WeaponType.Knife, WeaponSlot.Melee, 0, 40, 90, false, 0, 0, 0, 200, 1.0f, Color.gray, 0, TeamRestriction.Both),
                CreateWeaponData("Glock", WeaponType.Pistol, WeaponSlot.Pistol, 200, 28, 400, false, 20, 120, 2.2f, 200, 1.0f, new Color(0.3f, 0.3f, 0.3f), 0, TeamRestriction.TerroristOnly),
                CreateWeaponData("USP", WeaponType.Pistol, WeaponSlot.Pistol, 200, 35, 352, false, 12, 24, 2.2f, 200, 1.0f, new Color(0.5f, 0.5f, 0.5f), 0, TeamRestriction.CounterTerroristOnly),
                CreateWeaponData("AK-47", WeaponType.Rifle, WeaponSlot.Primary, 2700, 36, 600, true, 30, 90, 2.5f, 200, 0.88f, new Color(0.5f, 0.35f, 0.15f), 0, TeamRestriction.TerroristOnly),
                CreateWeaponData("M4A4", WeaponType.Rifle, WeaponSlot.Primary, 3100, 33, 666, true, 30, 90, 3.1f, 200, 0.88f, new Color(0.3f, 0.35f, 0.4f), 0, TeamRestriction.CounterTerroristOnly),
                CreateWeaponData("AWP", WeaponType.Sniper, WeaponSlot.Primary, 4750, 115, 41, false, 10, 30, 3.7f, 300, 0.82f, new Color(0.2f, 0.35f, 0.2f), 15f, TeamRestriction.Both),
            };

            return db;
        }

        WeaponData CreateWeaponData(string name, WeaponType type, WeaponSlot slot, int cost,
            float damage, float rpm, bool auto, int mag, int reserve, float reloadTime,
            float range, float speedMult, Color color, float scopeFov = 0, TeamRestriction teamRestriction = TeamRestriction.Both)
        {
            var data = ScriptableObject.CreateInstance<WeaponData>();
            data.teamRestriction = teamRestriction;
            data.weaponName = name;
            data.weaponType = type;
            data.slot = slot;
            data.cost = cost;
            data.damage = damage;
            data.fireRate = rpm;
            data.isAutomatic = auto;
            data.magazineSize = mag;
            data.reserveAmmo = reserve;
            data.reloadTime = reloadTime;
            data.range = range;
            data.moveSpeedMultiplier = speedMult;
            data.viewModelColor = color;
            data.scopeZoomFOV = scopeFov;
            data.killReward = type == WeaponType.Sniper ? 100 : 300;
            data.armorPenetration = type == WeaponType.Rifle ? 0.77f : 0.5f;
            // Legacy fallback accuracy — use StandardSetupConverter for proper per-weapon values
            data.spreadBase = type == WeaponType.Sniper ? 0f : 0.04f;
            data.inaccuracyStand = type == WeaponType.Sniper ? 8f : 0.25f;
            data.inaccuracyMove = type == WeaponType.Sniper ? 12f : 2f;
            data.inaccuracyCrouch = type == WeaponType.Sniper ? 6f : 0.15f;
            data.inaccuracyCrouchMove = type == WeaponType.Sniper ? 10f : 1.2f;
            data.inaccuracyJump = type == WeaponType.Sniper ? 15f : 7f;
            data.inaccuracyFire = type == WeaponType.Sniper ? 7f : 3f;
            data.inaccuracyRecoveryTime = type == WeaponType.Sniper ? 1.2f : 0.35f;
            data.drawTime = 0.5f;
            data.name = name;
            return data;
        }
    }
}
