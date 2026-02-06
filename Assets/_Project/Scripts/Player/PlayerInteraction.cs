using UnityEngine;
using UnityEngine.InputSystem;

namespace CounterSiege
{
    public class PlayerInteraction : MonoBehaviour
    {
        public float interactRange = 3f;
        public LayerMask interactMask = ~0;

        PlayerHealth health;
        PlayerInventory inventory;
        PlayerLook look;
        bool holdingInteract;
        float interactHoldTime;

        // Bomb interaction
        BombController currentBomb;
        BombSite currentBombSite;

        [HideInInspector] public string contextPrompt = "";

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
            inventory = GetComponent<PlayerInventory>();
            look = GetComponent<PlayerLook>();
        }

        void Update()
        {
            CheckBombSite();
            CheckPickup();

            if (holdingInteract)
            {
                interactHoldTime += Time.deltaTime;
                HandleHoldInteract();
            }
            else
            {
                interactHoldTime = 0f;
                if (currentBomb != null && currentBomb.bombState == BombState.Planting)
                    currentBomb.CancelPlant();
                if (currentBomb != null && currentBomb.bombState == BombState.Defusing)
                    currentBomb.CancelDefuse();
            }
        }

        void CheckBombSite()
        {
            currentBombSite = null;
            Collider[] cols = Physics.OverlapSphere(transform.position, 1f);
            foreach (var col in cols)
            {
                var site = col.GetComponent<BombSite>();
                if (site != null)
                {
                    currentBombSite = site;
                    break;
                }
            }
        }

        void CheckPickup()
        {
            if (look == null || look.CameraTransform == null) return;

            if (Physics.Raycast(look.CameraTransform.position, look.CameraTransform.forward, out RaycastHit hit, interactRange, interactMask))
            {
                if (hit.collider.CompareTag("WeaponPickup"))
                {
                    contextPrompt = "Press E to pick up";
                    if (holdingInteract && interactHoldTime < 0.2f)
                    {
                        var pickup = hit.collider.GetComponent<WeaponPickup>();
                        if (pickup != null)
                        {
                            inventory.AddWeapon(pickup.weaponData, pickup.currentAmmo, pickup.currentReserve);
                            Destroy(pickup.gameObject);
                        }
                    }
                    return;
                }
            }

            UpdateContextPrompt();
        }

        void UpdateContextPrompt()
        {
            if (health.team == Team.Terrorist && inventory.HasBomb && currentBombSite != null)
            {
                contextPrompt = "Hold E to plant bomb";
            }
            else if (health.team == Team.CounterTerrorist && currentBomb != null && currentBomb.bombState == BombState.Planted)
            {
                contextPrompt = "Hold E to defuse bomb";
            }
            else
            {
                contextPrompt = "";
            }
        }

        void HandleHoldInteract()
        {
            // Plant bomb (T)
            if (health.team == Team.Terrorist && inventory.HasBomb && currentBombSite != null)
            {
                if (currentBomb == null)
                {
                    var bombObj = inventory.DropBomb();
                    if (bombObj != null)
                    {
                        currentBomb = bombObj.GetComponent<BombController>();
                        if (currentBomb != null)
                            currentBomb.StartPlant(gameObject, currentBombSite);
                    }
                }
            }
            // Defuse bomb (CT)
            else if (health.team == Team.CounterTerrorist)
            {
                if (currentBomb == null)
                {
                    var bombs = FindObjectsByType<BombController>(FindObjectsSortMode.None);
                    foreach (var b in bombs)
                    {
                        if (b.bombState == BombState.Planted &&
                            Vector3.Distance(transform.position, b.transform.position) < 3f)
                        {
                            currentBomb = b;
                            break;
                        }
                    }
                }

                if (currentBomb != null && currentBomb.bombState == BombState.Planted)
                {
                    currentBomb.StartDefuse(gameObject);
                }
            }
        }

        public void OnInteract(InputAction.CallbackContext ctx)
        {
            if (ctx.started) holdingInteract = true;
            if (ctx.canceled) holdingInteract = false;
        }
    }
}
