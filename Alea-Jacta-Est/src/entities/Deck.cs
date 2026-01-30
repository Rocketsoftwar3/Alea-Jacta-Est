using System.Collections.Generic;

namespace Alea_Jacta_Est.Entities;

/// <summary>Represents a collection of cards.</summary>
public class Deck
{
    public List<Card> Cards { get; }

    public Deck()
    {
        Cards = new List<Card>();
    }

    /// <summary>Adds a single card to the deck.</summary>
    public void AddCard(Card card)
    {
        Cards.Add(card);
    }

    /// <summary>Adds multiple cards to the deck.</summary>
    public void AddCards(IEnumerable<Card> cards)
    {
        Cards.AddRange(cards);
    }

    /// <summary>Removes a card from the deck.</summary>
    public bool RemoveCard(Card card)
    {
        return Cards.Remove(card);
    }

    /// <summary>Draws the top card from the deck.</summary>
    public Card? DrawCard()
    {
        if (Cards.Count == 0) return null;
        var card = Cards[^1];
        Cards.RemoveAt(Cards.Count - 1);
        return card;
    }
}
