using UnityEngine;

namespace CounterSiege
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Counter Siege/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Round Timing")]
        public float roundTime = 115f;
        public float freezeTime = 15f;
        public float bombTimer = 40f;
        public float defuseTime = 10f;
        public float buyTime = 20f;
        public float postRoundTime = 5f;
        public float warmupTime = 5f;

        [Header("Economy")]
        public int startMoney = 800;
        public int maxMoney = 16000;
        public int winReward = 3250;
        public int lossBase = 1400;
        public int lossIncrement = 500;
        public int maxLossBonus = 3400;
        public int killRewardDefault = 300;

        [Header("Bomb")]
        public float plantTime = 3.2f;
        public float bombDamage = 500f;
        public float bombRadius = 30f;

        [Header("Match")]
        public int maxRounds = 30;
        public int winsToMatch = 16;
        public int halfTimeRound = 15;
        public int playersPerTeam = 5;

        [Header("Bot")]
        public BotDifficulty defaultDifficulty = BotDifficulty.Normal;
    }
}
