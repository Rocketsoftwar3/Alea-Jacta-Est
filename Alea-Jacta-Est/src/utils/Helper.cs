using System;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Utils;

/// <summary>Utility helpers for game operations.</summary>
public static class Helper
{
    private static readonly Random _random = new Random();

    /// <summary>Moves a random card from source deck to destination deck.</summary>
    /// <returns>The moved card, or null if source is empty.</returns>
    public static Card? MoveRandomCard(Deck sourceDeck, Deck destinationDeck)
    {
        if (sourceDeck.Cards.Count == 0)
            return null;

        int randomIndex = _random.Next(0, sourceDeck.Cards.Count);
        var card = sourceDeck.Cards[randomIndex];

        sourceDeck.Cards.RemoveAt(randomIndex);
        destinationDeck.AddCard(card);

        return card;
    }

    /// <summary>Generates a random discount between min and max (inclusive).</summary>
    public static int RandomDiscount(int min, int max)
    {
        return _random.Next(min, max + 1);
    }
}
