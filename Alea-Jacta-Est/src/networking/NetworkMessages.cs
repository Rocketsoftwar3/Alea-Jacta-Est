using MessagePack;
using System.Collections.Generic;

namespace Alea_Jacta_Est.Networking;

public static class NetMsgType
{
    public const byte JoinRoom      = 2;
    public const byte PlayerJoined  = 3;
    public const byte PlayerLeft    = 4;
    public const byte RoomInfo      = 5;
    public const byte StartGame     = 6;
    public const byte ReadyToggle   = 7;
    public const byte Kick          = 9;
    public const byte GameCommand   = 20;
    public const byte GameStateSync = 21;
    public const byte RequestSync   = 22;
    public const byte Disconnect    = 30;
}

[MessagePackObject]
public class JoinRoomRequest
{
    [Key(0)] public string PlayerName { get; set; } = "";
}

[MessagePackObject]
public class JoinRoomResponse
{
    [Key(0)] public bool Accepted       { get; set; }
    [Key(1)] public string? Reason      { get; set; }
    [Key(2)] public int AssignedPlayerId { get; set; }
    [Key(3)] public RoomInfoMessage? RoomInfo { get; set; }
}

[MessagePackObject]
public class PlayerJoinedMessage
{
    [Key(0)] public int    PlayerId   { get; set; }
    [Key(1)] public string PlayerName { get; set; } = "";
}

[MessagePackObject]
public class PlayerLeftMessage
{
    [Key(0)] public int    PlayerId { get; set; }
    [Key(1)] public string Reason   { get; set; } = "";
}

[MessagePackObject]
public class RoomInfoMessage
{
    [Key(0)] public string              HostName    { get; set; } = "";
    [Key(1)] public int                 PlayerCount { get; set; }
    [Key(2)] public int                 MaxPlayers  { get; set; }
    [Key(3)] public List<LobbyPlayerInfo> Players   { get; set; } = new();
}

[MessagePackObject]
public class LobbyPlayerInfo
{
    [Key(0)] public int    Id       { get; set; }
    [Key(1)] public string Name     { get; set; } = "";
    [Key(2)] public bool   IsReady  { get; set; }
    [Key(3)] public bool   IsHost   { get; set; }
}

[MessagePackObject]
public class ReadyToggleMessage
{
    [Key(0)] public int  PlayerId { get; set; }
    [Key(1)] public bool IsReady  { get; set; }
}

[MessagePackObject]
public class StartGameMessage
{
    [Key(0)] public long Seed { get; set; }
}

[MessagePackObject]
public class KickMessage
{
    [Key(0)] public int    PlayerId { get; set; }
    [Key(1)] public string Reason   { get; set; } = "Exclu par l'hôte";
}
