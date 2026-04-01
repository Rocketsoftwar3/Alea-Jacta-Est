using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class HandWindowService
{
    private static readonly Vector2 CardThumbSize = new(36, 60);

    private readonly CommandQueue _commands;
    private readonly EffectManager _effectManager;

    public HandWindowService(CommandQueue commands, EffectManager effectManager)
    {
        _commands = commands;
        _effectManager = effectManager;
    }

    public void Render(GameState state, ImGuiRenderer imGuiRenderer)
    {
        var player = state.LocalPlayer;
        bool isPlayPhase = state.CurrentTurnPhase == TurnPhase.PlayPhase;

        string phaseLabel = state.CurrentTurnPhase switch
        {
            TurnPhase.DrawPhase       => "Pioche",
            TurnPhase.PlayPhase       => "Jeu",
            TurnPhase.ValidatePhase   => "Validation",
            TurnPhase.ResolutionPhase => "Résolution",
            TurnPhase.CleanupPhase    => "Nettoyage",
            TurnPhase.ShopPhase       => "Boutique",
            _                         => "?"
        };

        ImGui.SetNextWindowPos(new Vector2(550, 10), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(420, 220), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(300, 120), new Vector2(700, 500));
        ImGui.Begin($"Main — {phaseLabel} (Tour {state.CurrentTurn})");

        // ── Value cards in hand ───────────────────────────────────────────────
        var handDeck = player.Decks[DeckType.HandDeck];
        if (handDeck.Cards.Count == 0 && player.Decks[DeckType.ArcanaHandDeck].Cards.Count == 0)
        {
            ImGui.TextDisabled("Aucune carte en main.");
            ImGui.End();
            return;
        }

        var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
                       | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit;
        float tableHeight = ImGui.GetContentRegionAvail().Y;

        if (ImGui.BeginTable("hand_table", 4, tableFlags, new Vector2(0, tableHeight)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("",       ImGuiTableColumnFlags.WidthFixed,   CardThumbSize.X + 4);
            ImGui.TableSetupColumn("Carte",  ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Valeur", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("",       ImGuiTableColumnFlags.WidthFixed,   80);
            ImGui.TableHeadersRow();

            // Value cards
            for (int i = 0; i < handDeck.Cards.Count; i++)
            {
                var card = handDeck.Cards[i];
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
                if (ImGui.Button($"Jouer##{i}") && card is ValueCard playable)
                    _commands.Enqueue(new PlayCardCommand(player, playable));
                if (!isPlayPhase) ImGui.EndDisabled();
            }

            // Arcana card
            var arcanaHand = player.Decks[DeckType.ArcanaHandDeck];
            for (int i = 0; i < arcanaHand.Cards.Count; i++)
            {
                var card = arcanaHand.Cards[i];
                ImGui.TableNextRow(ImGuiTableRowFlags.None, CardThumbSize.Y + 4);

                ImGui.TableNextColumn();
                var texId = imGuiRenderer.GetOrBindTexture(card.TextureRecto);
                ImGui.Image(texId, CardThumbSize);

                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetTextLineHeight() * 0.5f));
                if (card is ArcanaCard ac)
                {
                    ImGui.TextUnformatted($"{ac.ArcanaNumber} — {ac.ArcanaName}");
                    ImGui.TextDisabled(ac.IsUpright ? "Endroit" : "Envers");
                }
                else
                {
                    ImGui.TextUnformatted("Arcane");
                }

                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetTextLineHeight() * 0.5f));
                ImGui.TextDisabled("Arc.");

                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CardThumbSize.Y * 0.5f - ImGui.GetFrameHeight() * 0.5f));
                int localIdx = state.Players.IndexOf(player);
                bool arcanaLimitReached = state.TurnStates.TryGetValue(localIdx, out var lts)
                    && lts.ArcanasPlayedThisTurn >= lts.MaxArcanasPerTurn;
                bool arcanaDisabled = !isPlayPhase || arcanaLimitReached;
                if (arcanaDisabled) ImGui.BeginDisabled();
                if (ImGui.Button($"Activer##a{i}") && card is ArcanaCard activatable)
                    _commands.Enqueue(new ActivateArcanaCommand(player, activatable, null, _effectManager));
                if (arcanaDisabled) ImGui.EndDisabled();
                if (arcanaLimitReached && isPlayPhase)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled("(limite)");
                }
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }
}
