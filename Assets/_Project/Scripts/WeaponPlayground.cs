using UnityEngine;

namespace CounterSiege
{
    /// <summary>
    /// Lightweight manager for the weapon testing scene.
    /// Spawns the player with every weapon, target dummies, and infinite reserve ammo.
    /// No bots, rounds, economy, or match logic.
    /// </summary>
    public class WeaponPlayground : MonoBehaviour
    {
        [Header("References")]
        public WeaponDatabase weaponDatabase;
        public UnityEngine.InputSystem.InputActionAsset inputActions;
        public GameObject playerPrefab;
        public GameObject hudPrefab;

        [Header("Settings")]
        public bool infiniteAmmo = true;
        public int targetCount = 6;
        public float targetSpacing = 3f;
        public float targetDistance = 15f;

        GameObject playerObject;
        PlayerInventory playerInventory;
        Target[] targets;

        // Minimal GameManager stand-in so weapons/inventory can find weapon data
        void Awake()
        {
            // Set up a GameManager instance if one doesn't exist
            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("_GameManager_Proxy");
                // AddComponent triggers Awake() which sets Instance
                var gm = gmObj.AddComponent<GameManager>();
                gm.weaponDatabase = weaponDatabase;
                gm.inputActions = inputActions;
                // Disable before Start() runs so it won't call InitializeMatch()
                gm.enabled = false;
            }
            else
            {
                GameManager.Instance.weaponDatabase = weaponDatabase;
                GameManager.Instance.inputActions = inputActions;
            }
        }

        void Start()
        {
            SpawnPlayer();
            SpawnTargets();
            GiveAllWeapons();

            // Instantiate HUD
            if (hudPrefab != null && HUDController.Instance == null)
                Instantiate(hudPrefab);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (infiniteAmmo && playerInventory != null)
                RefillAmmo();

            // Press T to reset all targets
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
                ResetTargets();
        }

        void SpawnPlayer()
        {
            Vector3 spawnPos = new Vector3(0, 1, 0);

            if (playerPrefab != null)
            {
                playerObject = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                playerObject.name = "Player";
                SetupWeaponCamera(playerObject);
            }
            else
            {
                playerObject = BuildPlayerFromCode(spawnPos);
            }

            var ph = playerObject.GetComponent<PlayerHealth>();
            if (ph != null) ph.Initialize(Team.CounterTerrorist, "Player");

            playerInventory = playerObject.GetComponent<PlayerInventory>();
        }

        void SetupWeaponCamera(GameObject player)
        {
            var mainCam = player.GetComponentInChildren<Camera>();
            if (mainCam == null) return;
            if (mainCam.transform.Find("WeaponCamera") != null) return;

            mainCam.cullingMask &= ~(1 << 7);

            var mainCamData = mainCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (mainCamData == null)
                mainCamData = mainCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            mainCamData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;

            var weaponCamObj = new GameObject("WeaponCamera");
            weaponCamObj.transform.SetParent(mainCam.transform);
            weaponCamObj.transform.localPosition = Vector3.zero;
            weaponCamObj.transform.localRotation = Quaternion.identity;
            var weaponCam = weaponCamObj.AddComponent<Camera>();
            weaponCam.cullingMask = 1 << 7;
            weaponCam.nearClipPlane = 0.01f;
            weaponCam.fieldOfView = 60f;

            var weaponCamData = weaponCamObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            weaponCamData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
            mainCamData.cameraStack.Add(weaponCam);
        }

        GameObject BuildPlayerFromCode(Vector3 spawnPos)
        {
            var go = new GameObject("Player");
            go.transform.position = spawnPos;
            int playerLayer = LayerMask.NameToLayer("Player");
            go.layer = playerLayer >= 0 ? playerLayer : 0;

            var cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = Vector3.up;

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(go.transform);
            body.transform.localPosition = new Vector3(0, 1, 0);
            body.layer = go.layer;
            Destroy(body.GetComponent<Collider>());
            var bodyR = body.GetComponent<Renderer>();
            bodyR.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bodyR.material.color = new Color(0.2f, 0.4f, 0.8f);

            // Head collider
            var headObj = new GameObject("HeadCollider");
            headObj.transform.SetParent(go.transform);
            headObj.transform.localPosition = new Vector3(0, 1.85f, 0);
            headObj.tag = "Head";
            headObj.layer = go.layer;
            var headCol = headObj.AddComponent<SphereCollider>();
            headCol.radius = 0.15f;

            // Camera
            var camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(go.transform);
            camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            var existingCam = Camera.main;
            if (existingCam != null)
            {
                existingCam.transform.SetParent(camHolder.transform);
                existingCam.transform.localPosition = Vector3.zero;
                existingCam.transform.localRotation = Quaternion.identity;
            }
            else
            {
                var camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                camObj.transform.SetParent(camHolder.transform);
                camObj.transform.localPosition = Vector3.zero;
                camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            // Weapon camera
            var cam = go.GetComponentInChildren<Camera>();
            cam.cullingMask &= ~(1 << 7);

            var camData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (camData == null)
                camData = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;

            var weaponCamObj = new GameObject("WeaponCamera");
            weaponCamObj.transform.SetParent(cam.transform);
            weaponCamObj.transform.localPosition = Vector3.zero;
            weaponCamObj.transform.localRotation = Quaternion.identity;
            var weaponCam = weaponCamObj.AddComponent<Camera>();
            weaponCam.cullingMask = 1 << 7;
            weaponCam.nearClipPlane = 0.01f;
            weaponCam.fieldOfView = 60f;

            var weaponCamData = weaponCamObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            weaponCamData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
            camData.cameraStack.Add(weaponCam);

            // Weapon holder
            var weaponHolder = new GameObject("WeaponHolder");
            weaponHolder.transform.SetParent(cam.transform);
            weaponHolder.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);

            // Components
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerLook>();
            var ph = go.AddComponent<PlayerHealth>();
            ph.Initialize(Team.CounterTerrorist, "Player");

            var inv = go.AddComponent<PlayerInventory>();
            inv.weaponHolder = weaponHolder.transform;

            go.AddComponent<PlayerEconomy>();
            go.AddComponent<PlayerInteraction>();
            go.AddComponent<PlayerInputHandler>();

            return go;
        }

        void GiveAllWeapons()
        {
            if (playerInventory == null || weaponDatabase == null) return;

            // Give knife first
            var knife = weaponDatabase.GetWeapon("Knife");
            if (knife != null) playerInventory.AddWeapon(knife);

            // Give a pistol
            var usp = weaponDatabase.GetWeapon("USP");
            if (usp != null) playerInventory.AddWeapon(usp);

            // Give a primary (AK47 by default, player can switch via targets)
            var ak = weaponDatabase.GetWeapon("AK47");
            if (ak != null) playerInventory.AddWeapon(ak);

            // Start with primary
            playerInventory.SwitchToSlot(2);
        }

        void RefillAmmo()
        {
            var weapon = playerInventory.CurrentWeapon;
            if (weapon is GunBase gun)
                gun.RefillReserve();
        }

        void SpawnTargets()
        {
            targets = new Target[targetCount];
            float totalWidth = (targetCount - 1) * targetSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < targetCount; i++)
            {
                float x = startX + i * targetSpacing;
                Vector3 pos = new Vector3(x, 0, targetDistance);
                targets[i] = CreateTarget(pos, $"Target_{i + 1}");
            }
        }

        Target CreateTarget(Vector3 position, string name)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            int playerLayer = LayerMask.NameToLayer("Player");
            go.layer = playerLayer >= 0 ? playerLayer : 0;

            // Body collider
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.4f;
            col.center = Vector3.up;

            // Body visual
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(go.transform);
            body.transform.localPosition = new Vector3(0, 1, 0);
            body.layer = go.layer;
            Destroy(body.GetComponent<Collider>());
            var bodyR = body.GetComponent<Renderer>();
            bodyR.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bodyR.material.color = new Color(0.8f, 0.2f, 0.2f);

            // Head collider
            var headObj = new GameObject("HeadCollider");
            headObj.transform.SetParent(go.transform);
            headObj.transform.localPosition = new Vector3(0, 1.85f, 0);
            headObj.tag = "Head";
            headObj.layer = go.layer;
            var headCol = headObj.AddComponent<SphereCollider>();
            headCol.radius = 0.15f;

            // Health
            var ph = go.AddComponent<PlayerHealth>();
            ph.Initialize(Team.Terrorist, name);

            // Target tracking component
            var target = go.AddComponent<Target>();
            return target;
        }

        void ResetTargets()
        {
            if (targets == null) return;
            foreach (var t in targets)
            {
                if (t == null) continue;
                var ph = t.GetComponent<PlayerHealth>();
                if (ph != null) ph.ResetHealth();

                // Re-enable visuals
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                    r.enabled = true;
                foreach (var c in t.GetComponentsInChildren<Collider>(true))
                    c.enabled = true;
            }
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            style.normal.textColor = new Color(1, 1, 1, 0.7f);

            float y = 10;
            GUI.Label(new Rect(10, y, 400, 25), "WEAPON PLAYGROUND", new GUIStyle(style) { fontSize = 18, fontStyle = FontStyle.Bold });
            y += 30;
            GUI.Label(new Rect(10, y, 400, 20), "1/2/3 - Switch weapon slots  |  Scroll - Cycle weapons", style);
            y += 20;
            GUI.Label(new Rect(10, y, 400, 20), "G - Drop weapon  |  E - Pick up weapon", style);
            y += 20;
            GUI.Label(new Rect(10, y, 400, 20), "T - Reset targets  |  B - Open buy menu (swap weapons)", style);
            y += 20;

            if (infiniteAmmo)
            {
                style.normal.textColor = new Color(0.5f, 1f, 0.5f, 0.7f);
                GUI.Label(new Rect(10, y, 400, 20), "Infinite ammo: ON", style);
            }
        }

        void OnDestroy()
        {
            // Clean up proxy GameManager if we created one
            if (GameManager.Instance != null && GameManager.Instance.gameObject.name == "_GameManager_Proxy")
                Destroy(GameManager.Instance.gameObject);
        }
    }

    /// <summary>
    /// Simple component to mark target dummies for tracking/resetting.
    /// </summary>
    public class Target : MonoBehaviour { }
}
