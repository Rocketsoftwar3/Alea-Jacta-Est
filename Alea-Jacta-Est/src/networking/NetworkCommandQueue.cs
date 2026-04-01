using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Networking;

/// <summary>
/// In multiplayer: clients serialize commands and send to host;
/// the host enqueues them in its local CommandQueue for authoritative execution.
/// In single-player: passes through to the inner CommandQueue directly.
/// </summary>
public class NetworkCommandQueue : ICommandQueue
{
    private readonly CommandQueue   _local;
    private readonly NetworkManager _network;

    /// <summary>Current game state — updated by Game1 when the state object is replaced at game start.</summary>
    public GameState? State { get; set; }

    /// <summary>True = client (sends commands to host); false = host or single-player (executes locally).</summary>
    public bool IsClient { get; set; }

    public NetworkCommandQueue(CommandQueue local, NetworkManager network)
    {
        _local   = local;
        _network = network;
    }

    public void Enqueue(IGameCommand command)
    {
        // Local-only commands always execute locally, even on clients.
        if (command is ReturnToMenuCommand)
        {
            _local.Enqueue(command);
            return;
        }

        if (!IsClient)
        {
            // Host or single-player: execute locally.
            _local.Enqueue(command);
            return;
        }

        if (State == null) return;

        // Client: serialize and send to host. The host will execute and broadcast a snapshot.
        var bytes = CommandSerializer.Serialize(command, State);
        _network.SendToHost(bytes);
        // Do NOT enqueue locally on clients — the host snapshot will update our state.
    }
}
