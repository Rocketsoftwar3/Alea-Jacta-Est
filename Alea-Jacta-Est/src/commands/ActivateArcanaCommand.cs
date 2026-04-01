using System.Linq;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public record ActivateArcanaCommand(Player Player, ArcanaCard Card, Player? Target, EffectManager EffectManager) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        bool needsTarget = Card.Effects.Any(e => e.RequiresTarget(Card));
        if (needsTarget)
        {
            // Open target selection modal; card stays in hand until target is confirmed
            state.PendingActivation = new PendingActivation(Card, Player);
            return;
        }

        Player.Decks[DeckType.ArcanaHandDeck].RemoveCard(Card);
        Player.Decks[DeckType.ArcanaDiscardDeck].AddCard(Card);

        EffectManager.PlayCard(state, Card, Player);
        events.Publish(new CardPlayed(Player, Card));

        // Track arcana count for Chariot endroit limit
        int idx = state.Players.IndexOf(Player);
        if (state.TurnStates.TryGetValue(idx, out var ts))
            ts.ArcanasPlayedThisTurn++;
    }
}
