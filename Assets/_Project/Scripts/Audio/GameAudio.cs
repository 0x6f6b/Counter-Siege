using UnityEngine;

namespace CounterSiege
{
    public class GameAudio : MonoBehaviour
    {
        [Header("Player - Footsteps")]
        public AudioClip[] footstepSounds;
        public float footstepInterval = 0.4f;
        public float footstepVolume = 0.3f;

        [Header("Player - Jump/Land")]
        public AudioClip[] landSounds;
        public float landVolume = 0.35f;

        [Header("Player - Damage")]
        public AudioClip[] damageSounds;
        public AudioClip[] headshotSounds;
        public AudioClip[] deathSounds;
        public AudioClip helmetHitSound;
        public AudioClip armorHitSound;

        [Header("Bomb")]
        public AudioClip bombPlantSound;
        public AudioClip bombDefuseSound;
        public AudioClip bombExplodeSound;
        public AudioClip bombBeepSound;

        [Header("Round Announcements")]
        public AudioClip roundStartSound;
        public AudioClip ctWinSound;
        public AudioClip tWinSound;
        public AudioClip bombPlantedAnnounce;
        public AudioClip bombDefusedAnnounce;

        [Header("UI")]
        public AudioClip buttonClickSound;

        float footstepTimer;
        Transform localPlayer;
        PlayerController localPlayerController;
        bool wasGrounded = true;

        void OnEnable()
        {
            EventBus.OnPlayerDied += OnPlayerDied;
            EventBus.OnBombPlanted += OnBombPlanted;
            EventBus.OnBombDefused += OnBombDefused;
            EventBus.OnBombExploded += OnBombExploded;
            EventBus.OnRoundWon += OnRoundWon;
            EventBus.OnRoundPhaseChanged += OnRoundPhaseChanged;
            EventBus.OnHealthChanged += OnHealthChanged;
        }

        void OnDisable()
        {
            EventBus.OnPlayerDied -= OnPlayerDied;
            EventBus.OnBombPlanted -= OnBombPlanted;
            EventBus.OnBombDefused -= OnBombDefused;
            EventBus.OnBombExploded -= OnBombExploded;
            EventBus.OnRoundWon -= OnRoundWon;
            EventBus.OnRoundPhaseChanged -= OnRoundPhaseChanged;
            EventBus.OnHealthChanged -= OnHealthChanged;
        }

        void Update()
        {
            // Find local player if needed
            if (localPlayer == null)
            {
                var gm = GameManager.Instance;
                if (gm != null && gm.playerObject != null)
                {
                    localPlayer = gm.playerObject.transform;
                    localPlayerController = localPlayer.GetComponent<PlayerController>();
                }
                return;
            }

            HandleFootsteps();
            HandleLanding();
        }

        void HandleFootsteps()
        {
            if (localPlayerController == null) return;
            if (footstepSounds == null || footstepSounds.Length == 0) return;

            if (localPlayerController.IsMoving && localPlayerController.IsGrounded)
            {
                float interval = localPlayerController.IsSprinting
                    ? footstepInterval * 0.7f
                    : localPlayerController.IsCrouching
                        ? footstepInterval * 1.5f
                        : footstepInterval;

                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0)
                {
                    footstepTimer = interval;
                    var clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                    float vol = localPlayerController.IsCrouching
                        ? footstepVolume * 0.4f
                        : footstepVolume;
                    AudioManager.Instance?.PlaySFX(clip, localPlayer.position, vol);
                }
            }
            else
            {
                footstepTimer = 0;
            }
        }

        void HandleLanding()
        {
            if (localPlayerController == null) return;

            bool grounded = localPlayerController.IsGrounded;
            if (grounded && !wasGrounded)
            {
                // Just landed
                if (landSounds != null && landSounds.Length > 0 && AudioManager.Instance != null)
                {
                    var clip = landSounds[Random.Range(0, landSounds.Length)];
                    AudioManager.Instance.PlaySFX(clip, localPlayer.position, landVolume);
                }
            }
            wasGrounded = grounded;
        }

        // Track previous health to detect damage
        int prevHealth = 100;

        void OnHealthChanged(GameObject entity, int health, int armor)
        {
            if (entity != localPlayer) return;

            if (health < prevHealth && health > 0 && localPlayer != null)
            {
                // Took damage
                if (damageSounds != null && damageSounds.Length > 0 && AudioManager.Instance != null)
                {
                    var clip = damageSounds[Random.Range(0, damageSounds.Length)];
                    AudioManager.Instance.PlaySFX2D(clip, 0.35f);
                }
            }
            prevHealth = health;
        }

        void OnPlayerDied(GameObject victim, DamageInfo info)
        {
            if (deathSounds == null || deathSounds.Length == 0) return;
            if (AudioManager.Instance == null) return;

            var clip = deathSounds[Random.Range(0, deathSounds.Length)];
            AudioManager.Instance.PlaySFX(clip, victim.transform.position, 0.4f);

            // Headshot dink
            if (info.hitZone == HitZone.Head && headshotSounds != null && headshotSounds.Length > 0)
            {
                var hsClip = headshotSounds[Random.Range(0, headshotSounds.Length)];
                AudioManager.Instance.PlaySFX(hsClip, victim.transform.position, 0.5f);
            }
        }

        void OnBombPlanted(GameObject planter)
        {
            if (AudioManager.Instance == null) return;
            if (bombPlantSound != null)
                AudioManager.Instance.PlaySFX2D(bombPlantSound, 0.4f);
            if (bombPlantedAnnounce != null)
                AudioManager.Instance.PlaySFX2D(bombPlantedAnnounce, 1f);
        }

        void OnBombDefused(GameObject defuser)
        {
            if (AudioManager.Instance == null) return;
            if (bombDefuseSound != null)
                AudioManager.Instance.PlaySFX2D(bombDefuseSound, 0.4f);
            if (bombDefusedAnnounce != null)
                AudioManager.Instance.PlaySFX2D(bombDefusedAnnounce, 1f);
        }

        void OnBombExploded()
        {
            if (bombExplodeSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX2D(bombExplodeSound, 0.6f);
        }

        void OnRoundWon(Team team)
        {
            if (AudioManager.Instance == null) return;
            var clip = team == Team.CounterTerrorist ? ctWinSound : tWinSound;
            if (clip != null)
                AudioManager.Instance.PlaySFX2D(clip, 1f);
        }

        void OnRoundPhaseChanged(RoundPhase phase)
        {
            if (phase == RoundPhase.Live && roundStartSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX2D(roundStartSound, 1f);
        }
    }
}
