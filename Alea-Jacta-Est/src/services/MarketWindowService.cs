using System.Collections.Generic;
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

        // ── Phase indicator ───────────────────────────────────────────────────
        bool isShopPhase = state.CurrentTurnPhase == TurnPhase.ShopPhase;
        ImGui.TextColored(isShopPhase
            ? new Vector4(1f, 0.7f, 0.3f, 1f)
            : new Vector4(0.7f, 0.7f, 0.7f, 1f),
            isShopPhase ? "Phase Boutique — Achats/Ventes disponibles" : "Hors boutique — Achat uniquement");

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

        // ── Sell section (ShopPhase only) ────────────────────────────────────
        if (isShopPhase)
        {
            ImGui.Separator();
            int localIndex = state.Players.IndexOf(player);
            bool hasSold = state.TurnStates.TryGetValue(localIndex, out var turnState) && turnState.HasSoldThisRound;

            ImGui.TextUnformatted("Vendre une carte :");
            if (hasSold) ImGui.TextDisabled("(déjà vendu ce tour)");

            var sellableSources = new List<(string Label, Deck Deck)>
            {
                ("Pioche",           player.Decks["MainDeck"]),
                ("Défausse",         player.Decks["DiscardDeck"]),
                ("Pioche Arcanique", player.Decks["SpecialDeck"]),
                ("Défausse Arc.",    player.Decks["ArcanaDiscardDeck"]),
            };

            var sellFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
                          | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit;

            if (ImGui.BeginTable("sell_table", 3, sellFlags, new Vector2(0, 180)))
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("",       ImGuiTableColumnFlags.WidthFixed,   CardThumbSize.X + 4);
                ImGui.TableSetupColumn("Carte",  ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("",       ImGuiTableColumnFlags.WidthFixed, 68);
                ImGui.TableHeadersRow();

                Card? cardToSell = null;
                Deck? sellDeck = null;

                foreach (var (sourceLabel, deck) in sellableSources)
                {
                    foreach (var card in deck.Cards)
                    {
                        ImGui.TableNextRow(ImGuiTableRowFlags.None, CardThumbSize.Y + 4);

                        ImGui.TableNextColumn();
                        var texId = imGuiRenderer.GetOrBindTexture(card.TextureRecto);
                        ImGui.Image(texId, CardThumbSize);

                        ImGui.TableNextColumn();
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetTextLineHeight() * 0.5f));
                        string cardName = card is ValueCard vc ? vc.DisplayName
                            : card is ArcanaCard ac ? ac.ArcanaName
                            : FormatTextureName(card.TextureRecto.Name);
                        ImGui.TextUnformatted(cardName);
                        ImGui.TextDisabled(sourceLabel);

                        ImGui.TableNextColumn();
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetFrameHeight() * 0.5f));

                        if (hasSold || card.Price == 0) ImGui.BeginDisabled();
                        if (ImGui.Button($"Vendre {card.Price}##s{card.GetHashCode()}"))
                        {
                            cardToSell = card;
                            sellDeck = deck;
                        }
                        if (hasSold || card.Price == 0) ImGui.EndDisabled();
                    }
                }

                if (cardToSell != null && sellDeck != null)
                {
                    int localIdx = state.Players.IndexOf(player);
                    _commands.Enqueue(new SellCardCommand(player, cardToSell, sellDeck, localIdx));
                }

                ImGui.EndTable();
            }
        }

        ImGui.End();
    }

    private static string FormatTextureName(string texturePath)
    {
        string file = Path.GetFileNameWithoutExtension(texturePath ?? "");
        return file.Replace("_", " ");
    }
}
