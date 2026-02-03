using UnityEngine;
using UnityEngine.InputSystem;

namespace CounterSiege
{
    public class PlayerInputHandler : MonoBehaviour
    {
        PlayerController controller;
        PlayerLook look;
        PlayerInventory inventory;
        PlayerInteraction interaction;

        InputActionAsset actions;
        InputActionMap playerMap;
        InputAction attackAction;
        InputAction scoreboardAction;
        InputAction buyMenuAction;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            look = GetComponent<PlayerLook>();
            inventory = GetComponent<PlayerInventory>();
            interaction = GetComponent<PlayerInteraction>();
        }

        void Start()
        {
            actions = GameManager.Instance?.inputActions;
            if (actions == null) return;

            actions = Instantiate(actions);
            playerMap = actions.FindActionMap("Player");
            if (playerMap == null) return;

            playerMap.Enable();

            playerMap.FindAction("Move").performed += controller.OnMove;
            playerMap.FindAction("Move").canceled += controller.OnMove;
            playerMap.FindAction("Look").performed += look.OnLook;
            playerMap.FindAction("Jump").performed += controller.OnJump;
            playerMap.FindAction("Crouch").performed += controller.OnCrouch;
            playerMap.FindAction("Crouch").canceled += controller.OnCrouch;
            playerMap.FindAction("Sprint").performed += controller.OnSprint;
            playerMap.FindAction("Sprint").canceled += controller.OnSprint;
            playerMap.FindAction("Attack").performed += inventory.OnAttack;
            playerMap.FindAction("Previous").performed += inventory.OnSlot3;  // Key 1 → Primary
            playerMap.FindAction("Next").performed += inventory.OnSlot2;      // Key 2 → Pistol

            BindAction("Reload", inventory.OnReload);
            BindAction("SecondaryFire", inventory.OnSecondaryFire);
            BindAction("Drop", inventory.OnDrop);
            BindAction("Slot3", inventory.OnSlot1);  // Key 3 → Knife
            BindAction("Slot4", inventory.OnSlot4);
            BindAction("ScrollWeapon", inventory.OnScrollWeapon);

            if (interaction != null)
            {
                playerMap.FindAction("Interact").started += interaction.OnInteract;
                playerMap.FindAction("Interact").canceled += interaction.OnInteract;
            }

            buyMenuAction = playerMap.FindAction("BuyMenu");
            scoreboardAction = playerMap.FindAction("Scoreboard");
            attackAction = playerMap.FindAction("Attack");
        }

        void BindAction(string name, System.Action<InputAction.CallbackContext> callback)
        {
            var action = playerMap?.FindAction(name);
            if (action != null)
                action.performed += callback;
        }

        void Update()
        {
            // Auto-fire for automatic weapons
            if (attackAction != null && attackAction.IsPressed())
                inventory.AttackHeld();

            // Buy menu toggle on B press (check every frame for key down)
            if (buyMenuAction != null && buyMenuAction.WasPressedThisFrame())
            {
                var hud = HUDController.Instance;
                if (hud != null && hud.buyMenu != null)
                    hud.buyMenu.Toggle();
            }
        }

        void OnDestroy()
        {
            if (playerMap != null)
                playerMap.Disable();
            if (actions != null)
                Destroy(actions);
        }

        public bool IsScoreboardHeld => scoreboardAction != null && scoreboardAction.IsPressed();
    }
}
