using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public record ActivateArcanaCommand(Player Player, ArcanaCard Card, Player? Target, EffectManager EffectManager) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        Player.Decks["ArcanaHandDeck"].RemoveCard(Card);
        Player.Decks["ArcanaDiscardDeck"].AddCard(Card);

        EffectManager.PlayCard(state, Card);
        events.Publish(new CardPlayed(Player, Card));
    }
}
