// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Main menu flow that asks for a map first then a team, plus a
//          volume slider that remembers the value next time."
// Modifications: Added the AudioManager auto-create when only the slider is
//                present, set Industrial to skip the team-pick step.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CounterSiege
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject mapPanel;
        public GameObject teamPanel;
        public TMP_Text teamPanelSubtitle;

        [Header("Map panel")]
        public Button competitiveButton;
        public Button sandboxButton;
        public Button quitButton;

        [Header("Team panel")]
        public Button playTButton;
        public Button playCTButton;
        public Button backButton;

        [Header("Audio")]
        public Slider volumeSlider;
        public TMP_Text volumeLabel;

        string pendingScene;
        string pendingMapLabel;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            ShowMapPanel();

            if (competitiveButton != null)
                competitiveButton.onClick.AddListener(() => PickMap("GameScene", "DUST  ·  5v5 BOMB DEFUSAL", needsTeam: true));
            if (sandboxButton != null)
                sandboxButton.onClick.AddListener(() => PickMap("Sandbox", "INDUSTRIAL  ·  5v5 BOMB DEFUSAL", needsTeam: true));
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);

            if (playTButton != null)
                playTButton.onClick.AddListener(() => StartGame(Team.Terrorist));
            if (playCTButton != null)
                playCTButton.onClick.AddListener(() => StartGame(Team.CounterTerrorist));
            if (backButton != null)
                backButton.onClick.AddListener(ShowMapPanel);

            if (volumeSlider != null)
            {
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

        void PickMap(string sceneName, string label, bool needsTeam)
        {
            pendingScene = sceneName;
            pendingMapLabel = label;

            if (!needsTeam)
            {
                GameManager.PlayerTeam = Team.CounterTerrorist;
                SceneManager.LoadScene(sceneName);
                return;
            }
            ShowTeamPanel();
        }

        void ShowMapPanel()
        {
            if (mapPanel != null) mapPanel.SetActive(true);
            if (teamPanel != null) teamPanel.SetActive(false);
        }

        void ShowTeamPanel()
        {
            if (mapPanel != null) mapPanel.SetActive(false);
            if (teamPanel != null) teamPanel.SetActive(true);
            if (teamPanelSubtitle != null) teamPanelSubtitle.text = pendingMapLabel;
        }

        void StartGame(Team team)
        {
            if (string.IsNullOrEmpty(pendingScene)) return;
            GameManager.PlayerTeam = team;
            SceneManager.LoadScene(pendingScene);
        }

        void OnVolumeChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.MasterVolume = value;
            UpdateVolumeLabel(value);
        }

        void UpdateVolumeLabel(float value)
        {
            if (volumeLabel != null)
                volumeLabel.text = $"Volume  {Mathf.RoundToInt(value * 100)}%";
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
