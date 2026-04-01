using System;
using System.Collections.Generic;
using Alea_Jacta_Est.Config;

namespace Alea_Jacta_Est.Entities;

/// <summary>Represents a player in the game with all their card decks.</summary>
public class Player
{
    public string Name { get; set; }
    public int Id { get; set; }
    public int Wallet { get; set; }
    public int Health { get; set; }
    public bool IsLocalPlayer { get; set; }

    /// <summary>All decks for this player, indexed by DeckType.</summary>
    public Dictionary<DeckType, Deck> Decks { get; }

    /// <summary>Market with available cards for purchase.</summary>
    public Market Market { get; }

    public Player(string name, int id = 0, bool isLocalPlayer = false)
    {
        Name = name;
        Id = id;
        Wallet = 0;
        Health = 100;
        IsLocalPlayer = isLocalPlayer;

        Decks = new Dictionary<DeckType, Deck>
        {
            [DeckType.MainDeck] = new Deck(),
            [DeckType.SpecialDeck] = new Deck(),
            [DeckType.DiscardDeck] = new Deck(),
            [DeckType.BoardDeck0] = new Deck(),
            [DeckType.BoardDeck1] = new Deck(),
            [DeckType.BoardDeck2] = new Deck(),
            [DeckType.BoardDeck3] = new Deck(),
            [DeckType.HandDeck] = new Deck(),
            [DeckType.ArcanaHandDeck] = new Deck(),
            [DeckType.ArcanaDiscardDeck] = new Deck()
        };

        Market = new Market();
    }

    /// <summary>Temporary bridge for UI services still using string keys. Remove after UI migration.</summary>
    public Deck GetDeck(string key) => Decks[Enum.Parse<DeckType>(key)];
}
