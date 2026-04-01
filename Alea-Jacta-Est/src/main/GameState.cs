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
    public int CurrentPlayerIndex { get; set; }

    /// <summary>Phase state machine and turn counter (extracted for SRP).</summary>
    public TurnPhaseManager PhaseManager { get; }

    /// <summary>Delegates to PhaseManager.CurrentTurnPhase.</summary>
    public TurnPhase CurrentTurnPhase
    {
        get => PhaseManager.CurrentTurnPhase;
        set => PhaseManager.CurrentTurnPhase = value;
    }

    /// <summary>Delegates to PhaseManager.CurrentTurn.</summary>
    public int CurrentTurn
    {
        get => PhaseManager.CurrentTurn;
        set => PhaseManager.CurrentTurn = value;
    }

    /// <summary>The player on this machine. Computed from IsLocalPlayer flag so it works in multiplayer.</summary>
    public Player LocalPlayer => Players.FirstOrDefault(p => p.IsLocalPlayer) ?? Players[0];

    /// <summary>True when running a single-player demo game (no network).</summary>
    public bool IsSinglePlayer { get; set; }

    /// <summary>Per-player round state keyed by player index in Players list.</summary>
    public Dictionary<int, PlayerTurnState> TurnStates { get; }

    /// <summary>Set when an arcana activation is waiting for a target to be selected.</summary>
    public PendingActivation? PendingActivation { get; set; }

    /// <summary>Populated when Phase transitions to Finished. Sorted by TotalDamageDealt descending.</summary>
    public List<(Player Player, int TotalDamageDealt)> FinalScores { get; set; } = new();

    public Player? CurrentPlayer => Players.Count > 0 && CurrentPlayerIndex < Players.Count
        ? Players[CurrentPlayerIndex]
        : null;

    /// <summary>True when the local player is the active player (or in single-player mode).</summary>
    public bool IsLocalPlayerTurn => IsSinglePlayer || CurrentPlayer == LocalPlayer;

    public bool AllPlayersValidated  => TurnStates.Values.All(s => s.HasValidated);
    public bool AllPlayersEndedShop  => TurnStates.Values.All(s => s.HasEndedShop);

    /// <summary>Set by HostGameController each frame. 0 = timer not running. Used by HUD to display countdown.</summary>
    public float ValidateSecondsRemaining { get; set; }

    public GameState(string localPlayerName)
    {
        PhaseManager = new TurnPhaseManager();
        Players = new List<Player>(MaxPlayers);
        Phase = GamePhase.WaitingForPlayers;
        CurrentPlayerIndex = 0;

        Players.Add(new Player(localPlayerName, isLocalPlayer: true));

        TurnStates = new Dictionary<int, PlayerTurnState>();
    }

    /// <summary>Sets the game as started and initializes TurnStates for all players.</summary>
    public void StartGame()
    {
        Phase = GamePhase.InProgress;
        PhaseManager.CurrentTurnPhase = TurnPhase.DrawPhase;
        PhaseManager.CurrentTurn = 1;

        TurnStates.Clear();
        for (int i = 0; i < Players.Count; i++)
            TurnStates[i] = new PlayerTurnState();
    }

    /// <summary>Delegates to PhaseManager.AdvanceTurnPhase().</summary>
    public void AdvanceTurnPhase() => PhaseManager.AdvanceTurnPhase();

    /// <summary>Delegates to PhaseManager.AdvanceToNextTurn().</summary>
    public void AdvanceToNextTurn() => PhaseManager.AdvanceToNextTurn(TurnStates);

    /// <summary>Delegates to VictoryChecker.</summary>
    public bool CheckVictory() => VictoryChecker.CheckVictory(this);

    /// <summary>Returns the index of the next alive, non-validated player after <paramref name="from"/>, or -1 if none.</summary>
    public int FindNextActivePlayer(int from)
    {
        for (int i = 1; i <= Players.Count; i++)
        {
            int idx = (from + i) % Players.Count;
            if (Players[idx].Health > 0 && TurnStates[idx].HasValidated == false)
                return idx;
        }
        return -1;
    }
}
