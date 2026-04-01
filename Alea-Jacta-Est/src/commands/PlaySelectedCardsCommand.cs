using System.Collections.Generic;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

/// <summary>Play all selected value cards to the board, then validate the turn.</summary>
public record PlaySelectedCardsCommand(Player Player) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        int pidx = state.Players.IndexOf(Player);
        if (!state.TurnStates.TryGetValue(pidx, out var ts)) return;

        var hand = Player.Decks[DeckType.HandDeck];
        var board = Player.Decks[DeckType.BoardDeck0];

        foreach (var card in ts.SelectedCards)
        {
            if (card is ValueCard vc && hand.Cards.Contains(card))
            {
                hand.RemoveCard(card);
                board.AddCard(card);
                events.Publish(new CardPlacedOnBoard(Player, card));
            }
        }

        ts.SelectedCards.Clear();
        ts.HasPlayed = true;
    }
}
