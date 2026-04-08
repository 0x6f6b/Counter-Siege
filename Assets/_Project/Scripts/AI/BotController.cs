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

            // BotAimController owns facing; agent handles position only.
            if (agent != null) agent.updateRotation = false;
        }

        void Start()
        {
            EventBus.OnRoundPhaseChanged += OnRoundPhaseChanged;

            // Catch up: GameManager may have already fired the initial phase event
            // before this bot's Start ran, leaving us stuck in BotIdleState.
            var rm = GameManager.Instance?.roundManager;
            if (rm != null && rm.currentPhase != RoundPhase.Warmup)
                OnRoundPhaseChanged(rm.currentPhase);
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

            // Face direction of travel while moving. BotAimController takes
            // over during combat (velocity ~ 0 while engaging), so these don't fight.
            if (agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.05f)
            {
                Vector3 dir = agent.velocity;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    var target = Quaternion.LookRotation(dir.normalized);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, target, 540f * Time.deltaTime);
                }
            }
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
