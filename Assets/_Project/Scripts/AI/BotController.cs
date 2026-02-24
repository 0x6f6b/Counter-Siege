using UnityEngine;
using UnityEngine.AI;

namespace CounterSiege
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class BotController : MonoBehaviour
    {
        [HideInInspector] public bool isFrozen;

        NavMeshAgent agent;
        PlayerHealth health;
        PlayerInventory inventory;
        PlayerEconomy economy;
        BotSensors sensors;
        BotAimController aimController;
        public BotStateMachine stateMachine;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<PlayerHealth>();
            inventory = GetComponent<PlayerInventory>();
            economy = GetComponent<PlayerEconomy>();
            sensors = GetComponent<BotSensors>();
            aimController = GetComponent<BotAimController>();
            stateMachine = new BotStateMachine(this);
        }

        void Start()
        {
            EventBus.OnRoundPhaseChanged += OnRoundPhaseChanged;
        }

        void OnDestroy()
        {
            EventBus.OnRoundPhaseChanged -= OnRoundPhaseChanged;
        }

        void Update()
        {
            if (health.isDead) return;

            sensors.UpdateSensors();
            stateMachine.Tick();
        }

        void OnRoundPhaseChanged(RoundPhase phase)
        {
            switch (phase)
            {
                case RoundPhase.FreezeTime:
                    stateMachine.ChangeState(new BotBuyState(this));
                    break;
                case RoundPhase.Live:
                    stateMachine.ChangeState(new BotNavigateState(this));
                    break;
                case RoundPhase.PostRound:
                    stateMachine.ChangeState(new BotIdleState(this));
                    break;
            }
        }

        // Public accessors for states
        public NavMeshAgent Agent => agent;
        public PlayerHealth Health => health;
        public PlayerInventory Inventory => inventory;
        public PlayerEconomy Economy => economy;
        public BotSensors Sensors => sensors;
        public BotAimController AimController => aimController;

        public void MoveTo(Vector3 position)
        {
            if (isFrozen || !agent.enabled) return;
            agent.isStopped = false;
            agent.SetDestination(position);
        }

        public void StopMoving()
        {
            if (agent.enabled)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        public void FireWeapon()
        {
            if (inventory.CurrentWeapon != null)
                inventory.CurrentWeapon.PrimaryFire();
        }

        public void ReloadWeapon()
        {
            if (inventory.CurrentWeapon != null)
                inventory.CurrentWeapon.Reload();
        }
    }
}
