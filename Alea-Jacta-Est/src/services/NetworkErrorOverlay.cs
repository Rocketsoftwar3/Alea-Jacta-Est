using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

/// <summary>
/// Shows a modal overlay when the client is disconnected from the host mid-game.
/// Subscribes to NetworkDisconnected and PlayerDisconnected events.
/// </summary>
public class NetworkErrorOverlay
{
    private readonly ICommandQueue _commands;
    private string? _errorMessage;

    public NetworkErrorOverlay(ICommandQueue commands, EventBus events)
    {
        _commands = commands;
        events.Subscribe<NetworkDisconnected>(e =>
            _errorMessage = $"Déconnecté du serveur\n{e.Reason}");
        events.Subscribe<PlayerDisconnected>(e =>
            _errorMessage = $"Le joueur {e.PlayerName} s'est déconnecté.");
    }

    public void Render(GameState state)
    {
        if (_errorMessage == null) return;

        var io = ImGui.GetIO();
        var dl = ImGui.GetBackgroundDrawList();
        dl.AddRectFilled(Vector2.Zero, io.DisplaySize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.55f)));

        var windowSize = new Vector2(380, 160);
        var center = new Vector2(
            (io.DisplaySize.X - windowSize.X) * 0.5f,
            (io.DisplaySize.Y - windowSize.Y) * 0.5f);

        ImGui.SetNextWindowPos(center, ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);

        ImGui.Begin("##NetError",
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar);

        ImGui.SetCursorPosX((windowSize.X - ImGui.CalcTextSize("Erreur réseau").X) * 0.5f);
        ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "Erreur réseau");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextWrapped(_errorMessage);
        ImGui.Spacing();

        float btnW = 140f;
        ImGui.SetCursorPosX((windowSize.X - btnW) * 0.5f);
        if (ImGui.Button("Retour au menu", new Vector2(btnW, 0)))
        {
            _errorMessage = null;
            _commands.Enqueue(new ReturnToMenuCommand());
        }

        ImGui.End();
    }
}
