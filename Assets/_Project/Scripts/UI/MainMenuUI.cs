using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CounterSiege
{
    public class MainMenuUI : MonoBehaviour
    {
        public Button playTButton;
        public Button playCTButton;
        public Button quitButton;
        public Slider volumeSlider;
        public Text volumeLabel;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playTButton != null)
                playTButton.onClick.AddListener(() => StartGame(Team.Terrorist));
            if (playCTButton != null)
                playCTButton.onClick.AddListener(() => StartGame(Team.CounterTerrorist));
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);

            if (volumeSlider != null)
            {
                // Ensure AudioManager exists
                if (AudioManager.Instance == null)
                {
                    var go = new GameObject("_AudioManager");
                    go.AddComponent<AudioManager>();
                }

                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.value = AudioManager.Instance.MasterVolume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                UpdateVolumeLabel(volumeSlider.value);
            }
        }

        void OnVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.MasterVolume = value;
            UpdateVolumeLabel(value);
        }

        void UpdateVolumeLabel(float value)
        {
            if (volumeLabel != null)
                volumeLabel.text = $"Volume: {Mathf.RoundToInt(value * 100)}%";
        }

        void StartGame(Team team)
        {
            GameManager.PlayerTeam = team;
            SceneManager.LoadScene("GameScene");
        }

        void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
