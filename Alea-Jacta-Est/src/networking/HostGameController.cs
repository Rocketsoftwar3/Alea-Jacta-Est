using System.Collections.Generic;
using MessagePack;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Services;

namespace Alea_Jacta_Est.Networking;

/// <summary>
/// Runs on the host. Receives serialized commands from clients, executes them
/// authoritatively, then broadcasts a full GameStateSync to all peers.
/// Also handles the auto-validate timeout and peer disconnection.
/// </summary>
public class HostGameController
{
    private readonly NetworkManager  _network;
    private readonly CommandQueue    _localQueue;
    private readonly GameState       _state;
    private readonly EventBus        _events;
    private readonly EffectManager   _effectManager;
    private readonly CardFactory     _cardFactory;
    private readonly GraphicsResources _gfx;

    // Auto-validate: if not all players validated within this window, host forces resolution.
    private const float AutoValidateSeconds = 60f;
    private float _validateTimer;
    private bool  _timerRunning;
    private int   _lastKnownPlayerIndex = -1; // detect CurrentPlayerIndex changes to reset timer

    // Tracks which LiteNetLib peer owns which player index.
    private readonly Dictionary<int, int> _peerToPlayerIndex = new(); // netId → playerIndex

    public HostGameController(
        NetworkManager network,
        CommandQueue   localQueue,
        GameState      state,
        EventBus       events,
        EffectManager  effectManager,
        CardFactory    cardFactory,
        GraphicsResources gfx)
    {
        _network       = network;
        _localQueue    = localQueue;
        _state         = state;
        _events        = events;
        _effectManager = effectManager;
        _cardFactory   = cardFactory;
        _gfx           = gfx;

        _network.OnDataReceived         += OnDataReceived;
        _network.OnRemotePeerDisconnected += OnPeerLeft;
    }

    /// <summary>Call after the game starts to register peer → player index mapping.</summary>
    public void RegisterPeer(int netPeerId, int playerIndex)
    {
        _peerToPlayerIndex[netPeerId] = playerIndex;
    }

    public void Update(float dt)
    {
        if (_state.IsSinglePlayer) return;

        switch (_state.CurrentTurnPhase)
        {
            case TurnPhase.PlayPhase:
                if (_state.AllPlayersValidated)
                {
                    // All players have taken their turn → go straight to resolution.
                    _timerRunning = false;
                    _state.ValidateSecondsRemaining = 0f;
                    _state.AdvanceTurnPhase(); // PlayPhase → ValidatePhase
                    _state.AdvanceTurnPhase(); // ValidatePhase → ResolutionPhase
                    _events.Publish(new TurnPhaseChanged(_state.CurrentTurnPhase));
                    BroadcastSnapshot();
                    break;
                }
                // Per-player auto-validate timer: reset when the active player changes.
                if (_state.CurrentPlayerIndex != _lastKnownPlayerIndex)
                {
                    _lastKnownPlayerIndex = _state.CurrentPlayerIndex;
                    _timerRunning = false; // force restart below
                }
                if (!_timerRunning)
                {
                    _validateTimer = AutoValidateSeconds;
                    _timerRunning  = true;
                }
                else
                {
                    _validateTimer -= dt;
                    _state.ValidateSecondsRemaining = System.Math.Max(0f, _validateTimer);
                    if (_validateTimer <= 0f)
                        ForceValidateCurrent();
                }
                break;

            case TurnPhase.ShopPhase:
                if (_state.AllPlayersEndedShop)
                {
                    _state.AdvanceToNextTurn();
                    _state.AdvanceTurnPhase(); // ShopPhase → DrawPhase
                    _events.Publish(new TurnPhaseChanged(_state.CurrentTurnPhase));
                    BroadcastSnapshot();
                }
                break;
        }
    }

    /// <summary>Returns seconds remaining on the auto-validate timer (0 if not running).</summary>
    public float ValidateTimeRemaining => _timerRunning ? System.Math.Max(0f, _validateTimer) : 0f;

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnDataReceived(int peerId, byte[] data)
    {
        if (data.Length < 1) return;

        // Client asking for a full state snapshot (e.g. after late join or timing gap)
        if (data[0] == NetMsgType.RequestSync)
        {
            SendSnapshotToPeer(peerId);
            return;
        }

        if (data[0] != NetMsgType.GameCommand) return;

        IGameCommand? cmd;
        try
        {
            cmd = CommandSerializer.Deserialize(data, _state, _effectManager);
        }
        catch
        {
            return; // malformed packet — ignore
        }

        if (cmd != null)
            _localQueue.Enqueue(cmd);

        // Broadcast happens after ExecuteAll in the Update loop (see Game1.cs).
        // We mark that a broadcast is pending so Game1 can call BroadcastSnapshot().
        _pendingBroadcast = true;
    }

    private void OnPeerLeft(int peerId, string reason)
    {
        if (!_peerToPlayerIndex.TryGetValue(peerId, out int pi)) return;
        _peerToPlayerIndex.Remove(peerId);

        // Mark the disconnected player as dead so the game can continue.
        if (pi < _state.Players.Count)
        {
            _state.Players[pi].Health = 0;
            _state.TurnStates[pi].HasValidated = true;
            _state.TurnStates[pi].HasEndedShop = true;
            _events.Publish(new PlayerDisconnected(pi, _state.Players[pi].Name));
        }

        _state.CheckVictory();
        BroadcastSnapshot();
    }

    /// <summary>
    /// Timer expired for the current player: mark them validated and pass to the next player
    /// (or let AllPlayersValidated detection advance to resolution on the next tick).
    /// </summary>
    private void ForceValidateCurrent()
    {
        _timerRunning = false;
        int idx = _state.CurrentPlayerIndex;
        if (_state.TurnStates.TryGetValue(idx, out var ts))
            ts.HasValidated = true;

        // Find next player who still needs to play.
        int next = -1;
        for (int i = 1; i <= _state.Players.Count; i++)
        {
            int ni = (idx + i) % _state.Players.Count;
            if (_state.Players[ni].Health > 0 && !_state.TurnStates[ni].HasValidated)
            { next = ni; break; }
        }
        if (next >= 0)
        {
            _state.CurrentPlayerIndex = next;
            _validateTimer = AutoValidateSeconds;
            _timerRunning  = true;
        }
        // else: AllPlayersValidated → detected on next Update tick → advances to resolution.

        _events.Publish(new TurnPhaseChanged(_state.CurrentTurnPhase));
        BroadcastSnapshot();
    }

    // ── Broadcast ─────────────────────────────────────────────────────────────

    private bool _pendingBroadcast;

    /// <summary>Returns true if a snapshot should be sent this frame (set by incoming command).</summary>
    public bool ConsumePendingBroadcast()
    {
        bool v = _pendingBroadcast;
        _pendingBroadcast = false;
        return v;
    }

    public void BroadcastSnapshot()
    {
        var packet = BuildSnapshotPacket();
        _network.BroadcastToAll(packet);
    }

    private void SendSnapshotToPeer(int peerId)
    {
        var packet = BuildSnapshotPacket();
        _network.SendToPeer(peerId, packet);
    }

    private byte[] BuildSnapshotPacket()
    {
        var snap  = GameStateSnapshot.FromGameState(_state);
        var inner = MessagePackSerializer.Serialize(snap);
        var packet = new byte[1 + inner.Length];
        packet[0] = NetMsgType.GameStateSync;
        inner.CopyTo(packet, 1);
        return packet;
    }
}
