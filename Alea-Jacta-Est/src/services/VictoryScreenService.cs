using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class VictoryScreenService
{
    private readonly CommandQueue _commands;
    private readonly DemoDataService _demoData;

    public VictoryScreenService(CommandQueue commands, DemoDataService demoData)
    {
        _commands = commands;
        _demoData = demoData;
    }

    public void Render(GameState state)
    {
        if (state.Phase != GamePhase.Finished) return;

        var io = ImGui.GetIO();

        // Dim the whole screen behind the results window
        var dl = ImGui.GetBackgroundDrawList();
        dl.AddRectFilled(Vector2.Zero, io.DisplaySize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.65f)));

        var windowSize = new Vector2(420, 300);
        var center = new Vector2((io.DisplaySize.X - windowSize.X) * 0.5f, (io.DisplaySize.Y - windowSize.Y) * 0.5f);

        ImGui.SetNextWindowPos(center, ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);

        ImGui.Begin("Resultats",
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar);

        ImGui.SetCursorPosX((windowSize.X - ImGui.CalcTextSize("=== RESULTATS ===").X) * 0.5f);
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.1f, 1f), "=== RESULTATS ===");
        ImGui.Separator();
        ImGui.Spacing();

        string[] medals = { "1er", "2eme", "3eme" };
        Vector4[] colors = {
            new(1f, 0.84f, 0f, 1f),   // gold
            new(0.75f, 0.75f, 0.75f, 1f), // silver
            new(0.8f, 0.5f, 0.2f, 1f),   // bronze
        };

        for (int i = 0; i < state.FinalScores.Count; i++)
        {
            var (player, damage) = state.FinalScores[i];
            string rank = i < medals.Length ? medals[i] : "   ";
            var color = i < colors.Length ? colors[i] : new Vector4(0.8f, 0.8f, 0.8f, 1f);

            ImGui.TextColored(color, rank);
            ImGui.SameLine();
            string alive = player.Health > 0 ? "" : " (éliminé)";
            ImGui.TextUnformatted($"  {player.Name}{alive}  -  {damage} degats infliges");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        float btnWidth = 160f;
        ImGui.SetCursorPosX((windowSize.X - btnWidth) * 0.5f);
        if (ImGui.Button("Nouvelle Partie", new Vector2(btnWidth, 0)))
            _commands.Enqueue(new NewGameCommand(_demoData));

        ImGui.End();
    }
}
