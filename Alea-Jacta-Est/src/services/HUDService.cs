using System;
using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class HUDService
{
    private readonly CommandQueue _commands;
    private readonly EffectManager _effectManager;
    private readonly DamageCalculationService _damageCalc;

    public HUDService(CommandQueue commands, EffectManager effectManager, DamageCalculationService damageCalc)
    {
        _commands = commands;
        _effectManager = effectManager;
        _damageCalc = damageCalc;
    }

    public void Render(GameState state, ref bool marketOpen)
    {
        var io = ImGui.GetIO();
        float hudWidth = 270f;

        ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(hudWidth, io.DisplaySize.Y), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.88f);

        var flags = ImGuiWindowFlags.NoMove
                  | ImGuiWindowFlags.NoResize
                  | ImGuiWindowFlags.NoTitleBar
                  | ImGuiWindowFlags.NoBringToFrontOnFocus
                  | ImGuiWindowFlags.NoScrollbar
                  | ImGuiWindowFlags.NoScrollWithMouse;

        ImGui.Begin("##HUD", flags);

        RenderHeader(state);
        ImGui.Separator();
        RenderPlayers(state);
        ImGui.Separator();
        RenderDamagePreview(state);
        RenderActionButtons(state, ref marketOpen);

        if (state.PendingActivation != null)
        {
            ImGui.Separator();
            RenderTargetSelection(state);
        }

        ImGui.End();
    }

    private static void RenderHeader(GameState state)
    {
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.2f, 1f), $"TOUR {state.CurrentTurn}");

        ImGui.Spacing();

        TurnPhase[] pipeline = {
            TurnPhase.DrawPhase, TurnPhase.PlayPhase, TurnPhase.ResolutionPhase,
            TurnPhase.CleanupPhase, TurnPhase.ShopPhase
        };
        string[] labels = { "Pioche", "Jeu", "Résol.", "Nett.", "Boutique" };
        Vector4[] colors = {
            new(0.5f, 0.8f, 1f, 1f),
            new(0.3f, 1f, 0.3f, 1f),
            new(1f, 0.4f, 0.4f, 1f),
            new(0.8f, 0.5f, 1f, 1f),
            new(1f, 0.7f, 0.3f, 1f),
        };

        for (int i = 0; i < pipeline.Length; i++)
        {
            bool isCurrent = state.CurrentTurnPhase == pipeline[i];
            if (isCurrent)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, colors[i]);
                ImGui.TextUnformatted(labels[i]);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.TextDisabled(labels[i]);
            }
            if (i < pipeline.Length - 1)
            {
                ImGui.SameLine();
                ImGui.TextDisabled(" ›");
                ImGui.SameLine();
            }
        }

        ImGui.Spacing();
    }

    private void RenderPlayers(GameState state)
    {
        foreach (var player in state.Players)
        {
            bool isLocal = player.IsLocalPlayer;

            if (isLocal)
                ImGui.TextColored(new Vector4(0.4f, 0.9f, 1f, 1f), $"{player.Name} (toi)");
            else
                ImGui.TextUnformatted(player.Name);

            // HP progress bar
            float hpFraction = Math.Clamp(player.Health / 100f, 0f, 1f);
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, HpColor(hpFraction));
            ImGui.ProgressBar(hpFraction, new Vector2(-1, 14), $"♥ {player.Health} / 100");
            ImGui.PopStyleColor();

            if (isLocal)
            {
                ImGui.TextUnformatted($"💰 {player.Wallet} pièces");
                if (player.Market.Discount > 0f)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled($"  (-{player.Market.Discount * 100:F0}%%)");
                }

                int mainCount    = player.Decks.TryGetValue("MainDeck",    out var md) ? md.Cards.Count : 0;
                int discardCount = player.Decks.TryGetValue("DiscardDeck", out var dd) ? dd.Cards.Count : 0;
                int arcanaCount  = player.Decks.TryGetValue("ArcanaHandDeck", out var ah) ? ah.Cards.Count : 0;
                ImGui.TextDisabled($"Pioche: {mainCount}  Déf: {discardCount}  Arc: {arcanaCount}");

                var effects = _effectManager.GetActiveEffectSummary();
                if (effects.Count > 0)
                {
                    ImGui.TextColored(new Vector4(0.9f, 0.6f, 1f, 1f), "Effets actifs :");
                    foreach (var (name, turns) in effects)
                        ImGui.TextDisabled($"  • {name} ({turns} tour(s))");
                }
            }
            else
            {
                int mainCount = player.Decks.TryGetValue("MainDeck", out var md) ? md.Cards.Count : 0;
                ImGui.TextDisabled($"Pioche: {mainCount}");

                // Impératrice: show opponent hand if visible
                bool canSeeHands = state.TurnStates.TryGetValue(0, out var localTs) && localTs.CanSeeOpponentHands;
                if (canSeeHands && player.Decks.TryGetValue("HandDeck", out var oppHand) && oppHand.Cards.Count > 0)
                {
                    ImGui.TextColored(new Vector4(0.4f, 1f, 0.8f, 1f), "Main visible (Impératrice) :");
                    foreach (var c in oppHand.Cards)
                    {
                        string cardLabel = c is ValueCard ovc ? ovc.DisplayName
                            : c is ArcanaCard oac ? oac.ArcanaName
                            : c.TextureRecto.Name;
                        ImGui.TextDisabled($"  {cardLabel}");
                    }
                }
            }

            ImGui.Spacing();
        }
    }

    private void RenderDamagePreview(GameState state)
    {
        if (state.CurrentTurnPhase != TurnPhase.PlayPhase) return;

        var player = state.LocalPlayer;
        if (!player.Decks.TryGetValue("BoardDeck0", out var board) || board.Cards.Count == 0) return;

        int localIdx = state.Players.IndexOf(player);
        float boost = state.TurnStates.TryGetValue(localIdx, out var ts) && ts.MultiplierDoubled ? 2f : 1f;
        int dmg = _damageCalc.CalculateDamage(board.Cards, boost);

        ImGui.TextColored(new Vector4(1f, 0.4f, 0.3f, 1f), $"⚔  Dégâts potentiels : {dmg}");
        ImGui.Separator();
    }

    private void RenderActionButtons(GameState state, ref bool marketOpen)
    {
        int localIdx = state.Players.IndexOf(state.LocalPlayer);
        bool isPlayPhase = state.CurrentTurnPhase == TurnPhase.PlayPhase;
        bool alreadyValidated = state.TurnStates.TryGetValue(localIdx, out var ts) && ts.HasValidated;
        bool isShopPhase = state.CurrentTurnPhase == TurnPhase.ShopPhase;

        ImGui.Spacing();

        // Validate turn button — green, PlayPhase only
        bool validateDisabled = !isPlayPhase || alreadyValidated;
        if (validateDisabled) ImGui.BeginDisabled();
        ImGui.PushStyleColor(ImGuiCol.Button,       new Vector4(0.15f, 0.60f, 0.25f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.78f, 0.35f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.10f, 0.45f, 0.18f, 1f));
        if (ImGui.Button("✓  Valider le tour", new Vector2(-1, 36)))
            _commands.Enqueue(new ValidateTurnCommand(localIdx));
        ImGui.PopStyleColor(3);
        if (validateDisabled) ImGui.EndDisabled();

        // End shop button — orange, ShopPhase only
        if (!isShopPhase) ImGui.BeginDisabled();
        ImGui.PushStyleColor(ImGuiCol.Button,       new Vector4(0.65f, 0.38f, 0.08f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.82f, 0.52f, 0.15f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.50f, 0.28f, 0.05f, 1f));
        if (ImGui.Button("→  Fin de boutique", new Vector2(-1, 30)))
            _commands.Enqueue(new EndShopPhaseCommand());
        ImGui.PopStyleColor(3);
        if (!isShopPhase) ImGui.EndDisabled();

        // Market toggle
        ImGui.Spacing();
        string marketLabel = marketOpen ? "Fermer le Marché" : "🏪  Ouvrir le Marché";
        if (ImGui.Button(marketLabel, new Vector2(-1, 28)))
            marketOpen = !marketOpen;
    }

    private void RenderTargetSelection(GameState state)
    {
        var pending = state.PendingActivation!;

        ImGui.TextColored(new Vector4(1f, 0.9f, 0.2f, 1f), "CHOISIR UNE CIBLE");
        ImGui.TextDisabled($"{pending.Card.ArcanaName} ({(pending.Card.IsUpright ? "Endroit" : "Envers")})");
        ImGui.Spacing();

        for (int i = 0; i < state.Players.Count; i++)
        {
            var target = state.Players[i];
            float f = Math.Clamp(target.Health / 100f, 0f, 1f);

            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, HpColor(f));
            ImGui.ProgressBar(f, new Vector2(60, 10), "");
            ImGui.PopStyleColor();

            ImGui.SameLine();
            string suffix = target == pending.Activator ? " (toi)" : "";
            if (ImGui.Button($"{target.Name}{suffix}  {target.Health}PV##{i}", new Vector2(-1, 0)))
                _commands.Enqueue(new ActivateArcanaWithTargetCommand(
                    pending.Activator, pending.Card, target, _effectManager));
        }

        ImGui.Spacing();
        if (ImGui.Button("Annuler", new Vector2(-1, 0)))
            state.PendingActivation = null;
    }

    private static Vector4 HpColor(float f) => f > 0.6f
        ? new(0.2f, 0.8f, 0.3f, 1f)
        : f > 0.3f
            ? new(1f, 0.6f, 0.1f, 1f)
            : new(0.9f, 0.2f, 0.2f, 1f);
}
