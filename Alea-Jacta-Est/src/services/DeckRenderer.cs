using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Services;

public class DeckRenderer
{
    private readonly CardRenderer _cardRenderer;

    public DeckRenderer(CardRenderer cardRenderer)
    {
        _cardRenderer = cardRenderer;
    }

    public void Draw(Deck deck)
    {
        foreach (var card in deck.Cards)
        {
            _cardRenderer.Draw(card);
        }
    }

    public void Draw(IEnumerable<Deck> decks)
    {
        foreach (var deck in decks)
        {
            Draw(deck);
        }
    }
}
