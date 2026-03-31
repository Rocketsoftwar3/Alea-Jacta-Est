using System.Collections.Generic;
using System.Linq;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Main;

public enum GamePhase
{
    WaitingForPlayers,
    InProgress,
    Finished
}

public enum TurnPhase
{
    DrawPhase,
    PlayPhase,
    ValidatePhase,
    ResolutionPhase,
    CleanupPhase,
    ShopPhase
}

public class GameState
{
    public const int MaxPlayers = 4;

    public List<Player> Players { get; }
    public GamePhase Phase { get; set; }
    public TurnPhase CurrentTurnPhase { get; set; }
    public int CurrentPlayerIndex { get; set; }
    public int CurrentTurn { get; set; }
    public Player LocalPlayer { get; }

    /// <summary>Per-player round state keyed by player index in Players list.</summary>
    public Dictionary<int, PlayerTurnState> TurnStates { get; }

    /// <summary>Set when an arcana activation is waiting for a target to be selected.</summary>
    public PendingActivation? PendingActivation { get; set; }

    /// <summary>Populated when Phase transitions to Finished. Sorted by TotalDamageDealt descending.</summary>
    public List<(Player Player, int TotalDamageDealt)> FinalScores { get; private set; } = new();

    public Player? CurrentPlayer => Players.Count > 0 && CurrentPlayerIndex < Players.Count
        ? Players[CurrentPlayerIndex]
        : null;

    public bool AllPlayersValidated => TurnStates.Values.All(s => s.HasValidated);

    public GameState(string localPlayerName)
    {
        Players = new List<Player>(MaxPlayers);
        Phase = GamePhase.WaitingForPlayers;
        CurrentTurnPhase = TurnPhase.DrawPhase;
        CurrentPlayerIndex = 0;
        CurrentTurn = 1;

        LocalPlayer = new Player(localPlayerName, isLocalPlayer: true);
        Players.Add(LocalPlayer);

        TurnStates = new Dictionary<int, PlayerTurnState>();
    }

    /// <summary>Sets the game as started and initializes TurnStates for all players.</summary>
    public void StartGame()
    {
        Phase = GamePhase.InProgress;
        CurrentTurnPhase = TurnPhase.DrawPhase;
        CurrentTurn = 1;

        TurnStates.Clear();
        for (int i = 0; i < Players.Count; i++)
            TurnStates[i] = new PlayerTurnState();
    }

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
    public void AdvanceToNextTurn()
    {
        CurrentTurn++;
        foreach (var ts in TurnStates.Values)
            ts.Reset();
    }

    /// <summary>Checks if only one (or zero) player remains alive. Transitions to Finished if so.</summary>
    public bool CheckVictory()
    {
        if (Phase != GamePhase.InProgress) return false;

        var alive = Players.Where(p => p.Health > 0).ToList();
        if (alive.Count <= 1)
        {
            Phase = GamePhase.Finished;
            BuildFinalScores();
            return true;
        }
        return false;
    }

    private void BuildFinalScores()
    {
        FinalScores = Players
            .Select((p, i) => (Player: p, TotalDamageDealt: TurnStates.TryGetValue(i, out var ts) ? ts.TotalDamageDealt : 0))
            .OrderByDescending(x => x.TotalDamageDealt)
            .ToList();
    }
}
