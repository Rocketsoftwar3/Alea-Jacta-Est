using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

/// <summary>
/// Full-screen transparent ImGui game table.
/// Layout: top banner (timeline + gold) -> opponent zones -> board -> local player zone (hand fan + piles + button).
/// </summary>
public class GameTableService
{
    // Card sizes (px)
    private static readonly Vector2 CardFan     = new(76,  126);  // local hand fan
    private static readonly Vector2 CardBoard   = new(66,  110);  // board played cards
    private static readonly Vector2 CardOpp     = new(44,   73);  // opponent card in fan
    private static readonly Vector2 CardPile    = new(44,   73);  // local deck piles
    private static readonly Vector2 CardOppBrd  = new(32,   53);  // opponent board cards

    // Fan parameters
    private const float FanRadius    = 200f;
    private const float FanMaxSpread = 70f;   // degrees
    private const float FanH         = 220f;  // reserved height for the hand fan

    private readonly ICommandQueue            _commands;
    private readonly EffectManager            _effectManager;
    private readonly DamageCalculationService _damageCalc;

    private TurnPhase _lastPhase   = TurnPhase.DrawPhase;
    private nint      _goldCoinId  = IntPtr.Zero;

    public GameTableService(ICommandQueue commands, EffectManager effectManager, DamageCalculationService damageCalc)
    {
        _commands      = commands;
        _effectManager = effectManager;
        _damageCalc    = damageCalc;
    }

    // ────────────────────────────────────────────────────────────────
    // Main entry point
    // ────────────────────────────────────────────────────────────────

    public void Render(GameState state, GraphicsResources gfx, ImGuiRenderer r, ref bool marketOpen)
    {
        // Bind goldcoin texture once
        if (_goldCoinId == IntPtr.Zero)
            _goldCoinId = r.GetOrBindTexture(gfx.GoldCoin);

        // Auto-open market when ShopPhase begins
        if (state.CurrentTurnPhase == TurnPhase.ShopPhase && _lastPhase != TurnPhase.ShopPhase)
            marketOpen = true;
        _lastPhase = state.CurrentTurnPhase;

        var io = ImGui.GetIO();
        float W = io.DisplaySize.X;
        float H = io.DisplaySize.Y;

        ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(W, H), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0f);

        var wFlags = ImGuiWindowFlags.NoTitleBar   | ImGuiWindowFlags.NoMove
                   | ImGuiWindowFlags.NoResize     | ImGuiWindowFlags.NoScrollbar
                   | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBringToFrontOnFocus
                   | ImGuiWindowFlags.NoNav;

        ImGui.Begin("##Table", wFlags);

        const float pad     = 6f;
        const float bannerH = 30f;
        float remaining     = H - bannerH;
        float oppH          = remaining * 0.24f;
        float boardH        = remaining * 0.18f;
        float localH        = remaining - oppH - boardH - pad * 3f;

        var player    = state.LocalPlayer;
        var opponents = state.Players.Where(p => !p.IsLocalPlayer).ToList();

        // ── 1. Top banner ────────────────────────────────────────────
        ImGui.SetCursorPos(Vector2.Zero);
        RenderTopBanner(state, W, bannerH);

        // ── 2. Opponent zones ────────────────────────────────────────
        ImGui.SetCursorPos(new Vector2(pad, bannerH + pad));
        RenderOpponents(state, opponents, r, W - pad * 2f, oppH);

        // ── 3. Board zone ────────────────────────────────────────────
        ImGui.SetCursorPos(new Vector2(pad, bannerH + oppH + pad * 2f));
        RenderBoard(state, player, r, W - pad * 2f, boardH);

        // ── 4. Local player zone ─────────────────────────────────────
        ImGui.SetCursorPos(new Vector2(pad, bannerH + oppH + boardH + pad * 3f));
        RenderLocal(state, player, r, W - pad * 2f, localH, ref marketOpen);

        ImGui.End();
    }

    // ────────────────────────────────────────────────────────────────
    // Top banner — turn + phase pipeline + gold (top-right of window)
    // ────────────────────────────────────────────────────────────────

    private void RenderTopBanner(GameState state, float W, float bannerH)
    {
        var dl   = ImGui.GetWindowDrawList();
        var wPos = ImGui.GetWindowPos();
        dl.AddRectFilled(wPos, new Vector2(wPos.X + W, wPos.Y + bannerH),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.6f)));

        ImGui.SetCursorPos(new Vector2(8f, 7f));
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.2f, 1f), $"Tour {state.CurrentTurn}");
        ImGui.SameLine(0, 20);

        TurnPhase[] phases = { TurnPhase.DrawPhase, TurnPhase.PlayPhase, TurnPhase.ResolutionPhase, TurnPhase.CleanupPhase, TurnPhase.ShopPhase };
        string[]    labels = { "Pioche", "Jeu", "Resolution", "Nettoyage", "Boutique" };
        Vector4[]   colors = {
            new(0.5f, 0.8f, 1f, 1f), new(0.3f, 1f, 0.3f, 1f),
            new(1f, 0.4f, 0.4f, 1f), new(0.8f, 0.5f, 1f, 1f), new(1f, 0.7f, 0.3f, 1f)
        };

        for (int i = 0; i < phases.Length; i++)
        {
            if (state.CurrentTurnPhase == phases[i])
            {
                ImGui.PushStyleColor(ImGuiCol.Text, colors[i]);
                ImGui.TextUnformatted(labels[i]);
                ImGui.PopStyleColor();
            }
            else
                ImGui.TextDisabled(labels[i]);

            if (i < phases.Length - 1) { ImGui.SameLine(0, 4); ImGui.TextDisabled(">"); ImGui.SameLine(0, 4); }
        }

        // ── Gold: coin icon + amount, top-right of window ────────────
        string walletStr = $"{state.LocalPlayer.Wallet}";
        float textW  = ImGui.CalcTextSize(walletStr).X;
        float goldX  = W - textW - 20f - 8f - 40f; // Shifted left to make room for mute button
        ImGui.SetCursorPos(new Vector2(goldX, 7f));
        ImGui.Image(_goldCoinId, new Vector2(16, 16));
        ImGui.SameLine(0, 4);
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.2f, 1f), walletStr);

        // ── Mute Button (Pixel Art Speaker) ──────────────────────────
        float muteX = W - 32f - 8f; 
        Alea_Jacta_Est.Utils.UIHelper.DrawMuteButton(new Vector2(muteX, 4f));
    }

    // ────────────────────────────────────────────────────────────────
    // Opponent zones — fan upward (verso) + arcana pile
    // ────────────────────────────────────────────────────────────────

    private void RenderOpponents(GameState state, List<Player> opponents, ImGuiRenderer r, float W, float oppH)
    {
        if (opponents.Count == 0) return;

        int pendingDamage = 0;
        if (state.CurrentTurnPhase == TurnPhase.PlayPhase)
        {
            int localIdx = state.Players.IndexOf(state.LocalPlayer);
            float bst = state.TurnStates.TryGetValue(localIdx, out var lts2) && lts2.MultiplierDoubled ? 2f : 1f;
            var board = state.LocalPlayer.Decks[DeckType.BoardDeck0];
            pendingDamage = _damageCalc.CalculateDamage(board.Cards, bst);
        }

        const float spacing = 6f;
        float slotW = (W - spacing * (opponents.Count - 1)) / opponents.Count;

        for (int i = 0; i < opponents.Count; i++)
        {
            if (i > 0) ImGui.SameLine(0, spacing);

            var opp = opponents[i];
            ImGui.BeginChild($"##opp{i}", new Vector2(slotW, oppH - spacing), ImGuiChildFlags.Borders);

            // ── Name + HP bar (Custom with Preview) ───────────────────
            ImGui.TextUnformatted(opp.Name);
            ImGui.SameLine(0, 8);
            
            var hpPos = ImGui.GetCursorScreenPos();
            var hpSize = new Vector2(Math.Max(10f, ImGui.GetContentRegionAvail().X), 18f);
            ImGui.Dummy(hpSize); // Reserve space
            
            var dl = ImGui.GetWindowDrawList();
            
            uint bgCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1f));
            dl.AddRectFilled(hpPos, hpPos + hpSize, bgCol);
            
            float currentHpRatio = Math.Clamp(opp.Health / 100f, 0f, 1f);
            float futureHpRatio = Math.Clamp((opp.Health - pendingDamage) / 100f, 0f, 1f);
            
            float currentW = hpSize.X * currentHpRatio;
            float futureW = hpSize.X * futureHpRatio;

            if (futureW > 0f)
                dl.AddRectFilled(hpPos, hpPos + new Vector2(futureW, hpSize.Y), ImGui.ColorConvertFloat4ToU32(HpColor(futureHpRatio)));
            
            if (pendingDamage > 0 && opp.Health > 0)
            {
                float dmgW = currentW - futureW;
                if (dmgW > 0f)
                {
                    float time = (float)ImGui.GetTime();
                    float alpha = 0.5f + 0.3f * MathF.Sin(time * 8f);
                    uint previewCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.3f, 0.1f, alpha));
                    dl.AddRectFilled(hpPos + new Vector2(futureW, 0), hpPos + new Vector2(currentW, hpSize.Y), previewCol);
                }
            }

            dl.AddRect(hpPos, hpPos + hpSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.6f, 0.2f, 1f)), 0f, 0, 1f);

            string hpText = $"{opp.Health}/100";
            if (pendingDamage > 0 && opp.Health > 0) hpText += $" (-{pendingDamage})";
            Vector2 ts = ImGui.CalcTextSize(hpText);
            dl.AddText(hpPos + new Vector2((hpSize.X - ts.X)/2f, (hpSize.Y - ts.Y)/2f), ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)), hpText);

            // ── Arcana deck pile (small, top-left of box) ────────────
            if (opp.Decks.TryGetValue(DeckType.SpecialDeck, out var arcDeck) && arcDeck.Cards.Count > 0)
            {
                ImGui.TextDisabled($"Arc: {arcDeck.Cards.Count}");
                ImGui.SameLine(0, 4);
                var arcTex = r.GetOrBindTexture(arcDeck.Cards[0].TextureVerso);
                var arcOrigin = ImGui.GetCursorScreenPos();
                ImGui.Dummy(new Vector2(30, 50));
                ImGui.GetWindowDrawList().AddImageQuad(arcTex,
                    arcOrigin, arcOrigin + new Vector2(30, 0),
                    arcOrigin + new Vector2(30, 50), arcOrigin + new Vector2(0, 50),
                    new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1));
                ImGui.SameLine(0, 6);
            }

            // ── Hand fan (face-down, fanning UPWARD from bottom) ─────
            if (opp.Decks.TryGetValue(DeckType.MainDeck, out var md) && md.Cards.Count > 0)
                RenderOppFan(opp, md, r, slotW);

            // ── Board cards (face up so everyone can see what was played) ───
            if (opp.Decks.TryGetValue(DeckType.BoardDeck0, out var brd) && brd.Cards.Count > 0)
            {
                ImGui.Spacing();
                int oppIdx = state.Players.IndexOf(opp);
                bool oppValidated = state.TurnStates.TryGetValue(oppIdx, out var ots) && ots.HasValidated;
                string boardLabel = oppValidated ? $"Plateau ({brd.Cards.Count}) [ok]" : $"Plateau ({brd.Cards.Count})";
                ImGui.TextDisabled(boardLabel);
                foreach (var c in brd.Cards)
                {
                    ImGui.Image(r.GetOrBindTexture(c.TextureRecto), CardOppBrd);
                    ImGui.SameLine(0, 2);
                }
            }

            // ── Imperatrice: show opponent hand if visible ───────────
            if (state.TurnStates.TryGetValue(0, out var lts) && lts.CanSeeOpponentHands
                && opp.Decks.TryGetValue(DeckType.HandDeck, out var oh) && oh.Cards.Count > 0)
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.4f, 1f, 0.8f, 1f), "Main visible:");
                foreach (var c in oh.Cards)
                {
                    string lbl = c is ValueCard vc2 ? vc2.DisplayName
                               : c is ArcanaCard ac2 ? ac2.ArcanaName : c.TextureRecto.Name;
                    ImGui.TextDisabled($"  {lbl}");
                }
            }

            // ── Death states ─────────────────────────────────────────
            bool willDie = (opp.Health - pendingDamage) <= 0 && pendingDamage > 0;
            bool isDead = opp.Health <= 0;
            if (willDie || isDead)
            {
                var cwMin = ImGui.GetWindowPos();
                var cwMax = cwMin + ImGui.GetWindowSize();
                uint crossCol = isDead ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.1f, 0.1f, 0.7f)) : ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.1f, 0.1f, 0.4f));
                dl.AddLine(cwMin, cwMax, crossCol, 8f);
                dl.AddLine(new Vector2(cwMax.X, cwMin.Y), new Vector2(cwMin.X, cwMax.Y), crossCol, 8f);
            }

            // ── Target selection overlay ─────────────────────────────
            if (state.PendingActivation != null && opp.Health > 0)
            {
                var wMin = ImGui.GetWindowPos();
                var wMax = wMin + ImGui.GetWindowSize();
                var mousePos = ImGui.GetIO().MousePos;
                bool isHov = mousePos.X >= wMin.X && mousePos.X <= wMax.X
                          && mousePos.Y >= wMin.Y && mousePos.Y <= wMax.Y;

                float time = (float)ImGui.GetTime();
                float pulse = 0.5f + 0.3f * MathF.Sin(time * 4f);

                // Pulsing border
                uint borderCol = isHov
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.85f, 0.1f, 1f))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.7f, 0.1f, pulse));
                dl.AddRect(wMin, wMax, borderCol, 0f, 0, isHov ? 3f : 2f);

                // Tinted overlay on hover
                if (isHov)
                {
                    uint tint = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.9f, 0.2f, 0.12f));
                    dl.AddRectFilled(wMin, wMax, tint);
                }

                // "CIBLER" label
                string targetLabel = isHov ? ">> CIBLER <<" : "CIBLER";
                var lblSize = ImGui.CalcTextSize(targetLabel);
                var lblPos = new Vector2(wMin.X + (wMax.X - wMin.X - lblSize.X) / 2f, wMax.Y - lblSize.Y - 4f);
                uint lblCol = ImGui.ColorConvertFloat4ToU32(isHov
                    ? new Vector4(1f, 1f, 0.4f, 1f)
                    : new Vector4(1f, 0.9f, 0.2f, pulse));
                dl.AddText(lblPos, lblCol, targetLabel);

                // Click to select target
                if (isHov && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    var p = state.PendingActivation;
                    _commands.Enqueue(new ActivateArcanaWithTargetCommand(p.Activator, p.Card, opp, _effectManager));
                }
            }

            ImGui.EndChild();
        }
    }

    /// <summary>
    /// Renders opponent's deck as a face-down fan, anchored at the TOP, opening DOWNWARD.
    /// This is the "across the table" view — inverted relative to the local player's fan.
    /// </summary>
    private static void RenderOppFan(Player opp, Deck deck, ImGuiRenderer r, float slotW)
    {
        int n = Math.Min(deck.Cards.Count, 15);
        if (n == 0) return;

        float oppRadius  = 90f;
        float spreadRad  = MathF.Min(MathF.PI * 50f / 180f, n * 0.12f);
        float maxAngle   = spreadRad / 2f;
        float outerDy    = oppRadius * (1f - MathF.Cos(maxAngle));
        float oppFanH    = outerDy + CardOpp.Y + 14f; // exact fit

        Vector2 origin   = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(slotW - 12f, oppFanH));

        var dl = ImGui.GetWindowDrawList();
        // Anchor at TOP center — fan opens downward (inverted / "à l'envers")
        float cx = origin.X + (slotW - 12f) * 0.5f;
        float cy = origin.Y + 6f;

        float angleStep  = n > 1 ? spreadRad / (n - 1) : 0f;
        float startAngle = -spreadRad / 2f;

        for (int i = 0; i < n; i++)
        {
            float angle  = startAngle + i * angleStep;
            float cardCx = cx + MathF.Sin(angle) * oppRadius;
            // FanDown from top: cy increases with (1-cos)
            float cardCy = cy + (1f - MathF.Cos(angle)) * oppRadius + CardOpp.Y * 0.5f;

            var tex = r.GetOrBindTexture(deck.Cards[i].TextureVerso);
            // Rotate cards 180° extra so they appear "à l'envers" (upside-down)
            DrawFanCard(dl, tex, new Vector2(cardCx, cardCy), angle + MathF.PI, CardOpp);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Board zone
    // ────────────────────────────────────────────────────────────────

    private void RenderBoard(GameState state, Player player, ImGuiRenderer r, float w, float h)
    {
        bool isPlayPhase = state.CurrentTurnPhase == TurnPhase.PlayPhase;
        var  board       = player.Decks[DeckType.BoardDeck0];

        ImGui.BeginChild("##board", new Vector2(w, h), ImGuiChildFlags.Borders);

        if (board.Cards.Count == 0)
        {
            ImGui.TextDisabled("Plateau - posez vos cartes en phase Jeu");
            ImGui.EndChild();
            return;
        }

        ImGui.TextDisabled("PLATEAU");
        if (isPlayPhase)
        {
            int idx   = state.Players.IndexOf(player);
            float bst = state.TurnStates.TryGetValue(idx, out var ts2) && ts2.MultiplierDoubled ? 2f : 1f;
            int dmg   = _damageCalc.CalculateDamage(board.Cards, bst);
            ImGui.SameLine(0, 12);
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.3f, 1f), $">> {dmg} degats potentiels");
        }

        ImGui.Spacing();

        var mousePos = ImGui.GetIO().MousePos;
        bool clicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var dl = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();

        ImGui.Dummy(new Vector2(w - 12f, CardBoard.Y + 4f));

        var toTake = new List<ValueCard>();
        for (int i = 0; i < board.Cards.Count; i++)
        {
            var card = board.Cards[i];
            float cardX = origin.X + i * (CardBoard.X + 6f) + CardBoard.X * 0.5f;
            float cardY = origin.Y + CardBoard.Y * 0.5f;
            var center = new Vector2(cardX, cardY);

            var tex = r.GetOrBindTexture(card.TextureRecto);
            DrawFanCard(dl, tex, center, 0f, CardBoard);

            if (IsMouseInRotatedRect(mousePos, center, CardBoard.X, CardBoard.Y, 0f))
            {
                int pidx = state.Players.IndexOf(player);
                bool md2 = state.TurnStates.TryGetValue(pidx, out var mts2) && mts2.MultiplierDoubled;
                CardTooltip(r, card, md2);
                if (clicked && isPlayPhase && card is ValueCard vc) { toTake.Add(vc); clicked = false; }
            }
        }
        foreach (var vc in toTake) _commands.Enqueue(new TakeBackCardCommand(player, vc));

        ImGui.EndChild();
    }

    // ────────────────────────────────────────────────────────────────
    // Local player zone
    // ────────────────────────────────────────────────────────────────

    private void RenderLocal(GameState state, Player player, ImGuiRenderer r, float w, float h, ref bool marketOpen)
    {
        bool isMyTurn    = state.IsLocalPlayerTurn;
        bool isPlayPhase = state.CurrentTurnPhase == TurnPhase.PlayPhase && isMyTurn;
        bool isShopPhase = state.CurrentTurnPhase == TurnPhase.ShopPhase;
        int  localIdx    = state.Players.IndexOf(player);
        bool alreadyVal  = state.TurnStates.TryGetValue(localIdx, out var ts) && ts.HasValidated;
        bool arcanaLimit = ts != null && ts.ArcanasPlayedThisTurn >= ts.MaxArcanasPerTurn;

        ImGui.BeginChild("##local", new Vector2(w, h), ImGuiChildFlags.Borders);

        // ── Stats (name + HP bar, no gold — gold is in banner) ───────
        // "Waiting" overlay when it's not the local player's turn
        if (!state.IsSinglePlayer && state.CurrentTurnPhase == TurnPhase.PlayPhase && !isMyTurn)
        {
            var cur = state.CurrentPlayer;
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.55f, 0.55f, 0.55f, 1f));
            ImGui.TextUnformatted($"En attente — tour de {cur?.Name ?? "?"}");
            ImGui.PopStyleColor();
            ImGui.Separator();
        }

        ImGui.TextColored(new Vector4(0.4f, 0.9f, 1f, 1f), player.Name);
        ImGui.SameLine(0, 12);
        float f = Math.Clamp(player.Health / 100f, 0f, 1f);
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, HpColor(f));
        ImGui.ProgressBar(f, new Vector2(280, 18), $"HP {player.Health}/100");
        ImGui.PopStyleColor();

        int pioche = player.Decks.TryGetValue(DeckType.MainDeck,       out var mdd) ? mdd.Cards.Count : 0;
        int arcArc = player.Decks.TryGetValue(DeckType.ArcanaHandDeck, out var aha) ? aha.Cards.Count : 0;
        ImGui.SameLine(0, 12);
        ImGui.TextDisabled($"Pioche: {pioche}  Arc: {arcArc}");

        var effects = _effectManager.GetActiveEffectSummary();
        if (effects.Count > 0)
        {
            ImGui.SameLine(0, 12);
            ImGui.TextColored(new Vector4(0.85f, 0.55f, 1f, 1f), "Effets:");
            foreach (var (en, turns) in effects)
            { ImGui.SameLine(0, 4); ImGui.TextDisabled($"[{en} {turns}t]"); }
        }

        ImGui.Separator();

        // ── Fan + arcana grid side by side ────────────────────────────
        const float arcGridW = 96f; // 2 cols × small card
        float fanW = w - 12f - arcGridW - 6f;

        Vector2 splitOrigin = ImGui.GetCursorScreenPos();
        RenderHandFan(state, player, r, fanW, isPlayPhase, localIdx);

        // Arcana grid — right column, same vertical origin as the fan
        ImGui.SetCursorScreenPos(new Vector2(splitOrigin.X + fanW + 6f, splitOrigin.Y));
        RenderArcanaGrid(player, r, isPlayPhase, arcanaLimit, arcGridW, FanH);

        // Advance cursor past the combined area
        ImGui.SetCursorScreenPos(new Vector2(splitOrigin.X, splitOrigin.Y + FanH + 2f));

        ImGui.Separator();

        // ── Deck piles row: [ArcDeck] --- [MainDeck] --- [DiscardDeck]
        RenderDeckPiles(player, r, w - 12f);

        ImGui.Separator();

        // ── Buttons: action left | right-aligned column ──────────────
        bool isActionPhase = (isPlayPhase && !alreadyVal) || isShopPhase;
        string actionLabel = isShopPhase ? "Fin de boutique" : "Valider le tour";
        Vector4 btnColor = isShopPhase ? new Vector4(0.60f, 0.35f, 0.08f, 1f) : new Vector4(0.15f, 0.60f, 0.25f, 1f);
        string boutiqueLabel = marketOpen ? "Fermer" : "Boutique";
        bool targeting = state.PendingActivation != null;

        const float btnH = 30f;
        const float rightColW = 140f;
        const float gap = 4f;
        float availW = ImGui.GetContentRegionAvail().X;
        var rowOrigin = ImGui.GetCursorPos();
        float rightX = rowOrigin.X + availW - rightColW;

        // Left: main action button
        if (Alea_Jacta_Est.Utils.UIHelper.DrawPixelButton("btn_action", actionLabel, new Vector2(availW - rightColW - 12f, btnH), btnColor, !isActionPhase))
        {
            if (isShopPhase) _commands.Enqueue(new EndShopPhaseCommand(localIdx));
            else             _commands.Enqueue(new ValidateTurnCommand(localIdx));
        }

        // Right column: stacked buttons, all same width, positioned absolutely
        ImGui.SetCursorPos(new Vector2(rightX, rowOrigin.Y));
        if (Alea_Jacta_Est.Utils.UIHelper.DrawPixelButton("btn_shop", boutiqueLabel, new Vector2(rightColW, btnH), new Vector4(0.2f, 0.3f, 0.6f, 1f), false))
            marketOpen = !marketOpen;

        if (targeting)
        {
            var pending = state.PendingActivation!;

            ImGui.SetCursorPos(new Vector2(rightX, rowOrigin.Y + btnH + gap));
            if (Alea_Jacta_Est.Utils.UIHelper.DrawPixelButton("btn_self", "Se cibler soi", new Vector2(rightColW, btnH), new Vector4(0.6f, 0.5f, 0.1f, 1f), false))
                _commands.Enqueue(new ActivateArcanaWithTargetCommand(pending.Activator, pending.Card, pending.Activator, _effectManager));

            ImGui.SetCursorPos(new Vector2(rightX, rowOrigin.Y + (btnH + gap) * 2f));
            if (Alea_Jacta_Est.Utils.UIHelper.DrawPixelButton("btn_cancel", "Annuler", new Vector2(rightColW, btnH), new Vector4(0.5f, 0.15f, 0.15f, 1f), false))
                state.PendingActivation = null;

            // Banner reminder
            var dlLocal = ImGui.GetWindowDrawList();
            var bannerPos = ImGui.GetWindowPos() + new Vector2(0, 2);
            string bannerTxt = $"CIBLE REQUISE : {pending.Card.ArcanaName} ({(pending.Card.IsUpright ? "Endroit" : "Envers")}) — Cliquer sur un adversaire";
            var bannerSize = ImGui.CalcTextSize(bannerTxt);
            float bannerX = bannerPos.X + (ImGui.GetWindowSize().X - bannerSize.X) / 2f;
            float time = (float)ImGui.GetTime();
            float pulse = 0.7f + 0.3f * MathF.Sin(time * 3f);
            uint bannerCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.9f, 0.2f, pulse));
            dlLocal.AddText(new Vector2(bannerX, bannerPos.Y), bannerCol, bannerTxt);
        }

        ImGui.EndChild();
    }

    // ────────────────────────────────────────────────────────────────
    // Arcana grid — 2-column scrollable grid on the right side of the fan
    // ────────────────────────────────────────────────────────────────

    private static readonly Vector2 CardArc  = new(40f, 66f);
    private const int   ArcGridCols = 2;
    private const float ArcGridGap  = 4f;

    private void RenderArcanaGrid(Player player, ImGuiRenderer r, bool isPlayPhase, bool arcanaLimit, float gridW, float gridH)
    {
        if (!player.Decks.TryGetValue(DeckType.ArcanaHandDeck, out var arcanaHand))
            return;

        // Header (outside scroll area)
        string header = $"Arc. ({arcanaHand.Cards.Count})";
        float hw = ImGui.CalcTextSize(header).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (gridW - hw) * 0.5f);
        ImGui.TextColored(arcanaHand.Cards.Count == 0
            ? new Vector4(0.45f, 0.45f, 0.45f, 0.7f)
            : new Vector4(0.85f, 0.55f, 1f, 1f), header);

        float headerH  = ImGui.GetTextLineHeightWithSpacing() + 2f;
        float scrollH  = gridH - headerH;
        bool  canActivate = isPlayPhase && !arcanaLimit;

        // Scrollable child — ImGui clips DrawList to child bounds automatically
        ImGui.BeginChild("##arcgrid", new Vector2(gridW, scrollH), ImGuiChildFlags.Borders);
        var dl = ImGui.GetWindowDrawList();

        for (int i = 0; i < arcanaHand.Cards.Count; i++)
        {
            var card = arcanaHand.Cards[i];
            if (card is not ArcanaCard ac) continue;

            int   col = i % ArcGridCols;
            int   row = i / ArcGridCols;
            float x   = col * (CardArc.X + ArcGridGap);
            float y   = row * (CardArc.Y + ArcGridGap);

            // InvisibleButton in child-local coords — positions and sizes the hit area
            ImGui.SetCursorPos(new Vector2(x, y));
            ImGui.PushID(i);
            bool pressed = ImGui.InvisibleButton("ag", CardArc);
            ImGui.PopID();

            // Draw card at the button's actual screen rect
            Vector2 tl     = ImGui.GetItemRectMin();
            Vector2 center = tl + CardArc * 0.5f;
            var texId = r.GetOrBindTexture(card.TextureRecto);
            DrawFanCard(dl, texId, center, 0f, CardArc);

            bool hovered = ImGui.IsItemHovered();
            if (hovered)
            {
                dl.AddRect(tl, tl + CardArc,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.85f, 0.55f, 1f, canActivate ? 1f : 0.35f)),
                    3f, ImDrawFlags.None, 2f);
                CardTooltip(r, card);
            }

            if (pressed && canActivate)
                _commands.Enqueue(new ActivateArcanaCommand(player, ac, null, _effectManager));
        }

        // Red tint when limit reached
        if (arcanaLimit && isPlayPhase && arcanaHand.Cards.Count > 0)
        {
            var wp = ImGui.GetWindowPos();
            var ws = ImGui.GetWindowSize();
            dl.AddRectFilled(wp, wp + ws,
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.1f, 0.1f, 0.12f)));
        }

        ImGui.EndChild();
    }

    // ────────────────────────────────────────────────────────────────
    // Deck piles: ArcanaHandDeck (left) | MainDeck (center) | DiscardDeck (right)
    // ────────────────────────────────────────────────────────────────

    private void RenderDeckPiles(Player player, ImGuiRenderer r, float zoneW)
    {
        const float pileSpacing = 80f;
        const float labelH      = 14f;
        float pileRowH          = CardPile.Y + labelH + 4f;

        Vector2 origin  = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(zoneW, pileRowH));

        var dl       = ImGui.GetWindowDrawList();
        var mousePos = ImGui.GetIO().MousePos;

        float centerX = origin.X + zoneW * 0.5f;
        float cardY   = origin.Y + CardPile.Y * 0.5f;

        // Helper: draw one pile and its label
        void DrawPile(DeckType deckKey, string label, float x, bool showFront)
        {
            if (!player.Decks.TryGetValue(deckKey, out var deck) || deck.Cards.Count == 0)
            {
                // Empty pile — draw border only
                var tl = new Vector2(x - CardPile.X * 0.5f, cardY - CardPile.Y * 0.5f);
                var br = new Vector2(x + CardPile.X * 0.5f, cardY + CardPile.Y * 0.5f);
                dl.AddRect(tl, br, ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.4f, 0.4f, 0.5f)));
                float lw = ImGui.CalcTextSize(label).X;
                dl.AddText(new Vector2(x - lw * 0.5f, cardY + CardPile.Y * 0.5f + 2f),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1f)), label);
                return;
            }

            var topCard = deck.Cards[^1];
            var tex     = showFront ? r.GetOrBindTexture(topCard.TextureRecto)
                                    : r.GetOrBindTexture(topCard.TextureVerso);
            var center  = new Vector2(x, cardY);
            DrawFanCard(dl, tex, center, 0f, CardPile);

            // Count badge (bottom-right corner)
            if (deck.Cards.Count > 1)
            {
                string cnt = $"{deck.Cards.Count}";
                var br     = new Vector2(x + CardPile.X * 0.5f, cardY + CardPile.Y * 0.5f);
                dl.AddRectFilled(br - new Vector2(18, 14), br,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.75f)));
                dl.AddText(br - new Vector2(14, 12),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)), cnt);
            }

            // Label below
            float lw2 = ImGui.CalcTextSize(label).X;
            dl.AddText(new Vector2(x - lw2 * 0.5f, cardY + CardPile.Y * 0.5f + 2f),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.7f, 0.7f, 0.7f, 1f)), label);

            // Tooltip on hover
            if (IsMouseInRotatedRect(mousePos, center, CardPile.X, CardPile.Y, 0f))
                CardTooltip(r, topCard);
        }

        DrawPile(DeckType.SpecialDeck,  "Arc. Pioche", centerX - pileSpacing, false);
        DrawPile(DeckType.MainDeck,     "Pioche",      centerX,               false);
        DrawPile(DeckType.DiscardDeck,  "Defausse",    centerX + pileSpacing, true);
    }

    // ────────────────────────────────────────────────────────────────
    // Hand fan
    // ────────────────────────────────────────────────────────────────

    private void RenderHandFan(GameState state, Player player, ImGuiRenderer r, float zoneW, bool isPlayPhase, int localIdx)
    {
        var hand   = player.Decks[DeckType.HandDeck];
        var arcana = player.Decks[DeckType.ArcanaHandDeck];

        if (hand.Cards.Count == 0 && arcana.Cards.Count == 0)
        {
            ImGui.TextDisabled("Main vide");
            return;
        }

        Vector2 origin = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(zoneW, FanH));

        var dl       = ImGui.GetWindowDrawList();
        var mousePos = ImGui.GetIO().MousePos;
        bool clicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        // ── Value cards fan ──────────────────────────────────────────────
        float fanCenterX = origin.X + zoneW * 0.49f;

        int   n           = hand.Cards.Count;
        float spreadRad   = MathF.Min(FanMaxSpread * MathF.PI / 180f, n * 0.18f);
        float angleStep   = n > 1 ? spreadRad / (n - 1) : 0f;
        float startAngle  = -spreadRad / 2f;

        // Compute anchorY so the lowest card bottom stays within the Dummy
        float maxAngle = n > 1 ? spreadRad / 2f : 0f;
        float outerDy  = FanRadius * (1f - MathF.Cos(maxAngle));
        float verticalOffset = ImGui.GetIO().DisplaySize.Y * 0.02f;
        float anchorY  = origin.Y + FanH - outerDy - CardFan.Y * 0.5f - 6f - verticalOffset;

        var positions = new (Vector2 center, float angle)[n];
        for (int i = 0; i < n; i++)
        {
            float a  = n > 1 ? startAngle + i * angleStep : 0f;
            float cx = fanCenterX + MathF.Sin(a) * FanRadius;
            float cy = anchorY - MathF.Cos(a) * FanRadius + FanRadius;
            positions[i] = (new Vector2(cx, cy), a);
        }

        // Draw back→front
        for (int i = 0; i < n; i++)
        {
            var tex = r.GetOrBindTexture(hand.Cards[i].TextureRecto);
            DrawFanCard(dl, tex, positions[i].center, positions[i].angle, CardFan);
        }

        // Hover & click front→back
        var toPlay = new List<ValueCard>();
        for (int i = n - 1; i >= 0; i--)
        {
            if (!IsMouseInRotatedRect(mousePos, positions[i].center, CardFan.X, CardFan.Y, positions[i].angle))
                continue;
            bool md = state.TurnStates.TryGetValue(localIdx, out var mts) && mts.MultiplierDoubled;
            CardTooltip(r, hand.Cards[i], md);
            if (clicked && isPlayPhase && hand.Cards[i] is ValueCard vc)
            {
                toPlay.Add(vc);
                clicked = false;
            }
            break;
        }
        foreach (var vc in toPlay) _commands.Enqueue(new PlayCardCommand(player, vc));
    }

    // ────────────────────────────────────────────────────────────────
    // ────────────────────────────────────────────────────────────────
    // DrawList helpers
    // ────────────────────────────────────────────────────────────────

    private static void DrawFanCard(ImDrawListPtr dl, nint tex, Vector2 center, float rotation, Vector2 size)
    {
        var (tl, tr, br, bl) = RotatedCorners(center, size.X, size.Y, rotation);
        dl.AddImageQuad(tex, tl, tr, br, bl,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
    }

    private static (Vector2, Vector2, Vector2, Vector2) RotatedCorners(Vector2 c, float w, float h, float rot)
    {
        float cos = MathF.Cos(rot), sin = MathF.Sin(rot);
        float hw = w * 0.5f, hh = h * 0.5f;
        Vector2 R(float lx, float ly) => new(c.X + lx * cos - ly * sin, c.Y + lx * sin + ly * cos);
        return (R(-hw, -hh), R(hw, -hh), R(hw, hh), R(-hw, hh));
    }

    private static bool IsMouseInRotatedRect(Vector2 mouse, Vector2 center, float w, float h, float rotation)
    {
        float dx = mouse.X - center.X, dy = mouse.Y - center.Y;
        float cos = MathF.Cos(-rotation), sin = MathF.Sin(-rotation);
        float lx = dx * cos - dy * sin, ly = dx * sin + dy * cos;
        return MathF.Abs(lx) <= w * 0.5f && MathF.Abs(ly) <= h * 0.5f;
    }

    // ────────────────────────────────────────────────────────────────
    // Tooltip
    // ────────────────────────────────────────────────────────────────

    private static void CardTooltip(ImGuiRenderer r, Card card, bool multiplierDoubled = false)
    {
        ImGui.BeginTooltip();
        ImGui.Image(r.GetOrBindTexture(card.TextureRecto), new Vector2(108, 180));
        ImGui.Separator();
        if (card is ValueCard vc)
        {
            ImGui.TextUnformatted(vc.DisplayName);
            if (vc.IsFaceCard)
            {
                float displayMult = multiplierDoubled ? vc.Multiplier * 2 : vc.Multiplier;
                ImGui.TextDisabled($"Multiplicateur: x{displayMult}");
            }
            else
                ImGui.TextDisabled($"Valeur: {vc.DamageValue} degats");
            ImGui.TextDisabled($"Enseigne: {vc.Suit}");
        }
        else if (card is ArcanaCard ac)
        {
            ImGui.TextUnformatted($"{ac.ArcanaNumber} - {ac.ArcanaName}");
            string orientation = ac.IsUpright ? "Endroit" : "Envers";
            ImGui.TextColored(ac.IsUpright
                ? new Vector4(0.4f, 0.9f, 1f, 1f)
                : new Vector4(1f, 0.5f, 0.3f, 1f), orientation);
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

    private static Vector4 HpColor(float f) => f > 0.6f
        ? new(0.2f, 0.8f, 0.3f, 1f)
        : f > 0.3f ? new(1f, 0.6f, 0.1f, 1f)
        : new(0.9f, 0.2f, 0.2f, 1f);
}
