using System.Linq;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Services;

namespace Alea_Jacta_Est.Commands;

public record DrawCardCommand(Player Player, DeckType SourceDeck) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        int pidx = state.Players.IndexOf(Player);
        if (!state.TurnStates.TryGetValue(pidx, out var ts)) return;

        if (SourceDeck == DeckType.MainDeck && ts.ValueDrawsRemaining > 0)
        {
            var mainDeck = Player.Decks[DeckType.MainDeck];
            var hand = Player.Decks[DeckType.HandDeck];
            var discard = Player.Decks[DeckType.DiscardDeck];

            if (mainDeck.Cards.Count == 0 && discard.Cards.Count > 0)
            {
                mainDeck.AddCards(discard.Cards.ToList());
                discard.Cards.Clear();
                TurnService.Shuffle(mainDeck.Cards);
            }

            var card = mainDeck.DrawCard();
            if (card != null)
            {
                hand.AddCard(card);
                ts.ValueDrawsRemaining--;
            }
        }
        else if (SourceDeck == DeckType.SpecialDeck && ts.ArcanaDrawsRemaining > 0)
        {
            var specialDeck = Player.Decks[DeckType.SpecialDeck];
            var arcanaHand = Player.Decks[DeckType.ArcanaHandDeck];
            var arcanaDiscard = Player.Decks[DeckType.ArcanaDiscardDeck];

            if (specialDeck.Cards.Count == 0 && arcanaDiscard.Cards.Count > 0)
            {
                specialDeck.AddCards(arcanaDiscard.Cards.ToList());
                arcanaDiscard.Cards.Clear();
                TurnService.Shuffle(specialDeck.Cards);
            }

            var card = specialDeck.DrawCard();
            if (card != null)
            {
                arcanaHand.AddCard(card);
                ts.ArcanaDrawsRemaining--;
            }
        }
    }
}
