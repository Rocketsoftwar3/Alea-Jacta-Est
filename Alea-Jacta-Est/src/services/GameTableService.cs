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

    private readonly ICommandQueue            _commands;
    private readonly EffectManager            _effectManager;
    private readonly DamageCalculationService _damageCalc;
    private readonly GameLogService           _gameLog;

    private TurnPhase _lastPhase   = TurnPhase.DrawPhase;
    private nint      _goldCoinId  = IntPtr.Zero;

    public GameTableService(ICommandQueue commands, EffectManager effectManager, DamageCalculationService damageCalc, GameLogService gameLog)
    {
        _commands      = commands;
        _effectManager = effectManager;
        _damageCalc    = damageCalc;
        _gameLog       = gameLog;
    }

    // ────────────────────────────────────────────────────────────────
    // Main entry point
    // ────────────────────────────────────────────────────────────────

    public void Render(GameState state, GraphicsResources gfx, ImGuiRenderer r, ref bool marketOpen)
    {
        // Bind goldcoin texture once
        if (_goldCoinId == IntPtr.Zero)
            _goldCoinId = r.GetOrBindTexture(gfx.GoldCoin);

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
        float remaining     = H - bannerH - pad * 3f;
        float oppH           = remaining * 0.25f;
        float boardH         = remaining * 0.30f;
        float localH         = remaining * 0.45f;

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
    // Game log panel — rendered inline inside the board zone (right)
    // ────────────────────────────────────────────────────────────────

    private void RenderGameLogInline(float logW, float logH)
    {
        ImGui.BeginChild("##gamelog", new Vector2(logW, logH), ImGuiChildFlags.Borders);

        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.8f), "Actu");
        ImGui.Separator();

        float contentH = ImGui.GetContentRegionAvail().Y;
        ImGui.BeginChild("##logscroll", new Vector2(0, contentH), ImGuiChildFlags.None);

        var entries = _gameLog.Entries;
        int startIdx = Math.Max(0, entries.Count - 30);
        for (int i = startIdx; i < entries.Count; i++)
        {
            var e = entries[i];
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + logW - 20f);
            ImGui.TextColored(e.Color, e.Message);
            ImGui.PopTextWrapPos();
        }

        if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 10f)
            ImGui.SetScrollHereY(1.0f);

        ImGui.EndChild();
        ImGui.EndChild();
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

        // ── Timer display ─────────────────────────────────────────────
        if (state.ValidateSecondsRemaining > 0f && state.CurrentTurnPhase == TurnPhase.PlayPhase)
        {
            int totalSecs = (int)MathF.Ceiling(state.ValidateSecondsRemaining);
            int mins = totalSecs / 60;
            int secs = totalSecs % 60;
            string timerStr = $"{mins}:{secs:D2}";
            Vector4 timerCol = totalSecs <= 30
                ? new Vector4(1f, 0.3f, 0.2f, 1f)
                : totalSecs <= 60
                    ? new Vector4(1f, 0.7f, 0.2f, 1f)
                    : new Vector4(0.8f, 0.8f, 0.8f, 1f);
            float timerW = ImGui.CalcTextSize(timerStr).X;
            float timerX = W * 0.5f - timerW * 0.5f;
            ImGui.SetCursorPos(new Vector2(timerX, 7f));
            ImGui.TextColored(timerCol, timerStr);
        }

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

            // ── Active arcana effects for this opponent ─────────────
            var oppEffects = _effectManager.GetActiveEffectsForPlayer(opp);
            if (oppEffects.Count > 0)
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.85f, 0.55f, 1f, 1f), "Effets actifs:");
                foreach (var (effCard, turns) in oppEffects)
                {
                    ImGui.Image(r.GetOrBindTexture(effCard.TextureRecto), CardOppBrd);
                    if (ImGui.IsItemHovered())
                        CardTooltip(r, effCard);
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
                    ImGui.Image(r.GetOrBindTexture(c.TextureRecto), CardOppBrd);
                    if (ImGui.IsItemHovered())
                        CardTooltip(r, c);
                    ImGui.SameLine(0, 2);
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

        const float logW = 220f;
        float boardW = w - logW - 6f;

        ImGui.BeginChild("##board", new Vector2(boardW, h), ImGuiChildFlags.Borders);

        if (board.Cards.Count == 0)
        {
            ImGui.TextDisabled("Plateau - posez vos cartes en phase Jeu");
            ImGui.EndChild();
            // Still render log on the right
            ImGui.SameLine(0, 6f);
            RenderGameLogInline(logW, h);
            return;
        }

        // Center the label line
        {
            string plateauLabel = "PLATEAU";
            string dmgLabel = "";
            int dmg = 0;
            if (isPlayPhase)
            {
                int idx   = state.Players.IndexOf(player);
                float bst = state.TurnStates.TryGetValue(idx, out var ts2) && ts2.MultiplierDoubled ? 2f : 1f;
                dmg   = _damageCalc.CalculateDamage(board.Cards, bst);
                dmgLabel = $"  >> {dmg} d\u00e9g\u00e2ts potentiels";
            }
            float totalLabelW = ImGui.CalcTextSize(plateauLabel).X + (dmgLabel.Length > 0 ? ImGui.CalcTextSize(dmgLabel).X : 0);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (boardW - 12f - totalLabelW) * 0.5f);
            ImGui.TextDisabled(plateauLabel);
            if (dmgLabel.Length > 0)
            {
                ImGui.SameLine(0, 0);
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.3f, 1f), dmgLabel);
            }
        }

        ImGui.Spacing();

        var mousePos = ImGui.GetIO().MousePos;
        bool clicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var dl = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();

        ImGui.Dummy(new Vector2(boardW - 12f, CardBoard.Y + 4f));

        int pidx = state.Players.IndexOf(player);
        bool multDoubled = state.TurnStates.TryGetValue(pidx, out var mts2) && mts2.MultiplierDoubled;

        var toTake = new List<ValueCard>();
        float totalCardsW = board.Cards.Count * CardBoard.X + Math.Max(0, board.Cards.Count - 1) * 6f;
        float startX = origin.X + (boardW - 12f - totalCardsW) * 0.5f;
        for (int i = 0; i < board.Cards.Count; i++)
        {
            var card = board.Cards[i];
            float cardX = startX + i * (CardBoard.X + 6f) + CardBoard.X * 0.5f;
            float cardY = origin.Y + CardBoard.Y * 0.5f;
            var center = new Vector2(cardX, cardY);

            var tex = r.GetOrBindTexture(card.TextureRecto);
            DrawFanCard(dl, tex, center, 0f, CardBoard);

            // Overlay value text on card
            if (card is ValueCard vc2)
            {
                string valTxt;
                Vector4 valCol;
                if (vc2.IsFaceCard)
                {
                    float displayMult = multDoubled ? vc2.Multiplier * 2 : vc2.Multiplier;
                    valTxt = $"x{displayMult:G}";
                    valCol = new Vector4(1f, 0.85f, 0.2f, 1f); // gold
                }
                else
                {
                    valTxt = $"+{vc2.DamageValue}";
                    valCol = new Vector4(0.4f, 1f, 0.5f, 1f); // green
                }
                var txtSize = ImGui.CalcTextSize(valTxt);
                var txtPos = new Vector2(center.X - txtSize.X * 0.5f, center.Y + CardBoard.Y * 0.5f - txtSize.Y - 4f);
                dl.AddRectFilled(txtPos - new Vector2(3, 1), txtPos + txtSize + new Vector2(3, 1),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.7f)), 3f);
                dl.AddText(txtPos, ImGui.ColorConvertFloat4ToU32(valCol), valTxt);
            }

            if (IsMouseInRotatedRect(mousePos, center, CardBoard.X, CardBoard.Y, 0f))
            {
                CardTooltip(r, card, multDoubled);
                if (clicked && isPlayPhase && card is ValueCard vc) { toTake.Add(vc); clicked = false; }
            }
        }
        foreach (var vc in toTake) _commands.Enqueue(new TakeBackCardCommand(player, vc));

        ImGui.EndChild();

        // ── Log panel on the right of the board ──────────────────────
        ImGui.SameLine(0, 6f);
        RenderGameLogInline(logW, h);
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

        ImGui.BeginChild("##local", new Vector2(w, h), ImGuiChildFlags.Borders,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        // ── Waiting overlay ─────────────────────────────────────────
        if (!state.IsSinglePlayer && state.CurrentTurnPhase == TurnPhase.PlayPhase && !isMyTurn)
        {
            var cur = state.CurrentPlayer;
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.55f, 0.55f, 0.55f, 1f));
            ImGui.TextUnformatted($"En attente \u2014 tour de {cur?.Name ?? "?"}");
            ImGui.PopStyleColor();
        }

        // ── Stats line: Name | HP bar | hoverable active effects ────
        ImGui.TextColored(new Vector4(0.4f, 0.9f, 1f, 1f), player.Name);
        ImGui.SameLine(0, 12);
        float f = Math.Clamp(player.Health / 100f, 0f, 1f);
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, HpColor(f));
        ImGui.ProgressBar(f, new Vector2(200, 16), $"HP {player.Health}/100");
        ImGui.PopStyleColor();

        // Hoverable active effects (card images with tooltip)
        var playerEffects = _effectManager.GetActiveEffectsForPlayer(player);
        if (playerEffects.Count > 0)
        {
            ImGui.SameLine(0, 12);
            ImGui.TextColored(new Vector4(0.85f, 0.55f, 1f, 1f), "Effets:");
            foreach (var (effCard, turns) in playerEffects)
            {
                ImGui.SameLine(0, 4);
                ImGui.Image(r.GetOrBindTexture(effCard.TextureRecto), new Vector2(20, 33));
                if (ImGui.IsItemHovered())
                    CardTooltip(r, effCard);
            }
        }

        // ── Layout: [Arcana left] | [Fan + Piles + Buttons right] ───
        // No nested children — use cursor positioning within ##local
        var layoutOrigin = ImGui.GetCursorScreenPos();
        float remainH = Math.Max(1f, h - (layoutOrigin.Y - ImGui.GetWindowPos().Y) - 4f);
        const float arcColW = 110f;
        float centerX = layoutOrigin.X + arcColW + 6f;
        float centerW = Math.Max(1f, w - 12f - arcColW - 6f);

        // Left column: Arcana cards (single nested child — safe)
        ImGui.BeginChild("##arc_col", new Vector2(arcColW, remainH), ImGuiChildFlags.None,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        RenderArcanaGrid(player, r, isPlayPhase, arcanaLimit, arcColW, remainH);
        ImGui.EndChild();

        // ── Right area: Fan + Piles + Buttons (rendered via draw list) ──
        float fanH = Math.Min(180f, remainH * 0.65f);
        float pilesW = Math.Min(160f, centerW * 0.22f);
        float fanW = centerW - pilesW - 6f;

        // Fan
        ImGui.SetCursorScreenPos(new Vector2(centerX, layoutOrigin.Y));
        RenderHandFan(state, player, r, fanW, isPlayPhase, localIdx, fanH);

        // Deck piles to the right of the fan
        ImGui.SetCursorScreenPos(new Vector2(centerX + fanW + 6f, layoutOrigin.Y));
        RenderDeckPiles(player, r, pilesW);

        // Advance cursor below the fan/piles row
        float rowH = fanH;
        ImGui.SetCursorScreenPos(new Vector2(centerX, layoutOrigin.Y + rowH + 2f));

        // ── Action buttons ───────────────────────────────────────────
        bool targeting = state.PendingActivation != null;
        bool hasSelected = ts != null && ts.SelectedCards.Count > 0;
        bool canDiscard = ts != null && ts.DiscardsRemaining > 0 && !ts.HasPlayed;
        bool hasPlayed = ts != null && ts.HasPlayed;

        const float btnH = 30f;
        const float gap = 4f;
        var rowOrigin = ImGui.GetCursorPos();

        if (isPlayPhase && !alreadyVal && !hasPlayed)
        {
            float btnW = 140f;
            float totalBtnsW = btnW * 2 + gap;
            float startX = rowOrigin.X + (centerW - totalBtnsW) * 0.5f;

            ImGui.SetCursorPos(new Vector2(startX, rowOrigin.Y));
            string discardLabel = $"D\u00e9fausser ({ts!.DiscardsRemaining})";
            bool discardDisabled = !hasSelected || !canDiscard;
            if (Alea_Jacta_Est.Utils.UIHelper.DrawPixelButton("btn_discard", discardLabel, new Vector2(btnW, btnH),
                new Vector4(0.6f, 0.3f, 0.1f, 1f), discardDisabled))
            {
                _commands.Enqueue(new DiscardCardsCommand(player, new List<Entities.Card>(ts.SelectedCards)));
            }

            ImGui.SetCursorPos(new Vector2(startX + btnW + gap, rowOrigin.Y));
            if (Alea_Jacta_Est.Utils.UIHelper.DrawPixelButton("btn_play", ">> Jouer <<", new Vector2(btnW, btnH),
                new Vector4(0.85f, 0.65f, 0.1f, 1f), !hasSelected))
            {
                _commands.Enqueue(new PlaySelectedCardsCommand(player));
                _commands.Enqueue(new ValidateTurnCommand(localIdx));
            }

            if (hasSelected)
            {
                float time = (float)ImGui.GetTime();
                float pulse = 0.5f + 0.5f * MathF.Sin(time * 3f);
                var btnScreenPos = ImGui.GetWindowPos() + new Vector2(startX + btnW + gap, rowOrigin.Y);
                var btnBR = btnScreenPos + new Vector2(btnW, btnH);
                uint borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.9f, 0.3f, pulse));
                ImGui.GetWindowDrawList().AddRect(btnScreenPos, btnBR, borderCol, 4f, ImDrawFlags.None, 2f);
            }
        }
        else if (isShopPhase)
        {
            // Shop phase handled by modal
        }
        else if (hasPlayed || alreadyVal)
        {
            float lblW = ImGui.CalcTextSize("Tour valid\u00e9 - en attente...").X;
            ImGui.SetCursorPosX(rowOrigin.X + (centerW - lblW) * 0.5f);
            ImGui.TextColored(new Vector4(0.5f, 0.8f, 0.5f, 1f), "Tour valid\u00e9 - en attente...");
        }

        if (targeting)
        {
            var pending = state.PendingActivation!;
            float targetBtnW = 120f;
            float rightX = rowOrigin.X + centerW - targetBtnW;

            ImGui.SetCursorPos(new Vector2(rightX, rowOrigin.Y));
            if (Alea_Jacta_Est.Utils.UIHelper.DrawPixelButton("btn_self", "Se cibler soi", new Vector2(targetBtnW, btnH), new Vector4(0.6f, 0.5f, 0.1f, 1f), false))
                _commands.Enqueue(new ActivateArcanaWithTargetCommand(pending.Activator, pending.Card, pending.Activator, _effectManager));

            ImGui.SetCursorPos(new Vector2(rightX, rowOrigin.Y + btnH + gap));
            if (Alea_Jacta_Est.Utils.UIHelper.DrawPixelButton("btn_cancel", "Annuler", new Vector2(targetBtnW, btnH), new Vector4(0.5f, 0.15f, 0.15f, 1f), false))
                state.PendingActivation = null;

            var dlLocal = ImGui.GetWindowDrawList();
            var bannerPos = ImGui.GetWindowPos() + new Vector2(0, 2);
            string bannerTxt = $"CIBLE REQUISE : {pending.Card.ArcanaName} ({(pending.Card.IsUpright ? "Endroit" : "Envers")}) \u2014 Cliquer sur un adversaire";
            var bannerSize = ImGui.CalcTextSize(bannerTxt);
            float bannerX = bannerPos.X + (ImGui.GetWindowSize().X - bannerSize.X) / 2f;
            float time2 = (float)ImGui.GetTime();
            float pulse2 = 0.7f + 0.3f * MathF.Sin(time2 * 3f);
            uint bannerCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.9f, 0.2f, pulse2));
            dlLocal.AddText(new Vector2(bannerX, bannerPos.Y), bannerCol, bannerTxt);
        }

        ImGui.EndChild(); // ##local
    }

    // ────────────────────────────────────────────────────────────────
    // Arcana column — vertical stack of arcana cards (left of fan)
    // ────────────────────────────────────────────────────────────────

    private void RenderArcanaGrid(Player player, ImGuiRenderer r, bool isPlayPhase, bool arcanaLimit, float colW, float colH)
    {
        if (!player.Decks.TryGetValue(DeckType.ArcanaHandDeck, out var arcanaHand))
            return;

        ImGui.TextColored(new Vector4(0.85f, 0.55f, 1f, 1f), "Arcanes");

        if (arcanaHand.Cards.Count == 0)
        {
            ImGui.TextDisabled("--");
            return;
        }

        bool canActivate = isPlayPhase && !arcanaLimit;
        var dl = ImGui.GetWindowDrawList();

        float headerH = ImGui.GetCursorPosY();
        float availH = colH - headerH;
        int n = arcanaHand.Cards.Count;
        const float spacing = 4f;

        // Grid: compute how many columns fit
        float maxCardW = 48f;
        float maxCardH = maxCardW * (93f / 56f);
        int cols = Math.Max(1, (int)((colW + spacing) / (maxCardW + spacing)));
        int rows = (n + cols - 1) / cols;

        // Shrink cards if they don't fit vertically
        float cardW = maxCardW;
        float cardH = maxCardH;
        float totalH = rows * (cardH + spacing);
        if (totalH > availH && rows > 0)
        {
            cardH = Math.Max(24f, (availH - rows * spacing) / rows);
            cardW = cardH * (56f / 93f);
        }

        var cardSize = new Vector2(cardW, cardH);

        for (int i = 0; i < arcanaHand.Cards.Count; i++)
        {
            var card = arcanaHand.Cards[i];
            if (card is not ArcanaCard ac) continue;

            int col = i % cols;
            int row = i / cols;
            float x = spacing + col * (cardW + spacing);
            float y = headerH + row * (cardH + spacing);

            ImGui.SetCursorPos(new Vector2(x, y));
            ImGui.PushID(i);
            bool pressed = ImGui.InvisibleButton("ac", cardSize);
            ImGui.PopID();

            Vector2 tl = ImGui.GetItemRectMin();
            Vector2 center = tl + cardSize * 0.5f;
            var texId = r.GetOrBindTexture(card.TextureRecto);
            DrawFanCard(dl, texId, center, 0f, cardSize);

            bool hovered = ImGui.IsItemHovered();
            if (hovered)
            {
                float time = (float)ImGui.GetTime();
                float glow = 0.7f + 0.3f * MathF.Sin(time * 4f);
                dl.AddRect(tl - new Vector2(1, 1), tl + cardSize + new Vector2(1, 1),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.85f, 0.55f, 1f, canActivate ? glow : 0.35f)),
                    3f, ImDrawFlags.None, 2f);
                CardTooltip(r, card);
            }

            if (pressed && canActivate)
                _commands.Enqueue(new ActivateArcanaCommand(player, ac, null, _effectManager));
        }

        // Red tint when limit reached
        if (arcanaLimit && isPlayPhase)
        {
            var wp = ImGui.GetWindowPos();
            var ws = ImGui.GetWindowSize();
            dl.AddRectFilled(wp, wp + ws,
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.1f, 0.1f, 0.12f)));
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Deck piles: ArcanaHandDeck (left) | MainDeck (center) | DiscardDeck (right)
    // ────────────────────────────────────────────────────────────────

    private void RenderDeckPiles(Player player, ImGuiRenderer r, float zoneW)
    {
        const float labelH  = 14f;
        const float rowGap  = 4f;

        // Vertical grid: 3 rows, each with a small card + label to the right
        float cellH = CardPile.Y * 0.6f; // smaller cards to fit vertically
        float cellW = CardPile.X * 0.6f;
        float totalH = 3 * cellH + 2 * rowGap + 3 * (labelH + 2f);

        Vector2 origin = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(zoneW, totalH));

        var dl       = ImGui.GetWindowDrawList();
        var mousePos = ImGui.GetIO().MousePos;
        var cardSize = new Vector2(cellW, cellH);

        void DrawPileRow(DeckType deckKey, string label, float y, bool showFront)
        {
            float cx = origin.X + zoneW * 0.5f;
            float cy = y + cellH * 0.5f;
            var center = new Vector2(cx, cy);

            if (!player.Decks.TryGetValue(deckKey, out var deck) || deck.Cards.Count == 0)
            {
                var tl = center - cardSize * 0.5f;
                var br = center + cardSize * 0.5f;
                dl.AddRect(tl, br, ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.4f, 0.4f, 0.5f)));
                float lw = ImGui.CalcTextSize(label).X;
                dl.AddText(new Vector2(cx - lw * 0.5f, cy + cellH * 0.5f + 2f),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1f)), label);
                return;
            }

            var topCard = deck.Cards[^1];
            var tex     = showFront ? r.GetOrBindTexture(topCard.TextureRecto)
                                    : r.GetOrBindTexture(topCard.TextureVerso);
            DrawFanCard(dl, tex, center, 0f, cardSize);

            // Count badge
            if (deck.Cards.Count > 1)
            {
                string cnt = $"{deck.Cards.Count}";
                var br     = center + cardSize * 0.5f;
                dl.AddRectFilled(br - new Vector2(16, 12), br,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.75f)));
                dl.AddText(br - new Vector2(13, 10),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)), cnt);
            }

            // Label below card
            float lw2 = ImGui.CalcTextSize(label).X;
            dl.AddText(new Vector2(cx - lw2 * 0.5f, cy + cellH * 0.5f + 2f),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.7f, 0.7f, 0.7f, 1f)), label);

            if (IsMouseInRotatedRect(mousePos, center, cellW, cellH, 0f))
                CardTooltip(r, topCard);
        }

        float y0 = origin.Y;
        float step = cellH + labelH + 2f + rowGap;
        DrawPileRow(DeckType.MainDeck,     "Pioche",             y0,            false);
        DrawPileRow(DeckType.SpecialDeck,  "Arc. Pioche",        y0 + step,     false);
        DrawPileRow(DeckType.DiscardDeck,  "D\u00e9fausse",      y0 + step * 2, true);
    }

    // ────────────────────────────────────────────────────────────────
    // Hand fan
    // ────────────────────────────────────────────────────────────────

    private void RenderHandFan(GameState state, Player player, ImGuiRenderer r, float zoneW, bool isPlayPhase, int localIdx, float fanH)
    {
        var hand   = player.Decks[DeckType.HandDeck];
        var arcana = player.Decks[DeckType.ArcanaHandDeck];

        if (hand.Cards.Count == 0 && arcana.Cards.Count == 0)
        {
            ImGui.TextDisabled("Main vide");
            return;
        }

        Vector2 origin = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(zoneW, fanH));

        var dl       = ImGui.GetWindowDrawList();
        var mousePos = ImGui.GetIO().MousePos;
        bool clicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        // ── Value cards fan ──────────────────────────────────────────────
        float fanCenterX = origin.X + zoneW * 0.5f;

        int   n           = hand.Cards.Count;
        float spreadRad   = MathF.Min(FanMaxSpread * MathF.PI / 180f, n * 0.18f);
        float angleStep   = n > 1 ? spreadRad / (n - 1) : 0f;
        float startAngle  = -spreadRad / 2f;

        // Compute anchorY so the lowest card bottom stays within the Dummy
        float maxAngle = n > 1 ? spreadRad / 2f : 0f;
        float outerDy  = FanRadius * (1f - MathF.Cos(maxAngle));
        float verticalOffset = ImGui.GetIO().DisplaySize.Y * 0.02f;
        float anchorY  = origin.Y + fanH - outerDy - CardFan.Y * 0.5f - 6f - verticalOffset;

        var positions = new (Vector2 center, float angle)[n];
        for (int i = 0; i < n; i++)
        {
            float a  = n > 1 ? startAngle + i * angleStep : 0f;
            float cx = fanCenterX + MathF.Sin(a) * FanRadius;
            float cy = anchorY - MathF.Cos(a) * FanRadius + FanRadius;
            positions[i] = (new Vector2(cx, cy), a);
        }

        // Get selection state
        var selected = state.TurnStates.TryGetValue(localIdx, out var selTs)
            ? selTs.SelectedCards : new List<Entities.Card>();

        // Draw back→front (selected cards are pushed up)
        const float selectLift = 20f;
        for (int i = 0; i < n; i++)
        {
            var card = hand.Cards[i];
            bool isSel = selected.Contains(card);
            var center = positions[i].center;
            if (isSel)
            {
                float rot = positions[i].angle;
                center = new Vector2(
                    center.X - MathF.Sin(rot) * selectLift,
                    center.Y - MathF.Cos(rot) * selectLift);
            }
            var tex = r.GetOrBindTexture(card.TextureRecto);
            DrawFanCard(dl, tex, center, positions[i].angle, CardFan);

            // Selection highlight border
            if (isSel)
            {
                var (tl, tr, br, bl) = RotatedCorners(center, CardFan.X + 4, CardFan.Y + 4, positions[i].angle);
                dl.AddQuad(tl, tr, br, bl,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.85f, 0.2f, 0.9f)), 2.5f);
            }
        }

        // Hover & click front→back — toggle selection
        for (int i = n - 1; i >= 0; i--)
        {
            var card = hand.Cards[i];
            bool isSel = selected.Contains(card);
            var center = positions[i].center;
            if (isSel)
            {
                float rot = positions[i].angle;
                center = new Vector2(
                    center.X - MathF.Sin(rot) * selectLift,
                    center.Y - MathF.Cos(rot) * selectLift);
            }

            if (!IsMouseInRotatedRect(mousePos, center, CardFan.X, CardFan.Y, positions[i].angle))
                continue;
            bool md = state.TurnStates.TryGetValue(localIdx, out var mts) && mts.MultiplierDoubled;
            CardTooltip(r, card, md);
            if (clicked && isPlayPhase && card is ValueCard)
            {
                // Toggle selection
                if (isSel)
                    selected.Remove(card);
                else
                    selected.Add(card);
                clicked = false;
            }
            break;
        }
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
                ImGui.TextDisabled($"Valeur: {vc.DamageValue} d\u00e9g\u00e2ts");
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
