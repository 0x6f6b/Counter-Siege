using UnityEngine;

namespace CounterSiege
{
    public class RoundManager : MonoBehaviour
    {
        public RoundPhase currentPhase { get; private set; }
        public float timer { get; private set; }
        public int currentRound { get; private set; }
        public int tScore { get; private set; }
        public int ctScore { get; private set; }

        GameManager gm;
        GameSettings settings;
        bool bombPlanted;
        bool roundEnded;

        void Awake()
        {
            gm = GetComponent<GameManager>();
        }

        void Start()
        {
            EventBus.OnBombPlanted += HandleBombPlanted;
            EventBus.OnBombDefused += HandleBombDefused;
            EventBus.OnBombExploded += HandleBombExploded;
            EventBus.OnPlayerDied += HandlePlayerDied;
        }

        void OnDestroy()
        {
            EventBus.OnBombPlanted -= HandleBombPlanted;
            EventBus.OnBombDefused -= HandleBombDefused;
            EventBus.OnBombExploded -= HandleBombExploded;
            EventBus.OnPlayerDied -= HandlePlayerDied;
        }

        public void StartMatch()
        {
            settings = gm.settings;
            currentRound = 0;
            tScore = 0;
            ctScore = 0;
            StartNewRound();
        }

        void StartNewRound()
        {
            currentRound++;
            roundEnded = false;
            bombPlanted = false;

            // Check half time
            if (currentRound == settings.halfTimeRound + 1)
            {
                gm.teamManager.SwapSides();
                (tScore, ctScore) = (ctScore, tScore);
            }

            // Respawn all players
            gm.RespawnAll();

            // Assign bomb carrier
            gm.teamManager.AssignBombCarrier();

            // Clean up old bombs
            foreach (var bomb in FindObjectsByType<BombController>(FindObjectsSortMode.None))
                Destroy(bomb.gameObject);

            // Clean up old weapon pickups
            foreach (var pickup in FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))
                Destroy(pickup.gameObject);

            SetPhase(RoundPhase.FreezeTime, settings.freezeTime);
        }

        void Update()
        {
            if (currentPhase == RoundPhase.Warmup) return;

            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                switch (currentPhase)
                {
                    case RoundPhase.FreezeTime:
                        SetPhase(RoundPhase.Live, settings.roundTime);
                        break;
                    case RoundPhase.Live:
                        if (!bombPlanted)
                            EndRound(Team.CounterTerrorist); // Time ran out
                        break;
                    case RoundPhase.PostRound:
                        StartNewRound();
                        break;
                }
            }

            // Check elimination during live phase
            if (currentPhase == RoundPhase.Live && !roundEnded)
                CheckElimination();
        }

        void SetPhase(RoundPhase phase, float duration)
        {
            currentPhase = phase;
            timer = duration;
            EventBus.OnRoundPhaseChanged?.Invoke(phase);

            if (phase == RoundPhase.FreezeTime)
                SetAllFrozen(true);
            else if (phase == RoundPhase.Live)
                SetAllFrozen(false);
        }

        void SetAllFrozen(bool frozen)
        {
            foreach (var p in gm.teamManager.GetAllPlayers())
            {
                var pc = p.GetComponent<PlayerController>();
                if (pc != null) pc.isFrozen = frozen;

                var agent = p.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.enabled)
                    agent.isStopped = frozen;

                var bot = p.GetComponent<BotController>();
                if (bot != null) bot.isFrozen = frozen;
            }
        }

        void CheckElimination()
        {
            int aliveT = gm.teamManager.GetAliveCount(Team.Terrorist);
            int aliveCT = gm.teamManager.GetAliveCount(Team.CounterTerrorist);

            if (aliveT == 0 && !bombPlanted)
                EndRound(Team.CounterTerrorist);
            else if (aliveCT == 0)
                EndRound(Team.Terrorist);
        }

        void EndRound(Team winner)
        {
            if (roundEnded) return;
            roundEnded = true;

            if (winner == Team.Terrorist) tScore++;
            else ctScore++;

            EventBus.OnRoundWon?.Invoke(winner);
            EventBus.OnScoreChanged?.Invoke();

            // Award money
            gm.economyManager.AwardRoundEnd(winner);

            // Check match end
            if (tScore >= settings.winsToMatch || ctScore >= settings.winsToMatch)
            {
                Team matchWinner = tScore >= settings.winsToMatch ? Team.Terrorist : Team.CounterTerrorist;
                gm.EndMatch(matchWinner);
                return;
            }

            if (currentRound >= settings.maxRounds)
            {
                gm.EndMatch(tScore > ctScore ? Team.Terrorist : Team.CounterTerrorist);
                return;
            }

            SetPhase(RoundPhase.PostRound, settings.postRoundTime);
        }

        void HandleBombPlanted(GameObject planter)
        {
            bombPlanted = true;
            timer = gm.settings.bombTimer;
        }

        void HandleBombDefused(GameObject defuser)
        {
            EndRound(Team.CounterTerrorist);
        }

        void HandleBombExploded()
        {
            EndRound(Team.Terrorist);
        }

        void HandlePlayerDied(GameObject victim, DamageInfo info)
        {
            // Kill reward
            if (info.attacker != null)
            {
                var eco = info.attacker.GetComponent<PlayerEconomy>();
                if (eco != null) eco.AddMoney(settings.killRewardDefault);
            }
        }

        public bool IsBuyTime => currentPhase == RoundPhase.FreezeTime ||
            (currentPhase == RoundPhase.Live && timer > settings.roundTime - settings.buyTime);
        public bool BombPlanted => bombPlanted;
    }
}
