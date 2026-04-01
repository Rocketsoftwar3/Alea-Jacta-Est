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

        if (state.IsSinglePlayer && player != null && player.IsLocalPlayer
            && state.CurrentTurnPhase == TurnPhase.PlayPhase)
        {
            // Solo: skip directly to resolution without waiting for other players.
            state.AdvanceTurnPhase(); // PlayPhase → ValidatePhase
            state.AdvanceTurnPhase(); // ValidatePhase → ResolutionPhase
            events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
        }
        else if (!state.IsSinglePlayer && state.CurrentTurnPhase == TurnPhase.PlayPhase)
        {
            // Multiplayer turn-based: pass the hand to the next alive player.
            int next = FindNextActivePlayer(state, PlayerIndex);
            if (next >= 0)
                state.CurrentPlayerIndex = next;
            // When AllPlayersValidated becomes true, HostGameController advances to ResolutionPhase.
            events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
        }
    }

    /// <summary>Returns the index of the next alive, non-validated player after <paramref name="from"/>, or -1 if none.</summary>
    private static int FindNextActivePlayer(GameState state, int from)
    {
        for (int i = 1; i <= state.Players.Count; i++)
        {
            int idx = (from + i) % state.Players.Count;
            if (state.Players[idx].Health > 0 && !state.TurnStates[idx].HasValidated)
                return idx;
        }
        return -1;
    }
}
