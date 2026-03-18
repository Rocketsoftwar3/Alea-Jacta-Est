using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public record BuyCardCommand(Player Buyer, Card Card, int Price) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        Buyer.Wallet -= Price;
        Buyer.Market.Deck.RemoveCard(Card);
        Buyer.Decks[DeckType.MainDeck.ToString()].AddCard(Card);
        events.Publish(new CardPurchased(Buyer, Card, Price));
    }
}
