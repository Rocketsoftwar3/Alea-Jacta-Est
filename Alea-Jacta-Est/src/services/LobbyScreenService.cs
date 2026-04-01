using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Networking;

namespace Alea_Jacta_Est.Services;

/// <summary>
/// Lobby waiting room screen. Shown when LobbyState == Hosting or InLobby.
/// </summary>
public class LobbyScreenService
{
    private readonly LobbyManager   _lobby;
    private readonly NetworkManager _network;

    private readonly List<string> _logs = new();

    private static readonly Vector4 ColorReady    = new(0.3f, 1f, 0.4f, 1f);
    private static readonly Vector4 ColorNotReady = new(0.6f, 0.6f, 0.6f, 1f);
    private static readonly Vector4 ColorError    = new(1f, 0.35f, 0.35f, 1f);
    private static readonly Vector4 ColorHost     = new(1f, 0.85f, 0.4f, 1f);
    private static readonly Vector4 ColorLog      = new(0.75f, 0.85f, 1f, 1f);

    public LobbyScreenService(LobbyManager lobby, NetworkManager network)
    {
        _lobby   = lobby;
        _network = network;

        // Subscribe to lobby events to build the log
        _lobby.OnLog += AddLog;
    }

    public void AddLog(string message) => _logs.Add(message);

    public void Render(GameState state)
    {
        var vp      = ImGui.GetMainViewport();
        var center  = new Vector2(vp.WorkSize.X / 2f, vp.WorkSize.Y / 2f);
        var winSize = new Vector2(500, 520);

        ImGui.SetNextWindowPos(center, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(winSize, ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.94f);

        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize
                  | ImGuiWindowFlags.NoMove    | ImGuiWindowFlags.NoScrollbar;

        if (!ImGui.Begin("##Lobby", flags)) { ImGui.End(); return; }

        // ── Titre ─────────────────────────────────────────────────────────────
        var titleStr = _network.IsHost ? "Salle d'attente  (Hôte)" : "Salle d'attente";
        ImGui.SetCursorPosX((winSize.X - ImGui.CalcTextSize(titleStr).X) / 2f);
        ImGui.TextColored(ColorHost, titleStr);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ── Erreur ────────────────────────────────────────────────────────────
        if (_lobby.ErrorMessage != null)
        {
            ImGui.TextColored(ColorError, _lobby.ErrorMessage);
            ImGui.Spacing();
        }

        // ── IP pour l'hôte ────────────────────────────────────────────────────
        if (_network.IsHost)
        {
            ImGui.TextColored(ColorNotReady,
                $"Port : {NetworkManager.GamePort}  —  Partagez votre IP aux autres joueurs");
            ImGui.Spacing();
        }

        // ── Liste des joueurs ─────────────────────────────────────────────────
        ImGui.Text($"Joueurs : {_lobby.Players.Count} / {_lobby.MaxPlayers}");
        ImGui.Spacing();

        if (ImGui.BeginTable("##players", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Nom",    ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Statut", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableHeadersRow();

            foreach (var p in _lobby.Players.OrderBy(x => x.PlayerId))
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                if (p.IsHost) ImGui.TextColored(ColorHost, $"[H] {p.Name}");
                else          ImGui.TextUnformatted(p.Name);

                ImGui.TableSetColumnIndex(1);
                if (p.IsReady) ImGui.TextColored(ColorReady,    "Pret");
                else           ImGui.TextColored(ColorNotReady, "En attente");

                ImGui.TableSetColumnIndex(2);
                if (_network.IsHost && !p.IsHost)
                {
                    if (ImGui.SmallButton($"Exclure##{p.PlayerId}"))
                        _lobby.KickPlayer(p.PlayerId);
                }
            }

            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ── Boutons ───────────────────────────────────────────────────────────
        if (_network.IsHost)
        {
            bool allReady = _lobby.Players.All(p => p.IsReady);
            bool canStart = allReady && _lobby.Players.Count >= 2;

            if (!canStart) ImGui.BeginDisabled();
            if (ImGui.Button("Lancer la partie", new Vector2(180, 36)))
                _lobby.StartGame();
            if (!canStart) ImGui.EndDisabled();

            if (!allReady)
            {
                ImGui.SameLine();
                ImGui.TextColored(ColorNotReady, "En attente de tous les joueurs...");
            }
        }
        else
        {
            // Bouton basé sur l'état RÉEL du joueur local (corrigé)
            var self = _lobby.LocalPlayer;
            if (self != null)
            {
                var label = self.IsReady ? "Pas prêt" : "Prêt";
                var color = self.IsReady
                    ? new Vector4(0.65f, 0.38f, 0.08f, 1f)
                    : new Vector4(0.15f, 0.60f, 0.25f, 1f);
                ImGui.PushStyleColor(ImGuiCol.Button, color);
                if (ImGui.Button(label, new Vector2(120, 36)))
                    _lobby.ToggleReady();
                ImGui.PopStyleColor();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Quitter", new Vector2(80, 36)))
            _lobby.LeaveLobby();

        // ── Logs ──────────────────────────────────────────────────────────────
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(ColorLog, "Journal :");
        ImGui.Spacing();

        float logH = 100f;
        ImGui.BeginChild("##logs", new Vector2(-1, logH), ImGuiChildFlags.Borders,
            ImGuiWindowFlags.HorizontalScrollbar);

        foreach (var line in _logs)
            ImGui.TextUnformatted(line);

        // Auto-scroll vers le bas
        if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            ImGui.SetScrollHereY(1.0f);

        ImGui.EndChild();

        ImGui.End();
    }
}
