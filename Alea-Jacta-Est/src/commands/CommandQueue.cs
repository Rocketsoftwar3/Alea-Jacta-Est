using System.Collections.Generic;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

public class CommandQueue : ICommandQueue
{
    private readonly List<IGameCommand> _pending = new();

    public void Enqueue(IGameCommand command)
    {
        _pending.Add(command);
    }

    /// <summary>Executes all pending commands and returns the number executed.</summary>
    public int ExecuteAll(GameState state, EventBus events)
    {
        int count = _pending.Count;
        foreach (var command in _pending)
            command.Execute(state, events);
        _pending.Clear();
        return count;
    }
}
