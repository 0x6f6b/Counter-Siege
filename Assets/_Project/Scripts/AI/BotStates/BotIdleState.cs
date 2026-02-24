namespace CounterSiege
{
    public class BotIdleState : IBotState
    {
        BotController bot;
        public BotIdleState(BotController bot) { this.bot = bot; }

        public void Enter() { bot.StopMoving(); }
        public void Tick() { }
        public void Exit() { }
    }
}
