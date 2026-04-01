using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alea_Jacta_Est.Networking;

public enum LobbyState
{
    Disconnected,
    Hosting,
    Connecting,
    InLobby,
    StartingGame
}

public class LobbyPlayer
{
    public int    PeerId   { get; set; }   // -1 for the host itself
    public int    PlayerId { get; set; }   // 0-3
    public string Name     { get; set; } = "";
    public bool   IsReady  { get; set; }
    public bool   IsHost   { get; set; }
}

public class LobbyManager
{
    // ── State ─────────────────────────────────────────────────────────────────
    public LobbyState      State        { get; private set; } = LobbyState.Disconnected;
    public List<LobbyPlayer> Players    { get; } = new();
    public int             MaxPlayers   { get; private set; } = 4;
    public string?         ErrorMessage { get; private set; }
    public bool            IsGameReady  { get; private set; }
    public long            GameSeed     { get; private set; }

    /// <summary>Subscribe to receive log lines for the lobby screen.</summary>
    public event Action<string>? OnLog;

    private readonly NetworkManager _network;
    private readonly LanDiscovery   _lan;
    private string _localPlayerName = "Joueur";
    private float  _deltaAccum;

    public LobbyManager(NetworkManager network, LanDiscovery lan)
    {
        _network = network;
        _lan     = lan;

        _network.OnJoinRequested          += HandleJoinRequested;
        _network.OnJoinResponse           += HandleJoinResponse;
        _network.OnRemotePeerDisconnected += HandlePeerDisconnected;
        _network.OnDataReceived           += HandleDataReceived;
        _network.OnDisconnected           += reason => SetError($"Déconnecté : {reason}");
        _network.OnConnectedToHost        += SendPendingJoinRequest;
    }

    // ── Host actions ──────────────────────────────────────────────────────────
    public void CreateRoom(string playerName, int maxPlayers)
    {
        _localPlayerName = playerName;
        MaxPlayers       = maxPlayers;

        Players.Clear();
        Players.Add(new LobbyPlayer { PeerId = -1, PlayerId = 0, Name = playerName, IsReady = true, IsHost = true });

        _network.StartHost();
        _lan.StartResponder(playerName, maxPlayers, 1);
        State = LobbyState.Hosting;
        ErrorMessage = null;
        Log($"Salle créée. En attente sur le port {NetworkManager.GamePort}...");
    }

    public void StartGame()
    {
        if (State != LobbyState.Hosting) return;
        if (Players.Count < 2) { SetError("Il faut au moins 2 joueurs"); return; }
        if (!Players.All(p => p.IsReady))  { SetError("Tous les joueurs doivent être prêts"); return; }

        GameSeed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var msg  = NetSerializer.Serialize(NetMsgType.StartGame, new StartGameMessage { Seed = GameSeed });
        _network.BroadcastToAll(msg);
        State        = LobbyState.StartingGame;
        IsGameReady  = true;
        Log($"Partie lancée ! (seed={GameSeed})");
    }

    public void KickPlayer(int playerId)
    {
        var p = Players.FirstOrDefault(x => x.PlayerId == playerId);
        if (p == null || p.IsHost) return;

        var msg = NetSerializer.Serialize(NetMsgType.Kick, new KickMessage { PlayerId = playerId });
        _network.SendToPeer(p.PeerId, msg);
        _network.KickPeer(p.PeerId);
        Players.Remove(p);
        Log($"{p.Name} a été exclu.");
        BroadcastRoomInfo();
    }

    // ── Client actions ────────────────────────────────────────────────────────
    public void JoinRoom(string ip, string playerName)
    {
        _localPlayerName = playerName;
        ErrorMessage     = null;
        State            = LobbyState.Connecting;

        Log($"Connexion à {ip}:{NetworkManager.GamePort}...");
        _network.ConnectToHost(ip);
        // JoinRoomRequest is sent in SendPendingJoinRequest() once OnConnectedToHost fires.
    }

    public void JoinDiscoveredRoom(LanDiscovery.DiscoveredRoom room, string playerName)
        => JoinRoom(room.Ip, playerName);

    public void ToggleReady()
    {
        var self = LocalPlayer;
        if (self == null || self.IsHost) return;
        self.IsReady = !self.IsReady;
        Log($"Vous êtes {(self.IsReady ? "prêt" : "pas prêt")}.");
        var msg = NetSerializer.Serialize(NetMsgType.ReadyToggle, new ReadyToggleMessage
        {
            PlayerId = self.PlayerId,
            IsReady  = self.IsReady
        });
        _network.SendToHost(msg);
    }

    public void LeaveLobby()
    {
        _network.Disconnect();
        _lan.StopScanner();
        _lan.StopResponder();
        Players.Clear();
        State        = LobbyState.Disconnected;
        IsGameReady  = false;
        ErrorMessage = null;
    }

    // ── Per-frame update ──────────────────────────────────────────────────────

    public void Update(float deltaSeconds)
    {
        _network.PollEvents();

        if (State == LobbyState.Hosting)
        {
            _lan.PollResponder();
            _deltaAccum += deltaSeconds;
            if (_deltaAccum >= 1f) { _deltaAccum = 0; _lan.UpdateResponder(Players.Count); }
        }

        if ((State == LobbyState.Connecting || State == LobbyState.InLobby) && !_network.IsHost)
            _lan.PollScanner(deltaSeconds);
    }

    private void SendPendingJoinRequest()
    {
        // Called via OnConnectedToHost — _serverPeer is guaranteed set at this point.
        Log("Connecté au serveur, envoi de la demande...");
        var req = NetSerializer.Serialize(NetMsgType.JoinRoom, new JoinRoomRequest { PlayerName = _localPlayerName });
        _network.SendToHost(req);
    }

    // ── Host: incoming events ─────────────────────────────────────────────────
    private void HandleJoinRequested(int peerId, JoinRoomRequest req)
    {
        if (Players.Count >= MaxPlayers)
        {
            var deny = NetSerializer.Serialize(NetMsgType.JoinRoom, new JoinRoomResponse
            {
                Accepted = false,
                Reason   = "La salle est pleine"
            });
            _network.SendToPeer(peerId, deny);
            _network.KickPeer(peerId);
            Log($"Connexion refusée ({req.PlayerName}) : salle pleine.");
            return;
        }

        int newId = Players.Max(p => p.PlayerId) + 1;
        var lp    = new LobbyPlayer { PeerId = peerId, PlayerId = newId, Name = req.PlayerName, IsReady = false };
        Players.Add(lp);
        Log($"{req.PlayerName} a rejoint la salle.");

        // Send accept response with current room state
        var accept = NetSerializer.Serialize(NetMsgType.JoinRoom, new JoinRoomResponse
        {
            Accepted         = true,
            AssignedPlayerId = newId,
            RoomInfo         = BuildRoomInfo()
        });
        _network.SendToPeer(peerId, accept);

        // Notify everyone else
        BroadcastRoomInfo();
    }

    private void HandlePeerDisconnected(int peerId, string reason)
    {
        var lp = Players.FirstOrDefault(p => p.PeerId == peerId);
        if (lp == null) return;
        Players.Remove(lp);
        Log($"{lp.Name} a quitté la salle ({reason}).");
        BroadcastRoomInfo();
    }

    // ── Client: incoming events ───────────────────────────────────────────────
    private void HandleJoinResponse(JoinRoomResponse resp)
    {
        if (!resp.Accepted)
        {
            SetError(resp.Reason ?? "Connexion refusée");
            _network.Disconnect();
            State = LobbyState.Disconnected;
            return;
        }

        // Rebuild Players list from server's room info
        if (resp.RoomInfo != null)
            ApplyRoomInfo(resp.RoomInfo);

        State = LobbyState.InLobby;
        Log($"Connecté ! Joueur #{resp.AssignedPlayerId} dans la salle.");
    }

    private void HandleDataReceived(int peerId, byte[] data)
    {
        var (msgType, payload) = NetSerializer.ReadEnvelope(data);

        switch (msgType)
        {
            case NetMsgType.RoomInfo:
                ApplyRoomInfo(NetSerializer.Deserialize<RoomInfoMessage>(payload));
                break;

            case NetMsgType.StartGame:
                var sg   = NetSerializer.Deserialize<StartGameMessage>(payload);
                GameSeed = sg.Seed;
                State    = LobbyState.StartingGame;
                IsGameReady = true;
                Log("L'hôte a lancé la partie !");
                break;

            case NetMsgType.Kick:
                SetError("Vous avez été exclu de la partie");
                _network.Disconnect();
                State       = LobbyState.Disconnected;
                IsGameReady = false;
                break;

            case NetMsgType.ReadyToggle when _network.IsHost:
                var rt = NetSerializer.Deserialize<ReadyToggleMessage>(payload);
                var rp = Players.FirstOrDefault(p => p.PlayerId == rt.PlayerId);
                if (rp != null)
                {
                    rp.IsReady = rt.IsReady;
                    Log($"{rp.Name} : {(rp.IsReady ? "prêt" : "pas prêt")}.");
                }
                BroadcastRoomInfo();
                break;
        }
    }

    private void Log(string msg) => OnLog?.Invoke(msg);

    private void SetError(string msg) { ErrorMessage = msg; Log($"[Erreur] {msg}"); }

    // ── Helpers ───────────────────────────────────────────────────────────────
    public LobbyPlayer? LocalPlayer => _network.IsHost
        ? Players.FirstOrDefault(p => p.IsHost)
        : Players.FirstOrDefault(p => p.Name == _localPlayerName);

    private RoomInfoMessage BuildRoomInfo() => new()
    {
        HostName    = Players.FirstOrDefault(p => p.IsHost)?.Name ?? "",
        PlayerCount = Players.Count,
        MaxPlayers  = MaxPlayers,
        Players     = Players.Select(p => new LobbyPlayerInfo
        {
            Id      = p.PlayerId,
            Name    = p.Name,
            IsReady = p.IsReady,
            IsHost  = p.IsHost
        }).ToList()
    };

    private void BroadcastRoomInfo()
    {
        var msg = NetSerializer.Serialize(NetMsgType.RoomInfo, BuildRoomInfo());
        _network.BroadcastToAll(msg);
    }

    private void ApplyRoomInfo(RoomInfoMessage info)
    {
        Players.Clear();
        foreach (var p in info.Players)
        {
            // Preserve our peerId if we already know it (client side peerId is unknown for remote players)
            var existing = Players.FirstOrDefault(x => x.PlayerId == p.Id);
            Players.Add(new LobbyPlayer
            {
                PeerId   = existing?.PeerId ?? -1,
                PlayerId = p.Id,
                Name     = p.Name,
                IsReady  = p.IsReady,
                IsHost   = p.IsHost
            });
        }
    }

    /// <summary>
    /// Builds the initial GameState from lobby data.
    /// Called by Game1 when transitioning from lobby to gameplay.
    /// </summary>
    public GameState BuildGameState()
    {
        // Players must be in the same order on every machine (by PlayerId).
        // Host is always PlayerId=0 → index 0 in state.Players.
        // LocalPlayer is resolved by the IsLocalPlayer flag (GameState.LocalPlayer is now computed).
        var host      = Players.First(p => p.IsHost);
        var hostLocal = _network.IsHost;

        // GameState ctor creates the host player at index 0 with isLocalPlayer=true.
        var state = new GameState(host.Name);

        // If we're a client, the host at index 0 is NOT our local player — fix the flag.
        if (!hostLocal)
            state.Players[0].IsLocalPlayer = false;

        // Add remaining players in PlayerId order.
        var ordered = Players.OrderBy(p => p.PlayerId).ToList();
        foreach (var lp in ordered)
        {
            if (lp.IsHost) continue; // already at index 0

            bool isLocal = !hostLocal && lp.Name == _localPlayerName;
            var player   = new Player(lp.Name, lp.PlayerId, isLocalPlayer: isLocal);
            state.Players.Add(player);
        }

        return state;
    }
}
