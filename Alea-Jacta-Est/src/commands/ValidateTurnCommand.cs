using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public record ValidateTurnCommand(int PlayerIndex) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        if (!state.TurnStates.TryGetValue(PlayerIndex, out var turnState))
            return;

        turnState.HasValidated = true;

        var player = PlayerIndex < state.Players.Count ? state.Players[PlayerIndex] : null;
        if (player != null)
            events.Publish(new TurnValidated(player));

        // TODO: Réseau — attendre la validation de tous les vrais joueurs connectés
        // Pour l'instant : le joueur local valide → on passe directement en résolution
        if (player != null && player.IsLocalPlayer && state.CurrentTurnPhase == TurnPhase.PlayPhase)
        {
            state.AdvanceTurnPhase(); // PlayPhase → ValidatePhase
            state.AdvanceTurnPhase(); // ValidatePhase → ResolutionPhase
            events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
        }
    }
}
