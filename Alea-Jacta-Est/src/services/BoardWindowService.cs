using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class BoardWindowService
{
    private static readonly Vector2 CardThumbSize = new(36, 60);

    private readonly CommandQueue _commands;

    public BoardWindowService(CommandQueue commands)
    {
        _commands = commands;
    }

    public void Render(GameState state, ImGuiRenderer imGuiRenderer)
    {
        var player = state.LocalPlayer;
        var board = player.Decks["BoardDeck0"];
        bool isPlayPhase = state.CurrentTurnPhase == TurnPhase.PlayPhase;

        ImGui.SetNextWindowPos(new Vector2(550, 240), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(420, 180), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(300, 100), new Vector2(700, 400));
        ImGui.Begin($"Plateau — {board.Cards.Count} carte(s)");

        if (board.Cards.Count == 0)
        {
            ImGui.TextDisabled("Aucune carte posée.");
            ImGui.End();
            return;
        }

        var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
                       | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit;
        float tableHeight = ImGui.GetContentRegionAvail().Y;

        if (ImGui.BeginTable("board_table", 4, tableFlags, new Vector2(0, tableHeight)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("",       ImGuiTableColumnFlags.WidthFixed,   CardThumbSize.X + 4);
            ImGui.TableSetupColumn("Carte",  ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Valeur", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("",       ImGuiTableColumnFlags.WidthFixed,   90);
            ImGui.TableHeadersRow();

            for (int i = board.Cards.Count - 1; i >= 0; i--)
            {
                var card = board.Cards[i];
                ImGui.TableNextRow(ImGuiTableRowFlags.None, CardThumbSize.Y + 4);

                ImGui.TableNextColumn();
                var texId = imGuiRenderer.GetOrBindTexture(card.TextureRecto);
                ImGui.Image(texId, CardThumbSize);

                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetTextLineHeight() * 0.5f));
                if (card is ValueCard vc)
                {
                    ImGui.TextUnformatted(vc.DisplayName);
                    ImGui.TextDisabled($"{vc.Suit}");
                }
                else
                {
                    ImGui.TextUnformatted(card.TextureRecto.Name);
                }

                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetTextLineHeight() * 0.5f));
                if (card is ValueCard vv)
                    ImGui.TextUnformatted(vv.IsFaceCard ? $"x{vv.Multiplier}" : $"{vv.DamageValue}");
                else
                    ImGui.TextDisabled("-");

                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetFrameHeight() * 0.5f));
                if (!isPlayPhase) ImGui.BeginDisabled();
                if (ImGui.Button($"Reprendre##{i}") && card is ValueCard takeable)
                    _commands.Enqueue(new TakeBackCardCommand(player, takeable));
                if (!isPlayPhase) ImGui.EndDisabled();
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }
}
