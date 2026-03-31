using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

/// <summary>Signals that the local player is done with the shop phase.</summary>
public record EndShopPhaseCommand : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        // TurnService.AdvanceToNextTurn() is called by Game1.Update() when it sees ShopPhase + this flag.
        // We use the state directly to advance here since TurnService isn't available in Execute().
        state.AdvanceToNextTurn();
        state.AdvanceTurnPhase(); // ShopPhase → DrawPhase
        events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }
}
