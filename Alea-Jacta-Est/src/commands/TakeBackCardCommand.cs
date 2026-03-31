using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public record TakeBackCardCommand(Player Player, ValueCard Card) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        Player.Decks["BoardDeck0"].RemoveCard(Card);
        Player.Decks["HandDeck"].AddCard(Card);
        events.Publish(new CardTakenBack(Player, Card));
    }
}
