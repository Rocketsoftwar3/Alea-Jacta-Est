using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public record PlayCardCommand(Player Player, ValueCard Card) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        Player.Decks[DeckType.HandDeck].RemoveCard(Card);
        Player.Decks[DeckType.BoardDeck0].AddCard(Card);
        events.Publish(new CardPlacedOnBoard(Player, Card));
    }
}
