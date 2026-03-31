using System;
using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Alea_Jacta_Est.Services;

/// <summary>
/// Handles hover and click on the local player's cards using ImGui's mouse API.
/// Hit-testing uses the actual rotated card rectangle so the fan curvature is respected.
/// On hover: card is redrawn on top via BackgroundDrawList (appears above SpriteBatch layer)
/// and a tooltip with card info is shown.
/// </summary>
public class CardInteractionService
{
    private static readonly DeckType[] LocalDeckTypes =
    {
        DeckType.MainDeck, DeckType.SpecialDeck, DeckType.DiscardDeck,
        DeckType.BoardDeck0, DeckType.BoardDeck1, DeckType.BoardDeck2, DeckType.BoardDeck3,
        DeckType.HandDeck, DeckType.ArcanaHandDeck
    };

    private const float HoverScale  = 1.12f;
    private const float HoverLiftPx = 12f;

    private readonly DeckRenderer _deckRenderer;
    private readonly CommandQueue _commands;
    private readonly EffectManager _effectManager;

    public CardInteractionService(DeckRenderer deckRenderer, CommandQueue commands, EffectManager effectManager)
    {
        _deckRenderer = deckRenderer;
        _commands = commands;
        _effectManager = effectManager;
    }

    public void Render(GameState state, GraphicsResources gfx, ImGuiRenderer imGuiRenderer)
    {
        var io = ImGui.GetIO();

        bool canInteract = !io.WantCaptureMouse;
        bool leftClicked = canInteract && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var mousePos     = io.MousePos;

        var drawList = ImGui.GetBackgroundDrawList();

        foreach (var deckType in LocalDeckTypes)
        {
            var deck   = PositionConfig.GetDeck(state.LocalPlayer, deckType);
            var config = PositionConfig.GetDeckConfig(state.LocalPlayer, deckType);
            if (config == null) continue; // deck has no visual config

            foreach (var card in deck.Cards)
                card.IsHovered = false;

            var positions = _deckRenderer.GetCardPositions(
                deck,
                gfx.Viewport.RelativeToScreen(config.RelativePosition),
                config.DisplayMode,
                gfx.Viewport.Scale,
                config.BaseCardScale,
                config.FanSpreadDegrees,
                config.FanRadius,
                config.StackOffset);

            bool handled = false;
            for (int i = positions.Count - 1; i >= 0; i--)
            {
                var info   = positions[i];
                float w    = Card.BaseWidth  * info.RenderScale;
                float h    = Card.BaseHeight * info.RenderScale;
                var center = ToNumerics(info.Center);

                if (!handled && canInteract && IsMouseInRotatedRect(mousePos, center, w, h, info.Rotation))
                {
                    handled = true;
                    info.Card.IsHovered = true;

                    // ── Redraw card on top via BackgroundDrawList ────────────────
                    bool showFront = config.IsFrontVisible ^ info.Card.IsFlipped;
                    var texture    = showFront ? info.Card.TextureRecto : info.Card.TextureVerso;
                    var texId      = imGuiRenderer.GetOrBindTexture(texture);

                    float wH = w * HoverScale;
                    float hH = h * HoverScale;
                    float liftX = -(float)Math.Sin(info.Rotation) * HoverLiftPx;
                    float liftY = -(float)Math.Cos(info.Rotation) * HoverLiftPx;
                    var liftedCenter = new Vector2(center.X + liftX, center.Y + liftY);

                    float displayRotation = info.Rotation + (info.Card.IsUpright ? 0f : Microsoft.Xna.Framework.MathHelper.Pi);
                    var (tl, tr, br, bl) = RotatedCorners(liftedCenter, wH, hH, displayRotation);

                    drawList.AddImageQuad(texId,
                        tl, tr, br, bl,
                        new Vector2(0, 0), new Vector2(1, 0),
                        new Vector2(1, 1), new Vector2(0, 1));

                    // ── Tooltip ──────────────────────────────────────────────────
                    ImGui.BeginTooltip();
                    if (info.Card is Entities.ValueCard vc)
                    {
                        ImGui.TextUnformatted(vc.DisplayName);
                        ImGui.Separator();
                        if (vc.IsFaceCard)
                            ImGui.TextUnformatted($"Multiplicateur : x{vc.Multiplier}");
                        else
                            ImGui.TextUnformatted($"Valeur : {vc.DamageValue}");
                        ImGui.TextUnformatted($"Enseigne : {vc.Suit}");
                    }
                    else if (info.Card is Entities.ArcanaCard ac)
                    {
                        ImGui.TextUnformatted($"{ac.ArcanaNumber} — {ac.ArcanaName}");
                        ImGui.Separator();
                        ImGui.TextUnformatted($"Orientation : {(ac.IsUpright ? "Endroit" : "Envers")}");
                        if (ac.Price > 0)
                            ImGui.TextUnformatted($"Prix : {ac.Price}");
                    }
                    else
                    {
                        ImGui.TextUnformatted(FormatTextureName(texture.Name));
                        ImGui.Separator();
                        ImGui.TextUnformatted($"Orientation : {(info.Card.IsUpright ? "Endroit" : "Envers")}");
                    }
                    ImGui.EndTooltip();

                    // ── Click → emit command (context-sensitive) ─────────────
                    if (leftClicked)
                    {
                        leftClicked = false;
                        var localPlayer = state.LocalPlayer;

                        if (state.CurrentTurnPhase == TurnPhase.PlayPhase)
                        {
                            if (deckType == DeckType.HandDeck && info.Card is ValueCard clickedValue)
                                _commands.Enqueue(new PlayCardCommand(localPlayer, clickedValue));
                            else if (deckType == DeckType.BoardDeck0 && info.Card is ValueCard boardValue)
                                _commands.Enqueue(new TakeBackCardCommand(localPlayer, boardValue));
                            else if (deckType == DeckType.ArcanaHandDeck && info.Card is ArcanaCard arcana)
                                _commands.Enqueue(new ActivateArcanaCommand(localPlayer, arcana, null, _effectManager));
                            else
                                _commands.Enqueue(new FlipCardCommand(info.Card));
                        }
                        else
                        {
                            _commands.Enqueue(new FlipCardCommand(info.Card));
                        }
                    }
                }
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string FormatTextureName(string texturePath)
    {
        string file = System.IO.Path.GetFileNameWithoutExtension(texturePath ?? "");
        return file.Replace("_", " ");
    }

    private static Vector2 ToNumerics(XnaVector2 v) => new(v.X, v.Y);

    private static bool IsMouseInRotatedRect(Vector2 mouse, Vector2 center, float w, float h, float rotation)
    {
        float dx = mouse.X - center.X;
        float dy = mouse.Y - center.Y;

        float cos    = (float)Math.Cos(-rotation);
        float sin    = (float)Math.Sin(-rotation);
        float localX = dx * cos - dy * sin;
        float localY = dx * sin + dy * cos;

        return Math.Abs(localX) <= w * 0.5f && Math.Abs(localY) <= h * 0.5f;
    }

    private static (Vector2, Vector2, Vector2, Vector2) RotatedCorners(Vector2 center, float w, float h, float rotation)
    {
        float cos = (float)Math.Cos(rotation);
        float sin = (float)Math.Sin(rotation);
        float hw = w * 0.5f, hh = h * 0.5f;

        Vector2 R(float lx, float ly) =>
            new(center.X + lx * cos - ly * sin,
                center.Y + lx * sin + ly * cos);

        return (R(-hw, -hh), R(hw, -hh), R(hw, hh), R(-hw, hh));
    }
}
