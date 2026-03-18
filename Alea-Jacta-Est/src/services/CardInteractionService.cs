using System;
using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

/// <summary>
/// Handles hover and click on the local player's cards using ImGui's mouse API
/// and BackgroundDrawList. No InvisibleButton — hit-testing uses the actual
/// rotated card rectangle so the fan curvature is respected precisely.
/// </summary>
public static class CardInteractionService
{
    private static readonly DeckType[] LocalDeckTypes =
    {
        DeckType.MainDeck, DeckType.SpecialDeck, DeckType.DiscardDeck,
        DeckType.BoardDeck0, DeckType.BoardDeck1, DeckType.BoardDeck2, DeckType.BoardDeck3
    };

    public static void Render(GameContext ctx)
    {
        var io = ImGui.GetIO();

        // When an ImGui info window is hovered the user is interacting with the UI,
        // not the game board — skip card interaction entirely.
        bool canInteract = !io.WantCaptureMouse;
        bool leftClicked = canInteract && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var mousePos    = io.MousePos;

        // BackgroundDrawList renders above SpriteBatch but behind all ImGui windows.
        var drawList  = ImGui.GetBackgroundDrawList();
        uint fillCol  = ImGui.ColorConvertFloat4ToU32(new Vector4(0.45f, 0.75f, 1.00f, 0.15f));
        uint borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.45f, 0.75f, 1.00f, 0.90f));

        foreach (var deckType in LocalDeckTypes)
        {
            var deck   = PositionConfig.GetDeck(ctx.LocalPlayer, deckType);
            var config = PositionConfig.GetDeckConfig(ctx.LocalPlayer, deckType);

            foreach (var card in deck.Cards)
                card.IsHovered = false;

            var positions = DeckRenderer.GetCardPositions(
                deck,
                ctx.Viewport.RelativeToScreen(config.RelativePosition),
                config.DisplayMode,
                ctx.Viewport.Scale,
                config.BaseCardScale,
                config.FanSpreadDegrees,
                config.FanRadius,
                config.StackOffset);

            // Iterate from topmost card (last rendered) so only the visible top card
            // gets the interaction when cards overlap in the fan.
            bool handled = false;
            for (int i = positions.Count - 1; i >= 0; i--)
            {
                var info = positions[i];
                float w = Card.BaseWidth  * info.RenderScale;
                float h = Card.BaseHeight * info.RenderScale;

                var center = ToNumerics(info.Center);
                if (!handled && canInteract && IsMouseInRotatedRect(mousePos, center, w, h, info.Rotation))
                {
                    handled = true;
                    info.Card.IsHovered = true;

                    // Draw highlight using the actual rotated quad — matches the card exactly
                    var (tl, tr, br, bl) = RotatedCorners(center, w, h, info.Rotation);
                    drawList.AddQuadFilled(tl, tr, br, bl, fillCol);
                    drawList.AddQuad      (tl, tr, br, bl, borderCol, 2.5f);

                    if (leftClicked)
                    {
                        info.Card.IsFlipped = !info.Card.IsFlipped;
                        leftClicked = false; // consume — one flip per click across all decks
                    }
                }
            }
        }
    }

    // ── Conversion helpers ───────────────────────────────────────────────────

    private static System.Numerics.Vector2 ToNumerics(Microsoft.Xna.Framework.Vector2 v) => new(v.X, v.Y);

    // ── Geometry helpers ────────────────────────────────────────────────────

    /// <summary>True if <paramref name="mouse"/> is inside the card's rotated rectangle.</summary>
    private static bool IsMouseInRotatedRect(Vector2 mouse, Vector2 center, float w, float h, float rotation)
    {
        float dx = mouse.X - center.X;
        float dy = mouse.Y - center.Y;

        // Rotate the mouse point into the card's local (unrotated) space
        float cos    = (float)Math.Cos(-rotation);
        float sin    = (float)Math.Sin(-rotation);
        float localX = dx * cos - dy * sin;
        float localY = dx * sin + dy * cos;

        return Math.Abs(localX) <= w * 0.5f && Math.Abs(localY) <= h * 0.5f;
    }

    /// <summary>Returns the 4 screen-space corners of a rotated card rectangle (TL, TR, BR, BL).</summary>
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
