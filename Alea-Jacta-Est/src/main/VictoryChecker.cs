using System.Collections.Generic;
using System.Linq;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Main;

/// <summary>Checks victory conditions and builds final scores.</summary>
public static class VictoryChecker
{
    /// <summary>Checks if only one (or zero) player remains alive. Transitions to Finished if so.</summary>
    public static bool CheckVictory(GameState state)
    {
        if (state.Phase != GamePhase.InProgress) return false;

        var alive = state.Players.Where(p => p.Health > 0).ToList();
        if (alive.Count <= 1)
        {
            state.Phase = GamePhase.Finished;
            state.FinalScores = BuildFinalScores(state.Players, state.TurnStates);
            return true;
        }
        return false;
    }

    public static List<(Player Player, int TotalDamageDealt)> BuildFinalScores(
        List<Player> players, Dictionary<int, PlayerTurnState> turnStates)
    {
        return players
            .Select((p, i) => (Player: p, TotalDamageDealt: turnStates.TryGetValue(i, out var ts) ? ts.TotalDamageDealt : 0))
            .OrderByDescending(x => x.TotalDamageDealt)
            .ToList();
    }
}
