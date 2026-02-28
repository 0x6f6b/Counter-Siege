using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class GameOverUI : MonoBehaviour
    {
        public GameObject panel;
        public Text resultText;
        public Text scoreText;
        public Button returnButton;

        void Start()
        {
            EventBus.OnMatchWon += OnMatchWon;
            if (panel != null) panel.SetActive(false);
            if (returnButton != null)
                returnButton.onClick.AddListener(OnReturn);
        }

        void OnDestroy()
        {
            EventBus.OnMatchWon -= OnMatchWon;
        }

        void OnMatchWon(Team winner)
        {
            if (panel != null) panel.SetActive(true);

            var player = GameManager.Instance?.playerObject;
            var ph = player?.GetComponent<PlayerHealth>();
            bool playerWon = ph != null && ph.team == winner;

            if (resultText != null)
                resultText.text = playerWon ? "VICTORY" : "DEFEAT";

            var rm = GameManager.Instance?.roundManager;
            if (scoreText != null && rm != null)
                scoreText.text = $"T {rm.tScore} : {rm.ctScore} CT";

            var look = player?.GetComponent<PlayerLook>();
            if (look != null) look.SetCursorLock(false);
        }

        void OnReturn()
        {
            GameManager.Instance?.ReturnToMenu();
        }
    }
}
