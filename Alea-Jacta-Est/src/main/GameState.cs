using System.Collections.Generic;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Main;

public enum GamePhase
{
    WaitingForPlayers,
    InProgress,
    Finished
}

public class GameState
{
    public const int MaxPlayers = 5;

    public List<Player> Players { get; }
    public GamePhase Phase { get; set; }
    public int CurrentPlayerIndex { get; set; }
    public int CurrentTurn { get; set; }
    public Player LocalPlayer { get; }

    public Player? CurrentPlayer => Players.Count > 0 && CurrentPlayerIndex < Players.Count
        ? Players[CurrentPlayerIndex]
        : null;

    public GameState(string localPlayerName)
    {
        Players = new List<Player>(MaxPlayers);
        Phase = GamePhase.WaitingForPlayers;
        CurrentPlayerIndex = 0;
        CurrentTurn = 0;

        LocalPlayer = new Player(localPlayerName, isLocalPlayer: true);
        Players.Add(LocalPlayer);
    }
}
