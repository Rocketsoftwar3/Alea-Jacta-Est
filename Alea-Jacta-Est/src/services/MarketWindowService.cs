using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class MarketWindowService
{
    private static readonly Vector2 CardThumbSize = new(36, 60);

    private readonly ICommandQueue _commands;

    public MarketWindowService(ICommandQueue commands)
    {
        _commands = commands;
    }

    public void Render(GameState state, ImGuiRenderer imGuiRenderer, ref bool isOpen)
    {
        bool isShopPhase = state.CurrentTurnPhase == TurnPhase.ShopPhase;

        // Only accessible during ShopPhase — auto-open and force modal
        if (!isShopPhase)
        {
            isOpen = false;
            return;
        }
        isOpen = true;

        var player = state.LocalPlayer;
        var market = player.Market;

        // Fullscreen modal with padding
        var io = ImGui.GetIO();
        const float modalPad = 40f;
        var modalSize = new Vector2(io.DisplaySize.X - modalPad * 2f, io.DisplaySize.Y - modalPad * 2f);
        ImGui.SetNextWindowSize(modalSize, ImGuiCond.Always);
        ImGui.SetNextWindowPos(new Vector2(modalPad, modalPad), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.95f);

        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize
                  | ImGuiWindowFlags.NoMove     | ImGuiWindowFlags.NoCollapse;

        ImGui.Begin("##MarketModal", flags);

        // Title bar with close button
        ImGui.TextColored(new Vector4(1f, 0.7f, 0.3f, 1f), "BOUTIQUE");
        ImGui.SameLine(modalSize.X - 200f);
        int localIdx = state.Players.IndexOf(player);
        if (Alea_Jacta_Est.Utils.UIHelper.DrawPixelButton("btn_close_shop", "Fermer et valider",
            new Vector2(180, 28), new Vector4(0.6f, 0.2f, 0.2f, 1f), false))
        {
            _commands.Enqueue(new EndShopPhaseCommand(localIdx));
        }
        ImGui.Separator();

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
                if (ImGui.IsItemHovered())
                    CardTooltip(imGuiRenderer, card);

                // Col 1 — name + description
                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4f);
                if (card is ArcanaCard ac)
                {
                    ImGui.TextUnformatted($"{ac.ArcanaNumber} — {ac.ArcanaName}");
                    ImGui.TextColored(ac.IsUpright
                        ? new Vector4(0.4f, 0.9f, 1f, 1f)
                        : new Vector4(1f, 0.5f, 0.3f, 1f),
                        ac.IsUpright ? "Endroit" : "Envers");
                    string desc = ac.IsUpright ? ac.DescriptionEndroit : ac.DescriptionEnvers;
                    if (!string.IsNullOrEmpty(desc))
                    {
                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + 200f);
                        ImGui.TextDisabled(desc);
                        ImGui.PopTextWrapPos();
                    }
                }
                else if (card is ValueCard vc)
                {
                    ImGui.TextUnformatted(vc.DisplayName);
                    ImGui.TextDisabled($"{vc.Suit}");
                }
                else
                {
                    string name = FormatTextureName(card.TextureRecto.Name);
                    ImGui.TextUnformatted(name);
                }
                if (card.Price != price)
                {
                    ImGui.TextDisabled($"(base : {card.Price})");
                }

                // Col 2 — price (green if affordable, red if not)
                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetTextLineHeight() * 0.5f));
                if (canAfford)
                    ImGui.TextColored(new Vector4(0.3f, 0.95f, 0.4f, 1f), $"{price}");
                else
                    ImGui.TextColored(new Vector4(0.95f, 0.3f, 0.3f, 1f), $"{price}");

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
                ("Pioche",           player.Decks[DeckType.MainDeck]),
                ("Défausse",         player.Decks[DeckType.DiscardDeck]),
                ("Pioche Arcanique", player.Decks[DeckType.SpecialDeck]),
                ("Défausse Arc.",    player.Decks[DeckType.ArcanaDiscardDeck]),
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
                    int sellIdx = state.Players.IndexOf(player);
                    _commands.Enqueue(new SellCardCommand(player, cardToSell, sellDeck, sellIdx));
                }

                ImGui.EndTable();
            }
        }

        ImGui.End();
    }

    private static void CardTooltip(ImGuiRenderer r, Card card)
    {
        ImGui.BeginTooltip();
        ImGui.Image(r.GetOrBindTexture(card.TextureRecto), new Vector2(108, 180));
        ImGui.Separator();
        if (card is ValueCard vc)
        {
            ImGui.TextUnformatted(vc.DisplayName);
            if (vc.IsFaceCard)
                ImGui.TextDisabled($"Multiplicateur: x{vc.Multiplier}");
            else
                ImGui.TextDisabled($"Valeur: {vc.DamageValue} d\u00e9g\u00e2ts");
            ImGui.TextDisabled($"Enseigne: {vc.Suit}");
        }
        else if (card is ArcanaCard ac)
        {
            ImGui.TextUnformatted($"{ac.ArcanaNumber} - {ac.ArcanaName}");
            ImGui.TextColored(ac.IsUpright
                ? new Vector4(0.4f, 0.9f, 1f, 1f)
                : new Vector4(1f, 0.5f, 0.3f, 1f),
                ac.IsUpright ? "Endroit" : "Envers");
            ImGui.Separator();
            string desc = ac.IsUpright ? ac.DescriptionEndroit : ac.DescriptionEnvers;
            if (!string.IsNullOrEmpty(desc))
            {
                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + 220f);
                ImGui.TextWrapped(desc);
                ImGui.PopTextWrapPos();
            }
            if (ac.Price > 0) ImGui.TextDisabled($"Prix: {ac.Price}");
        }
        ImGui.EndTooltip();
    }

    private static string FormatTextureName(string texturePath)
    {
        string file = Path.GetFileNameWithoutExtension(texturePath ?? "");
        return file.Replace("_", " ");
    }
}
