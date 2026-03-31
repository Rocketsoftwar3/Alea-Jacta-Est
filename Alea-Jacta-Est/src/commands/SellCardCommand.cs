using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public record SellCardCommand(Player Player, Card Card, Deck SourceDeck, int PlayerIndex) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        if (!state.TurnStates.TryGetValue(PlayerIndex, out var turnState))
            return;
        if (turnState.HasSoldThisRound)
            return;

        SourceDeck.RemoveCard(Card);
        Player.Wallet += Card.Price;
        turnState.HasSoldThisRound = true;

        events.Publish(new CardSold(Player, Card, Card.Price));
    }
}
