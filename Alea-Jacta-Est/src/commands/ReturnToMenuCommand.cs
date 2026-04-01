using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

/// <summary>Returns the game to the main menu (WaitingForPlayers state).</summary>
public record ReturnToMenuCommand : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        state.Phase = GamePhase.WaitingForPlayers;
        state.IsSinglePlayer = false;
        state.Players.Clear();
        state.TurnStates.Clear();
        state.CurrentTurn = 1;
        state.CurrentTurnPhase = TurnPhase.DrawPhase;
        state.CurrentPlayerIndex = 0;
        state.PendingActivation = null;
        events.Publish(new ReturnedToMenu());
    }
}
