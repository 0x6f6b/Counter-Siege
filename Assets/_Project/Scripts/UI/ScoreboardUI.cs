using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class ScoreboardUI : MonoBehaviour
    {
        public GameObject scoreboardPanel;
        public Text scoreboardText;

        void Update()
        {
            var player = GameManager.Instance?.playerObject;
            if (player == null) return;

            var inputHandler = player.GetComponent<PlayerInputHandler>();
            bool show = inputHandler != null && inputHandler.IsScoreboardHeld;

            if (scoreboardPanel != null)
                scoreboardPanel.SetActive(show);

            if (show) UpdateScoreboard();
        }

        void UpdateScoreboard()
        {
            var sm = GameManager.Instance?.scoreManager;
            if (sm == null || scoreboardText == null) return;

            var text = new System.Text.StringBuilder();
            text.AppendLine("TERRORISTS");
            text.AppendLine("Name              K   D");
            text.AppendLine("-------------------------");

            foreach (var stat in sm.GetTeamStats(Team.Terrorist))
                text.AppendLine($"{stat.playerName,-18}{stat.kills,2}  {stat.deaths,2}");

            text.AppendLine();
            text.AppendLine("COUNTER-TERRORISTS");
            text.AppendLine("Name              K   D");
            text.AppendLine("-------------------------");

            foreach (var stat in sm.GetTeamStats(Team.CounterTerrorist))
                text.AppendLine($"{stat.playerName,-18}{stat.kills,2}  {stat.deaths,2}");

            scoreboardText.text = text.ToString();
        }
    }
}
