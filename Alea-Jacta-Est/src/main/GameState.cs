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
}
