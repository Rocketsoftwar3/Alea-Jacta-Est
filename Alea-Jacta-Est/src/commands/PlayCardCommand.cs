using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public record PlayCardCommand(Player Player, ValueCard Card) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        Player.Decks["HandDeck"].RemoveCard(Card);
        Player.Decks["BoardDeck0"].AddCard(Card);
        events.Publish(new CardPlacedOnBoard(Player, Card));
    }
}
