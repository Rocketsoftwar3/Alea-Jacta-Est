using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public interface IGameCommand
{
    void Execute(GameState state, EventBus events);
}
