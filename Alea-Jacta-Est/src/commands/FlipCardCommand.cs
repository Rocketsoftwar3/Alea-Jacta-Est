using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public record FlipCardCommand(Card Card) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        Card.IsFlipped = !Card.IsFlipped;
        events.Publish(new CardFlipped(Card));
    }
}
