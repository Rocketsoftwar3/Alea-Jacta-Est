using System;
using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Config;
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
public static class CardInteractionService
{
    private static readonly DeckType[] LocalDeckTypes =
    {
        DeckType.MainDeck, DeckType.SpecialDeck, DeckType.DiscardDeck,
        DeckType.BoardDeck0, DeckType.BoardDeck1, DeckType.BoardDeck2, DeckType.BoardDeck3
    };

    private const float HoverScale  = 1.12f; // card grows when hovered
    private const float HoverLiftPx = 12f;   // card lifts upward when hovered

    public static void Render(GameContext ctx, ImGuiRenderer imGuiRenderer)
    {
        var io = ImGui.GetIO();

        bool canInteract = !io.WantCaptureMouse;
        bool leftClicked = canInteract && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var mousePos     = io.MousePos;

        var drawList = ImGui.GetBackgroundDrawList();

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
                    // Lift the card along the screen-up axis (Y-axis before rotation)
                    float liftX = -(float)Math.Sin(info.Rotation) * HoverLiftPx;
                    float liftY = -(float)Math.Cos(info.Rotation) * HoverLiftPx;
                    var liftedCenter = new Vector2(center.X + liftX, center.Y + liftY);

                    // Use the full display rotation (includes IsUpright 180° flip)
                    float displayRotation = info.Rotation + (info.Card.IsUpright ? 0f : Microsoft.Xna.Framework.MathHelper.Pi);
                    var (tl, tr, br, bl) = RotatedCorners(liftedCenter, wH, hH, displayRotation);

                    // UV order matches corner order: TL(0,0) TR(1,0) BR(1,1) BL(0,1)
                    drawList.AddImageQuad(texId,
                        tl, tr, br, bl,
                        new Vector2(0, 0), new Vector2(1, 0),
                        new Vector2(1, 1), new Vector2(0, 1));

                    // ── Tooltip ──────────────────────────────────────────────────
                    string cardName = FormatTextureName(texture.Name);
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(cardName);
                    ImGui.Separator();
                    ImGui.TextUnformatted($"Orientation : {(info.Card.IsUpright ? "Endroit" : "Envers")}");
                    if (info.Card.Price > 0)
                        ImGui.TextUnformatted($"Prix        : {info.Card.Price}");
                    ImGui.EndTooltip();

                    // ── Click ────────────────────────────────────────────────────
                    if (leftClicked)
                    {
                        info.Card.IsFlipped = !info.Card.IsFlipped;
                        leftClicked = false;
                    }
                }
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string FormatTextureName(string texturePath)
    {
        // "cards/tarot_bateleur" → "tarot bateleur"
        string file = System.IO.Path.GetFileNameWithoutExtension(texturePath ?? "");
        return file.Replace("_", " ");
    }

    private static Vector2 ToNumerics(XnaVector2 v) => new(v.X, v.Y);

    /// <summary>True if <paramref name="mouse"/> is inside the card's rotated rectangle.</summary>
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

    /// <summary>Returns the 4 screen-space corners of a rotated rectangle (TL, TR, BR, BL).</summary>
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
