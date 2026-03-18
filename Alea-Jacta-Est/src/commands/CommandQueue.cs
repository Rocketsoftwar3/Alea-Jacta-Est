using System.Collections.Generic;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public class CommandQueue
{
    private readonly List<IGameCommand> _pending = new();

    public void Enqueue(IGameCommand command)
    {
        _pending.Add(command);
    }

    public void ExecuteAll(GameState state, EventBus events)
    {
        foreach (var command in _pending)
            command.Execute(state, events);
        _pending.Clear();
    }
}
