using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("References")]
        public CrosshairUI crosshair;
        public ScopeOverlayUI scopeOverlay;
        public HealthArmorUI healthArmor;
        public AmmoUI ammo;
        public RoundTimerUI roundTimer;
        public KillfeedUI killfeed;
        public ScoreboardUI scoreboard;
        public BuyMenuUI buyMenu;
        public RoundEndUI roundEnd;
        public GameOverUI gameOver;

        [Header("Bomb")]
        public Text bombStatusText;

        [Header("Money")]
        public Text moneyText;

        [Header("Context")]
        public Text contextText;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            EventBus.OnRoundPhaseChanged += OnPhaseChanged;
            EventBus.OnMoneyChanged += OnMoneyChanged;
            EventBus.OnBombStateChanged += OnBombStateChanged;
            EventBus.OnScopeChanged += OnScopeChanged;
        }

        void OnDestroy()
        {
            EventBus.OnRoundPhaseChanged -= OnPhaseChanged;
            EventBus.OnMoneyChanged -= OnMoneyChanged;
            EventBus.OnBombStateChanged -= OnBombStateChanged;
            EventBus.OnScopeChanged -= OnScopeChanged;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var player = GameManager.Instance?.playerObject;
            if (player != null && contextText != null)
            {
                var interaction = player.GetComponent<PlayerInteraction>();
                if (interaction != null)
                    contextText.text = interaction.contextPrompt;
            }
        }

        void OnPhaseChanged(RoundPhase phase)
        {
            if (roundEnd != null) roundEnd.Hide();
        }

        void OnMoneyChanged(GameObject player, int amount)
        {
            if (player == GameManager.Instance?.playerObject && moneyText != null)
                moneyText.text = $"${amount}";
        }

        void OnBombStateChanged(string status)
        {
            if (bombStatusText != null)
                bombStatusText.text = status;
        }

        void OnScopeChanged(bool scoped, int level)
        {
            if (scopeOverlay != null)
            {
                if (scoped) scopeOverlay.Show();
                else scopeOverlay.Hide();
            }

            if (crosshair != null)
                crosshair.gameObject.SetActive(!scoped);
        }
    }
}
