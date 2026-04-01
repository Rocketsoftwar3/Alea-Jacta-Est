using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public record TakeBackCardCommand(Player Player, ValueCard Card) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        Player.Decks[DeckType.BoardDeck0].RemoveCard(Card);
        Player.Decks[DeckType.HandDeck].AddCard(Card);
        events.Publish(new CardTakenBack(Player, Card));
    }
}
