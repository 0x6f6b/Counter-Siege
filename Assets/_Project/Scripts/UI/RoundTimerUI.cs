using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class RoundTimerUI : MonoBehaviour
    {
        public Text timerText;
        public Text phaseText;
        public Text scoreText;

        Color normalColor = Color.white;
        Color urgentColor = Color.red;

        void Update()
        {
            var rm = GameManager.Instance?.roundManager;
            if (rm == null) return;

            float time = rm.timer;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);

            if (timerText != null)
            {
                timerText.text = $"{minutes}:{seconds:00}";
                timerText.color = time < 10f && rm.currentPhase == RoundPhase.Live ? urgentColor : normalColor;
            }

            if (phaseText != null)
            {
                phaseText.text = rm.currentPhase switch
                {
                    RoundPhase.FreezeTime => "FREEZE TIME",
                    RoundPhase.Live => rm.BombPlanted ? "BOMB PLANTED" : $"ROUND {rm.currentRound}",
                    RoundPhase.PostRound => "ROUND OVER",
                    _ => ""
                };
            }

            if (scoreText != null)
                scoreText.text = $"T {rm.tScore} : {rm.ctScore} CT";
        }
    }
}
