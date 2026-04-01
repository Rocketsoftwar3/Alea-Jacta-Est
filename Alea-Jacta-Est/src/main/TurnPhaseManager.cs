using System.Collections.Generic;

namespace Alea_Jacta_Est.Main;

/// <summary>Manages the turn phase state machine and turn counter.</summary>
public class TurnPhaseManager
{
    public TurnPhase CurrentTurnPhase { get; set; } = TurnPhase.DrawPhase;
    public int CurrentTurn { get; set; } = 1;

    /// <summary>Advances to the next phase in the cycle.</summary>
    public void AdvanceTurnPhase()
    {
        CurrentTurnPhase = CurrentTurnPhase switch
        {
            TurnPhase.DrawPhase       => TurnPhase.PlayPhase,
            TurnPhase.PlayPhase       => TurnPhase.ValidatePhase,
            TurnPhase.ValidatePhase   => TurnPhase.ResolutionPhase,
            TurnPhase.ResolutionPhase => TurnPhase.CleanupPhase,
            TurnPhase.CleanupPhase    => TurnPhase.ShopPhase,
            TurnPhase.ShopPhase       => TurnPhase.DrawPhase,
            _                         => TurnPhase.DrawPhase
        };
    }

    /// <summary>Resets all TurnStates and increments the turn counter.</summary>
    public void AdvanceToNextTurn(Dictionary<int, PlayerTurnState> turnStates)
    {
        CurrentTurn++;
        foreach (var ts in turnStates.Values)
            ts.Reset();
    }
}
