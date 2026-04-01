using System.Collections.Generic;
using System.Linq;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Services;

namespace Alea_Jacta_Est.Commands;

/// <summary>Discard selected cards and draw replacements from MainDeck.</summary>
public record DiscardCardsCommand(Player Player, List<Card> Cards) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        int pidx = state.Players.IndexOf(Player);
        if (!state.TurnStates.TryGetValue(pidx, out var ts)) return;
        if (ts.DiscardsRemaining <= 0) return;

        var hand = Player.Decks[DeckType.HandDeck];
        var discard = Player.Decks[DeckType.DiscardDeck];
        var mainDeck = Player.Decks[DeckType.MainDeck];

        int discardCount = 0;
        foreach (var card in Cards)
        {
            if (hand.Cards.Contains(card))
            {
                hand.RemoveCard(card);
                discard.AddCard(card);
                discardCount++;
            }
        }

        // Draw replacements
        if (mainDeck.Cards.Count < discardCount && discard.Cards.Count > 0)
        {
            // Don't reshuffle the cards we just discarded — they should stay in discard
            // Only reshuffle if main deck was already empty before
        }

        int toDraw = System.Math.Min(discardCount, mainDeck.Cards.Count);
        for (int i = 0; i < toDraw; i++)
        {
            var drawn = mainDeck.DrawCard();
            if (drawn != null) hand.AddCard(drawn);
        }

        ts.DiscardsRemaining--;
        ts.SelectedCards.Clear();
    }
}
