using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Alea_Jacta_Est.Networking;

public class NetworkManager : INetEventListener
{
    // ── Config ────────────────────────────────────────────────────────────────
    public  const int    GamePort       = 27015;
    private const string ConnectionKey  = "AleaJactaEst_v1";

    // ── State ─────────────────────────────────────────────────────────────────
    public bool IsHost      { get; private set; }
    public bool IsRunning   { get; private set; }

    /// <summary>Host only: currently connected peer IDs.</summary>
    public IReadOnlyCollection<int> ConnectedPeerIds => _peers.Keys;

    private NetManager?                   _netManager;
    private NetPeer?                      _serverPeer;               // client → host
    private readonly Dictionary<int, NetPeer> _peers = new();        // host: peerId → peer

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Host only: a new client connected and sent a JoinRoomRequest.</summary>
    public event Action<int, JoinRoomRequest>? OnJoinRequested;

    /// <summary>Client only: received a JoinRoomResponse from host.</summary>
    public event Action<JoinRoomResponse>? OnJoinResponse;

    /// <summary>Host only: a peer disconnected.</summary>
    public event Action<int, string>? OnRemotePeerDisconnected;

    /// <summary>Raw data received (used by LobbyManager for all lobby messages).</summary>
    public event Action<int, byte[]>? OnDataReceived;

    /// <summary>Client only: connection to host successfully established (peer handshake done).</summary>
    public event Action? OnConnectedToHost;

    /// <summary>Client only: lost connection to host.</summary>
    public event Action<string>? OnDisconnected;

    // ── Host API ──────────────────────────────────────────────────────────────
    public void StartHost()
    {
        IsHost      = true;
        _netManager = new NetManager(this) { AutoRecycle = true };
        _netManager.Start(GamePort);
        IsRunning   = true;
    }

    public void StopHost()
    {
        _netManager?.Stop();
        _peers.Clear();
        IsRunning = false;
        IsHost    = false;
    }

    public void SendToPeer(int peerId, byte[] data, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        if (_peers.TryGetValue(peerId, out var peer))
        {
            var writer = new NetDataWriter();
            writer.Put(data);
            peer.Send(writer, method);
        }
    }

    public void BroadcastToAll(byte[] data, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        var writer = new NetDataWriter();
        writer.Put(data);
        foreach (var peer in _peers.Values)
            peer.Send(writer, method);
    }

    public void BroadcastToAllExcept(int excludePeerId, byte[] data, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        var writer = new NetDataWriter();
        writer.Put(data);
        foreach (var (id, peer) in _peers)
            if (id != excludePeerId)
                peer.Send(writer, method);
    }

    public void KickPeer(int peerId)
    {
        if (_peers.TryGetValue(peerId, out var peer))
            peer.Disconnect();
    }

    // ── Client API ────────────────────────────────────────────────────────────
    public void ConnectToHost(string ip)
    {
        IsHost      = false;
        _netManager = new NetManager(this) { AutoRecycle = true };
        _netManager.Start();
        _netManager.Connect(ip, GamePort, ConnectionKey);
        IsRunning   = true;
    }

    public void Disconnect()
    {
        _serverPeer?.Disconnect();
        _netManager?.Stop();
        IsRunning = false;
    }

    public void SendToHost(byte[] data, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        if (_serverPeer == null) return;
        var writer = new NetDataWriter();
        writer.Put(data);
        _serverPeer.Send(writer, method);
    }

    // ── Frame polling ─────────────────────────────────────────────────────────
    public void PollEvents() => _netManager?.PollEvents();

    // ── INetEventListener ─────────────────────────────────────────────────────
    public void OnConnectionRequest(ConnectionRequest request)
    {
        if (IsHost)
            request.AcceptIfKey(ConnectionKey);
        else
            request.Reject();
    }

    public void OnPeerConnected(NetPeer peer)
    {
        if (IsHost)
        {
            _peers[peer.Id] = peer;
            // Wait for the JoinRoomRequest that the client sends immediately after connecting
        }
        else
        {
            _serverPeer = peer;
            OnConnectedToHost?.Invoke(); // fire AFTER _serverPeer is set
        }
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (IsHost)
        {
            _peers.Remove(peer.Id);
            OnRemotePeerDisconnected?.Invoke(peer.Id, disconnectInfo.Reason.ToString());
        }
        else
        {
            _serverPeer = null;
            IsRunning   = false;
            OnDisconnected?.Invoke(disconnectInfo.Reason.ToString());
        }
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var data = reader.GetRemainingBytes();
        if (data.Length == 0) return;

        var (msgType, payload) = NetSerializer.ReadEnvelope(data);

        if (IsHost && msgType == NetMsgType.JoinRoom)
        {
            var req = NetSerializer.Deserialize<JoinRoomRequest>(payload);
            OnJoinRequested?.Invoke(peer.Id, req);
            return;
        }

        if (!IsHost && msgType == NetMsgType.JoinRoom)
        {
            var resp = NetSerializer.Deserialize<JoinRoomResponse>(payload);
            OnJoinResponse?.Invoke(resp);
            return;
        }

        OnDataReceived?.Invoke(peer.Id, data);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        OnDisconnected?.Invoke($"Erreur réseau : {socketError}");
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
}
