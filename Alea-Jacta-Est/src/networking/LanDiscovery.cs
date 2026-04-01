using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Net;

namespace Alea_Jacta_Est.Networking;

/// <summary>
/// LAN discovery via unconnected UDP broadcast.
/// Host runs a Responder; clients run a Scanner.
/// </summary>
public class LanDiscovery
{
    public const int BroadcastPort = 27016;

    private const string RequestMagic  = "AJE_DISCOVER";
    private const string ResponseMagic = "AJE_HERE";

    // ── Discovered rooms (client side) ────────────────────────────────────────
    public record DiscoveredRoom(string HostName, string Ip, int PlayerCount, int MaxPlayers, DateTime LastSeen);

    private readonly List<DiscoveredRoom> _rooms = new();
    public IReadOnlyList<DiscoveredRoom> DiscoveredRooms => _rooms;

    // ── Responder (host side) ─────────────────────────────────────────────────
    private NetManager?  _responder;
    private string       _hostName   = "";
    private int          _maxPlayers = 4;
    private int          _playerCount = 0;

    private class ResponderListener : INetEventListener
    {
        private readonly LanDiscovery _parent;
        public ResponderListener(LanDiscovery p) => _parent = p;

        public void OnNetworkReceiveUnconnected(IPEndPoint remote, NetPacketReader reader, UnconnectedMessageType msgType)
        {
            if (msgType != UnconnectedMessageType.Broadcast) return;
            var msg = reader.GetString();
            if (msg != RequestMagic) return;

            var reply = new NetDataWriter();
            reply.Put(ResponseMagic);
            reply.Put(_parent._hostName);
            reply.Put(_parent._playerCount);
            reply.Put(_parent._maxPlayers);
            _parent._responder!.SendUnconnectedMessage(reply, remote);
        }

        public void OnConnectionRequest(ConnectionRequest r)                                       { }
        public void OnPeerConnected(NetPeer p)                                                     { }
        public void OnPeerDisconnected(NetPeer p, DisconnectInfo i)                                { }
        public void OnNetworkReceive(NetPeer p, NetPacketReader r, byte c, DeliveryMethod m)       { }
        public void OnNetworkError(IPEndPoint ep, System.Net.Sockets.SocketError e)               { }
        public void OnNetworkLatencyUpdate(NetPeer p, int latency)                                 { }
    }

    public void StartResponder(string hostName, int maxPlayers, int currentPlayerCount)
    {
        _hostName    = hostName;
        _maxPlayers  = maxPlayers;
        _playerCount = currentPlayerCount;

        _responder = new NetManager(new ResponderListener(this))
        {
            AutoRecycle         = true,
            UnconnectedMessagesEnabled = true,
            BroadcastReceiveEnabled    = true,
        };
        _responder.Start(BroadcastPort);
    }

    public void UpdateResponder(int currentPlayerCount) => _playerCount = currentPlayerCount;

    public void StopResponder()
    {
        _responder?.Stop();
        _responder = null;
    }

    public void PollResponder() => _responder?.PollEvents();

    // ── Scanner (client side) ─────────────────────────────────────────────────
    private NetManager?  _scanner;
    private float        _broadcastTimer = 0f;
    private const float  BroadcastInterval = 2f;

    private class ScannerListener : INetEventListener
    {
        private readonly LanDiscovery _parent;
        public ScannerListener(LanDiscovery p) => _parent = p;

        public void OnNetworkReceiveUnconnected(IPEndPoint remote, NetPacketReader reader, UnconnectedMessageType msgType)
        {
            if (msgType != UnconnectedMessageType.BasicMessage) return;
            var magic = reader.GetString();
            if (magic != ResponseMagic) return;

            var hostName    = reader.GetString();
            var playerCount = reader.GetInt();
            var maxPlayers  = reader.GetInt();
            var ip          = remote.Address.ToString();

            _parent.UpsertRoom(hostName, ip, playerCount, maxPlayers);
        }

        public void OnConnectionRequest(ConnectionRequest r)                                       { }
        public void OnPeerConnected(NetPeer p)                                                     { }
        public void OnPeerDisconnected(NetPeer p, DisconnectInfo i)                                { }
        public void OnNetworkReceive(NetPeer p, NetPacketReader r, byte c, DeliveryMethod m)       { }
        public void OnNetworkError(IPEndPoint ep, System.Net.Sockets.SocketError e)               { }
        public void OnNetworkLatencyUpdate(NetPeer p, int latency)                                 { }
    }

    public void StartScanner()
    {
        _scanner = new NetManager(new ScannerListener(this))
        {
            AutoRecycle                = true,
            UnconnectedMessagesEnabled = true,
        };
        _scanner.Start();
        _broadcastTimer = BroadcastInterval; // send first broadcast immediately
    }

    public void StopScanner()
    {
        _scanner?.Stop();
        _scanner = null;
        _rooms.Clear();
    }

    public void PollScanner(float deltaSeconds)
    {
        if (_scanner == null) return;
        _scanner.PollEvents();

        _broadcastTimer -= deltaSeconds;
        if (_broadcastTimer <= 0f)
        {
            _broadcastTimer = BroadcastInterval;
            SendBroadcast();
        }

        PruneStaleRooms();
    }

    private void SendBroadcast()
    {
        var writer = new NetDataWriter();
        writer.Put(RequestMagic);
        _scanner!.SendBroadcast(writer, BroadcastPort);
    }

    private void UpsertRoom(string hostName, string ip, int playerCount, int maxPlayers)
    {
        for (int i = 0; i < _rooms.Count; i++)
        {
            if (_rooms[i].Ip == ip)
            {
                _rooms[i] = new DiscoveredRoom(hostName, ip, playerCount, maxPlayers, DateTime.UtcNow);
                return;
            }
        }
        _rooms.Add(new DiscoveredRoom(hostName, ip, playerCount, maxPlayers, DateTime.UtcNow));
    }

    private void PruneStaleRooms()
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-5);
        _rooms.RemoveAll(r => r.LastSeen < cutoff);
    }
}
