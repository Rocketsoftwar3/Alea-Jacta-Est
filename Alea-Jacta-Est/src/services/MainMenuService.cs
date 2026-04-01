using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Networking;

namespace Alea_Jacta_Est.Services;

/// <summary>
/// Main menu screen, shown when Phase == WaitingForPlayers and lobby is Disconnected.
/// </summary>
public class MainMenuService
{
    private enum Screen { Home, Host, Join }
    private Screen _screen = Screen.Home;

    private string _playerName = PickRandomName();
    private int    _maxPlayers = 4;
    private string _joinIp     = "127.0.0.1";

    private readonly LobbyManager _lobby;
    private readonly LanDiscovery _lan;
    private readonly ICommandQueue _commands;
    private readonly DemoDataService _demoData;

    private static readonly Vector4 ColorTitle  = new(1f, 0.85f, 0.4f, 1f);
    private static readonly Vector4 ColorError  = new(1f, 0.35f, 0.35f, 1f);
    private static readonly Vector4 ColorDim    = new(0.6f, 0.6f, 0.6f, 1f);

    public MainMenuService(LobbyManager lobby, LanDiscovery lan, ICommandQueue commands, DemoDataService demoData)
    {
        _lobby    = lobby;
        _lan      = lan;
        _commands = commands;
        _demoData = demoData;
    }

    public void Render(GameState state)
    {
        var vp      = ImGui.GetMainViewport();
        var center  = new Vector2(vp.WorkSize.X / 2f, vp.WorkSize.Y / 2f);
        var winSize = new Vector2(460, 420);

        ImGui.SetNextWindowPos(center, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(winSize, ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.92f);

        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize
                  | ImGuiWindowFlags.NoMove    | ImGuiWindowFlags.NoScrollbar;

        if (!ImGui.Begin("##MainMenu", flags))
        {
            ImGui.End();
            return;
        }

        // ── Title ─────────────────────────────────────────────────────────────
        var titleW = ImGui.CalcTextSize("ALEA JACTA EST").X;
        ImGui.SetCursorPosX((winSize.X - titleW) / 2f);
        ImGui.TextColored(ColorTitle, "ALEA JACTA EST");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ── Mute Button ───────────────────────────────────────────────────────
        float muteX = winSize.X - 32f - 8f;
        Alea_Jacta_Est.Utils.UIHelper.DrawMuteButton(new Vector2(muteX, 8f));

        // ── Error message ─────────────────────────────────────────────────────
        if (_lobby.ErrorMessage != null)
        {
            ImGui.TextColored(ColorError, _lobby.ErrorMessage);
            ImGui.Spacing();
        }

        switch (_screen)
        {
            case Screen.Home:   RenderHome(state); break;
            case Screen.Host:   RenderHost();      break;
            case Screen.Join:   RenderJoin();      break;
        }

        ImGui.End();
    }

    private void RenderHome(GameState state)
    {
        var btnSize = new Vector2(200, 36);
        float btnX  = (460f - btnSize.X) / 2f;

        ImGui.TextColored(ColorDim, "Nom du joueur");
        ImGui.SetNextItemWidth(200);
        ImGui.SetCursorPosX(btnX);
        ImGui.InputText("##pname", ref _playerName, 32);
        ImGui.Spacing();

        ImGui.SetCursorPosX(btnX);
        if (ImGui.Button("Créer une table", btnSize))
        {
            _screen = Screen.Host;
        }

        ImGui.SetCursorPosX(btnX);
        if (ImGui.Button("Rejoindre une table", btnSize))
        {
            _lan.StartScanner();
            _screen = Screen.Join;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.SetCursorPosX(btnX);
        if (ImGui.Button("Partie solo", btnSize))
        {
            _commands.Enqueue(new StartSinglePlayerCommand(_demoData));
        }
    }

    private void RenderHost()
    {
        ImGui.TextUnformatted("Nombre de joueurs max");
        ImGui.SetNextItemWidth(200);
        ImGui.SliderInt("##maxp", ref _maxPlayers, 2, 4);
        ImGui.Spacing();

        if (ImGui.Button("Lancer le serveur", new Vector2(200, 36)))
        {
            _lobby.CreateRoom(_playerName, _maxPlayers);
        }
        ImGui.SameLine();
        if (ImGui.Button("Retour", new Vector2(80, 36)))
            _screen = Screen.Home;
    }

    private void RenderJoin()
    {
        ImGui.TextUnformatted("Adresse IP");
        ImGui.SetNextItemWidth(240);
        ImGui.InputText("##ip", ref _joinIp, 64);
        ImGui.SameLine();
        if (ImGui.Button("Connexion"))
            _lobby.JoinRoom(_joinIp, _playerName);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(ColorDim, "--- Tables locales ---");
        ImGui.Spacing();

        var rooms = _lan.DiscoveredRooms;
        if (rooms.Count == 0)
        {
            ImGui.TextColored(ColorDim, "Recherche en cours...");
        }
        else
        {
            foreach (var room in rooms)
            {
                ImGui.TextUnformatted($"{room.HostName}  ({room.PlayerCount}/{room.MaxPlayers})  {room.Ip}");
                ImGui.SameLine();
                if (ImGui.Button($"Rejoindre##{room.Ip}"))
                    _lobby.JoinDiscoveredRoom(room, _playerName);
            }
        }

        ImGui.Spacing();
        if (ImGui.Button("Retour", new Vector2(80, 36)))
        {
            _lan.StopScanner();
            _screen = Screen.Home;
        }
    }

    private static string PickRandomName()
    {
        try
        {
            var path  = Path.Combine("Content", "defaultname.txt");
            var names = File.ReadAllLines(path)
                            .Select(l => l.Trim())
                            .Where(l => l.Length > 0)
                            .ToArray();
            if (names.Length > 0)
                return names[new Random().Next(names.Length)];
        }
        catch { /* fichier absent ou illisible → fallback */ }
        return "Joueur";
    }
}
