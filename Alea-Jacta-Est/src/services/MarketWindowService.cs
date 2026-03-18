using System.IO;
using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class MarketWindowService
{
    private static readonly Vector2 CardThumbSize = new(36, 60);

    private readonly CommandQueue _commands;

    public MarketWindowService(CommandQueue commands)
    {
        _commands = commands;
    }

    public void Render(GameState state, ImGuiRenderer imGuiRenderer, ref bool isOpen)
    {
        if (!isOpen) return;

        var player = state.LocalPlayer;
        var market = player.Market;

        ImGui.SetNextWindowSize(new Vector2(340, 380), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(250, 120), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(260, 200), new Vector2(500, 600));

        if (!ImGui.Begin("Marché", ref isOpen))
        {
            ImGui.End();
            return;
        }

        // ── Header ────────────────────────────────────────────────────────────
        ImGui.TextUnformatted($"Portefeuille : {player.Wallet} pièces");
        if (market.Discount > 0f)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"  (-{market.Discount * 100:F0}%%)");
        }
        ImGui.Separator();

        if (market.Deck.Cards.Count == 0)
        {
            ImGui.TextDisabled("Aucune carte disponible.");
            ImGui.End();
            return;
        }

        // ── Card table ────────────────────────────────────────────────────────
        var tableFlags = ImGuiTableFlags.Borders
                       | ImGuiTableFlags.RowBg
                       | ImGuiTableFlags.ScrollY
                       | ImGuiTableFlags.SizingFixedFit;

        float tableHeight = ImGui.GetContentRegionAvail().Y;

        if (ImGui.BeginTable("market_table", 4, tableFlags, new Vector2(0, tableHeight)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("",      ImGuiTableColumnFlags.WidthFixed,   CardThumbSize.X + 4);
            ImGui.TableSetupColumn("Carte", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Prix",  ImGuiTableColumnFlags.WidthFixed,   44);
            ImGui.TableSetupColumn("",      ImGuiTableColumnFlags.WidthFixed,   68);
            ImGui.TableHeadersRow();

            Card? cardToBuy = null;
            int buyPrice = 0;

            for (int i = 0; i < market.Deck.Cards.Count; i++)
            {
                var card = market.Deck.Cards[i];
                int price = market.GetDiscountedPrice(card.Price);
                bool canAfford = player.Wallet >= price;

                ImGui.TableNextRow(ImGuiTableRowFlags.None, CardThumbSize.Y + 4);

                // Col 0 — thumbnail
                ImGui.TableNextColumn();
                var texId = imGuiRenderer.GetOrBindTexture(card.TextureRecto);
                ImGui.Image(texId, CardThumbSize);

                // Col 1 — name
                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetTextLineHeight() * 0.5f));
                string name = FormatTextureName(card.TextureRecto.Name);
                ImGui.TextUnformatted(name);
                if (card.Price != price)
                {
                    ImGui.TextDisabled($"(base : {card.Price})");
                }

                // Col 2 — price
                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetTextLineHeight() * 0.5f));
                if (canAfford)
                    ImGui.TextUnformatted($"{price}");
                else
                    ImGui.TextDisabled($"{price}");

                // Col 3 — buy button
                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetFrameHeight() * 0.5f));
                if (!canAfford) ImGui.BeginDisabled();
                if (ImGui.Button($"Acheter##{i}"))
                {
                    cardToBuy = card;
                    buyPrice = price;
                }
                if (!canAfford) ImGui.EndDisabled();
            }

            // Enqueue command instead of mutating state directly
            if (cardToBuy != null)
            {
                _commands.Enqueue(new BuyCardCommand(player, cardToBuy, buyPrice));
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }

    private static string FormatTextureName(string texturePath)
    {
        string file = Path.GetFileNameWithoutExtension(texturePath ?? "");
        return file.Replace("_", " ");
    }
}
