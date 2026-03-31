using System;
using Microsoft.Xna.Framework;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Rendering;

/// <summary>Renders the layered background. All game content (cards, UI) is handled by ImGui.</summary>
public class GameRenderer
{
    private const float ShakeDuration = 0.5f; // seconds
    private const float MaxOffsetX    = 10f;
    private const float MaxOffsetY    = 6f;
    private const int   Pad           = 14;   // overscan to avoid black edges during shift

    private readonly GraphicsResources _gfx;
    private float _shakeTimer = -1f; // -1 = inactive

    public GameRenderer(GraphicsResources gfx)
    {
        _gfx = gfx;
    }

    /// <summary>Trigger a parallax shake on layer 1. Safe to call multiple times in quick succession.</summary>
    public void TriggerShake() { _shakeTimer = 0f; }

    public void Render(GameState state, GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Advance shake timer
        if (_shakeTimer >= 0f)
        {
            _shakeTimer += elapsed;
            if (_shakeTimer >= ShakeDuration)
                _shakeTimer = -1f;
        }

        // Compute layer-1 offset (damped oscillation)
        float offsetX = 0f, offsetY = 0f;
        if (_shakeTimer >= 0f)
        {
            float t         = _shakeTimer;
            float intensity = 1f - t / ShakeDuration; // linear decay 1 → 0
            offsetX = MathF.Sin(t * 22f) * intensity * MaxOffsetX;
            offsetY = MathF.Cos(t * 17f) * intensity * MaxOffsetY;
        }

        int W = _gfx.GraphicsDevice.Viewport.Width;
        int H = _gfx.GraphicsDevice.Viewport.Height;

        _gfx.GraphicsDevice.Clear(Color.Black);
        _gfx.SpriteBatch.Begin(samplerState: _gfx.HighQualitySampler);

        // Layer 0 — static base
        _gfx.SpriteBatch.Draw(
            _gfx.BackgroundLayer0,
            new Rectangle(0, 0, W, H),
            Color.White);

        // Layer 1 — slightly oversized so the shift never reveals black edges
        _gfx.SpriteBatch.Draw(
            _gfx.BackgroundLayer1,
            new Rectangle(-Pad + (int)offsetX, -Pad + (int)offsetY, W + Pad * 2, H + Pad * 2),
            Color.White);

        _gfx.SpriteBatch.End();
    }
}
