using UnityEngine;
using UnityEngine.InputSystem;

namespace CounterSiege
{
    public class PlayerInventory : MonoBehaviour
    {
        public Transform weaponHolder;

        WeaponBase[] weapons = new WeaponBase[5]; // Melee, Pistol, Primary, Bomb, Grenade
        int currentSlot = -1;
        bool hasBomb;

        PlayerController playerController;
        PlayerLook playerLook;

        void Awake()
        {
            playerController = GetComponent<PlayerController>();
            playerLook = GetComponent<PlayerLook>();
        }

        void Start()
        {
            if (weaponHolder == null)
            {
                // Try to find it
                var cam = GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    weaponHolder = cam.transform.Find("WeaponHolder");
                    if (weaponHolder == null)
                    {
                        var holder = new GameObject("WeaponHolder");
                        holder.transform.SetParent(cam.transform);
                        holder.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                        weaponHolder = holder.transform;
                    }
                }
            }
        }

        void Update()
        {
            if (currentSlot >= 0 && weapons[currentSlot] != null)
            {
                weapons[currentSlot].Tick();

                // Update speed multiplier
                if (playerController != null)
                    playerController.currentSpeedMultiplier = weapons[currentSlot].weaponData.moveSpeedMultiplier;
            }
        }

        public void GiveDefaultLoadout(Team team)
        {
            ClearWeapons();

            string pistolName = team == Team.Terrorist ? "Glock" : "USP";
            var pistolData = FindWeaponData(pistolName);
            if (pistolData != null) AddWeapon(pistolData);

            SwitchToSlot(1); // Start with pistol
        }

        public void AddWeapon(WeaponData data, int ammo = -1, int reserve = -1)
        {
            int slot = (int)data.slot;

            // Drop existing weapon in that slot
            if (weapons[slot] != null)
                DropWeapon(slot);

            // Create viewmodel
            var weaponGO = CreateViewModel(data);
            WeaponBase weapon;

            switch (data.weaponType)
            {
                case WeaponType.Knife:
                    weapon = weaponGO.AddComponent<Knife>();
                    break;
                case WeaponType.Sniper:
                    weapon = weaponGO.AddComponent<SniperRifle>();
                    break;
                case WeaponType.Grenade:
                    weapon = weaponGO.AddComponent<Grenade>();
                    break;
                default:
                    weapon = weaponGO.AddComponent<GunBase>();
                    break;
            }

            weapon.weaponData = data;
            weapons[slot] = weapon;

            if (weapon is GunBase gun)
                gun.Initialize(ammo, reserve);

            weaponGO.SetActive(false);
            SwitchToSlot(slot);

            EventBus.OnWeaponPickedUp?.Invoke(gameObject, data);
        }

        Transform GetRightHandBone()
        {
            var model = transform.Find("CharacterModel");
            if (model == null) return null;
            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return null;
            return animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        GameObject CreateViewModel(WeaponData data)
        {
            // Use 3D model prefab if available
            if (data.viewModelPrefab != null)
            {
                // Parent to hand bone if character model exists, otherwise fall back to weaponHolder
                Transform parent = GetRightHandBone() ?? weaponHolder;
                var instance = Instantiate(data.viewModelPrefab, parent);
                instance.name = data.weaponName + "_ViewModel";
                SetViewModelLayerRecursive(instance);
                RemoveCollidersRecursive(instance);
                return instance;
            }

            // Fallback: procedural geometry
            var go = new GameObject(data.weaponName + "_ViewModel");

            if (weaponHolder != null)
            {
                go.transform.SetParent(weaponHolder);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
            }

            if (data.weaponType != WeaponType.Knife)
            {
                // Gun body
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.SetParent(go.transform);
                body.transform.localPosition = Vector3.zero;
                body.transform.localScale = new Vector3(0.06f, 0.08f, 0.3f);
                Destroy(body.GetComponent<Collider>());
                SetViewModelLayerRecursive(body);
                var bodyR = body.GetComponent<Renderer>();
                bodyR.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                bodyR.material.color = data.viewModelColor;

                // Barrel
                var barrel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                barrel.transform.SetParent(go.transform);
                barrel.transform.localPosition = new Vector3(0, 0.02f, 0.2f);
                barrel.transform.localScale = new Vector3(0.03f, 0.15f, 0.03f);
                barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Destroy(barrel.GetComponent<Collider>());
                SetViewModelLayerRecursive(barrel);
                var barrelR = barrel.GetComponent<Renderer>();
                barrelR.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                barrelR.material.color = Color.black;
            }
            else
            {
                // Knife blade
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.transform.SetParent(go.transform);
                blade.transform.localPosition = new Vector3(0, 0, 0.15f);
                blade.transform.localScale = new Vector3(0.02f, 0.04f, 0.2f);
                Destroy(blade.GetComponent<Collider>());
                SetViewModelLayerRecursive(blade);
                var bladeR = blade.GetComponent<Renderer>();
                bladeR.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                bladeR.material.color = Color.gray;
            }

            return go;
        }

        void SetViewModelLayerRecursive(GameObject go)
        {
            bool isBot = GetComponent<BotController>() != null;
            // Bots: force to Default layer so the weapon overlay camera doesn't render them through walls.
            // Player: set to WeaponViewModel layer for the overlay camera.
            int layer = isBot ? 0 : LayerMask.NameToLayer("WeaponViewModel");
            if (layer < 0) return;
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetViewModelLayerRecursive(child.gameObject);
        }

        void RemoveCollidersRecursive(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
                Destroy(col);
        }

        public void SwitchToSlot(int slot)
        {
            if (slot < 0 || slot >= weapons.Length) return;
            if (weapons[slot] == null) return;

            if (currentSlot >= 0 && weapons[currentSlot] != null)
                weapons[currentSlot].OnUnequip();

            currentSlot = slot;
            weapons[currentSlot].OnEquip(gameObject);
        }

        public void DropWeapon(int slot)
        {
            if (weapons[slot] == null) return;
            if (slot == (int)WeaponSlot.Melee) return; // Can't drop knife

            var weapon = weapons[slot];
            var data = weapon.weaponData;

            int ammo = weapon.CurrentAmmo;
            int reserve = weapon.CurrentReserve;

            Vector3 dropPos = transform.position + transform.forward * 1.5f + Vector3.up;
            WeaponPickup.Create(data, dropPos, ammo, reserve);

            Destroy(weapon.gameObject);
            weapons[slot] = null;

            EventBus.OnWeaponDropped?.Invoke(gameObject, data);

            if (currentSlot == slot)
            {
                // Switch to another weapon
                for (int i = 0; i < weapons.Length; i++)
                {
                    if (weapons[i] != null) { SwitchToSlot(i); return; }
                }
                currentSlot = -1;
            }
        }

        // Single-use weapons (grenades) call this to remove themselves after firing.
        public void ConsumeCurrentWeapon()
        {
            if (currentSlot < 0 || weapons[currentSlot] == null) return;
            int slot = currentSlot;
            var weapon = weapons[slot];
            weapons[slot] = null;
            Destroy(weapon.gameObject);
            currentSlot = -1;
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null) { SwitchToSlot(i); return; }
            }
        }

        public void DropAllWeapons()
        {
            for (int i = weapons.Length - 1; i >= 0; i--)
            {
                if (weapons[i] != null && i != (int)WeaponSlot.Melee)
                    DropWeapon(i);
            }
        }

        public GameObject DropBomb()
        {
            if (!hasBomb) return null;
            hasBomb = false;
            // Clear HUD indicator if local player drops the bomb
            if (GameManager.Instance != null && gameObject == GameManager.Instance.playerObject)
                EventBus.OnBombStateChanged?.Invoke("");

            Vector3 pos = transform.position + Vector3.down * 0.5f;
            var bombGO = new GameObject("Bomb");
            bombGO.transform.position = pos;
            var bomb = bombGO.AddComponent<BombController>();

            // Visual
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(bombGO.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.3f, 0.15f, 0.4f);
            Destroy(visual.GetComponent<Collider>());
            var r = visual.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            r.material.color = new Color(0.8f, 0.2f, 0f);

            var col = bombGO.AddComponent<BoxCollider>();
            col.size = new Vector3(0.3f, 0.15f, 0.4f);

            return bombGO;
        }

        public void GiveBomb()
        {
            hasBomb = true;
            // Show HUD indicator only for the local human player
            if (GameManager.Instance != null && gameObject == GameManager.Instance.playerObject)
                EventBus.OnBombStateChanged?.Invoke("⚠ YOU HAVE THE BOMB");
        }

        public void ClearWeapons()
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null)
                {
                    Destroy(weapons[i].gameObject);
                    weapons[i] = null;
                }
            }
            currentSlot = -1;
            hasBomb = false;
        }

        WeaponData FindWeaponData(string name)
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.weaponDatabase != null)
                return gm.weaponDatabase.GetWeapon(name);
            return null;
        }

        // Input callbacks
        public void OnAttack(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            if (IsUiBlockingInput()) return;
            if (currentSlot >= 0 && weapons[currentSlot] != null)
            {
                if (weapons[currentSlot].weaponData.isAutomatic) return; // handled in AttackHeld
                weapons[currentSlot].PrimaryFire();
            }
        }

        public void AttackHeld()
        {
            if (IsUiBlockingInput()) return;
            if (currentSlot >= 0 && weapons[currentSlot] != null && weapons[currentSlot].weaponData.isAutomatic)
                weapons[currentSlot].PrimaryFire();
        }

        public void OnSecondaryFire(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            if (IsUiBlockingInput()) return;
            if (currentSlot >= 0 && weapons[currentSlot] != null)
                weapons[currentSlot].SecondaryFire();
        }

        // Suppress fire input while a blocking UI panel (e.g. buy menu) is open.
        static bool IsUiBlockingInput()
        {
            var hud = HUDController.Instance;
            return hud != null && hud.buyMenu != null && hud.buyMenu.IsOpen;
        }

        public void OnReload(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && currentSlot >= 0 && weapons[currentSlot] != null)
                weapons[currentSlot].Reload();
        }

        public void OnDrop(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) DropWeapon(currentSlot);
        }

        public void OnSlot1(InputAction.CallbackContext ctx) { if (ctx.performed) SwitchToSlot(0); }
        public void OnSlot2(InputAction.CallbackContext ctx) { if (ctx.performed) SwitchToSlot(1); }
        public void OnSlot3(InputAction.CallbackContext ctx) { if (ctx.performed) SwitchToSlot(2); }
        public void OnSlot4(InputAction.CallbackContext ctx) { if (ctx.performed) SwitchToSlot(3); }
        public void OnSlot5(InputAction.CallbackContext ctx) { if (ctx.performed) SwitchToSlot(4); }

        public void OnScrollWeapon(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            float scroll = ctx.ReadValue<float>();
            if (Mathf.Abs(scroll) < 0.1f) return;

            int dir = scroll > 0 ? 1 : -1;
            int next = currentSlot;
            for (int i = 0; i < weapons.Length; i++)
            {
                next = (next + dir + weapons.Length) % weapons.Length;
                if (weapons[next] != null)
                {
                    SwitchToSlot(next);
                    break;
                }
            }
        }

        public WeaponBase CurrentWeapon => currentSlot >= 0 ? weapons[currentSlot] : null;
        public bool HasBomb => hasBomb;
        public bool HasWeaponInSlot(WeaponSlot slot) => weapons[(int)slot] != null;
    }
}
