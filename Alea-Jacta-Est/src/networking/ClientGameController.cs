using MessagePack;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Services;

namespace Alea_Jacta_Est.Networking;

/// <summary>
/// Runs on clients. Receives GameStateSync snapshots from the host and applies
/// them to the local GameState so the UI stays in sync.
/// </summary>
public class ClientGameController
{
    private readonly NetworkManager  _network;
    private readonly GameState       _state;
    private readonly EventBus        _events;
    private readonly CardFactory     _cardFactory;
    private readonly GraphicsResources _gfx;

    private TurnPhase _lastKnownPhase;

    public ClientGameController(
        NetworkManager network,
        GameState      state,
        EventBus       events,
        CardFactory    cardFactory,
        GraphicsResources gfx)
    {
        _network     = network;
        _state       = state;
        _events      = events;
        _cardFactory = cardFactory;
        _gfx         = gfx;

        _lastKnownPhase = state.CurrentTurnPhase;

        _network.OnDataReceived  += OnDataReceived;
        _network.OnDisconnected  += OnDisconnectedFromHost;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnDataReceived(int peerId, byte[] data)
    {
        if (data.Length < 2) return;
        if (data[0] != NetMsgType.GameStateSync) return;

        GameStateSnapshot snap;
        try
        {
            var payload = new byte[data.Length - 1];
            System.Array.Copy(data, 1, payload, 0, payload.Length);
            snap = MessagePackSerializer.Deserialize<GameStateSnapshot>(payload);
        }
        catch
        {
            return; // malformed — ignore
        }

        var phaseBefore = _state.CurrentTurnPhase;
        GameStateSnapshot.ApplyToGameState(snap, _state, _cardFactory, _gfx);

        if (_state.CurrentTurnPhase != phaseBefore)
            _events.Publish(new TurnPhaseChanged(_state.CurrentTurnPhase));
    }

    private void OnDisconnectedFromHost(string reason)
    {
        // Surface to the UI via the event bus so an overlay can be shown.
        _events.Publish(new NetworkDisconnected(reason));
    }
}
