using System.Collections.Generic;

namespace Alea_Jacta_Est.Entities;

/// <summary>Represents a player in the game with all their card decks.</summary>
public class Player
{
    public string Name { get; set; }
    public int Id { get; set; }
    public int Wallet { get; set; }
    public bool IsLocalPlayer { get; set; }

    /// <summary>All decks for this player, indexed by name.</summary>
    public Dictionary<string, Deck> Decks { get; }

    /// <summary>Market with available cards for purchase.</summary>
    public Market Market { get; }

    public Player(string name, int id = 0, bool isLocalPlayer = false)
    {
        Name = name;
        Id = id;
        Wallet = 0;
        IsLocalPlayer = isLocalPlayer;

        Decks = new Dictionary<string, Deck>
        {
            ["MainDeck"] = new Deck(),
            ["SpecialDeck"] = new Deck(),
            ["DiscardDeck"] = new Deck(),
            ["BoardDeck0"] = new Deck(),
            ["BoardDeck1"] = new Deck(),
            ["BoardDeck2"] = new Deck(),
            ["BoardDeck3"] = new Deck()
        };

        Market = new Market();
    }
}
