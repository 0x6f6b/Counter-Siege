namespace CounterSiege
{
    public interface IBotState
    {
        void Enter();
        void Tick();
        void Exit();
    }

    public class BotStateMachine
    {
        IBotState currentState;
        BotController bot;

        public BotStateMachine(BotController bot)
        {
            this.bot = bot;
            currentState = new BotIdleState(bot);
        }

        public void ChangeState(IBotState newState)
        {
            currentState?.Exit();
            currentState = newState;
            currentState?.Enter();
        }

        public void Tick()
        {
            currentState?.Tick();
        }

        public IBotState CurrentState => currentState;
    }
}
