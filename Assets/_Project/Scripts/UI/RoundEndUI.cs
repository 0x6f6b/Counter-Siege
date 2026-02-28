using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class RoundEndUI : MonoBehaviour
    {
        public GameObject panel;
        public Text resultText;

        void Start()
        {
            EventBus.OnRoundWon += OnRoundWon;
            Hide();
        }

        void OnDestroy()
        {
            EventBus.OnRoundWon -= OnRoundWon;
        }

        void OnRoundWon(Team winner)
        {
            if (panel != null) panel.SetActive(true);
            if (resultText != null)
            {
                resultText.text = winner == Team.Terrorist
                    ? "TERRORISTS WIN"
                    : "COUNTER-TERRORISTS WIN";
                resultText.color = winner == Team.Terrorist
                    ? new Color(1f, 0.8f, 0.2f)
                    : new Color(0.3f, 0.5f, 1f);
            }
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
