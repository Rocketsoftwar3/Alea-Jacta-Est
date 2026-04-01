using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

/// <summary>Signals that a player is done with the shop phase.</summary>
public record EndShopPhaseCommand(int PlayerIndex = 0) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        if (state.IsSinglePlayer)
        {
            state.AdvanceToNextTurn();
            state.AdvanceTurnPhase(); // ShopPhase → DrawPhase
            events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
        }
        else
        {
            if (state.TurnStates.TryGetValue(PlayerIndex, out var ts))
                ts.HasEndedShop = true;
            // HostGameController.CheckAllShopDone() will advance the phase when all players are done
        }
    }
}
