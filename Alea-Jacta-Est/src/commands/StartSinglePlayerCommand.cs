using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Services;

namespace Alea_Jacta_Est.Commands;

/// <summary>Initializes a solo game with demo decks and starts the game loop.</summary>
public record StartSinglePlayerCommand(DemoDataService DemoData) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        state.IsSinglePlayer = true;
        DemoData.InitializeDemoDecks(state);
        state.StartGame();
        events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }
}
