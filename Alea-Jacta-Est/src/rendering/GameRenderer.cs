using Microsoft.Xna.Framework;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Rendering;

/// <summary>Renders the background only. All game content (cards, UI) is now handled by ImGui.</summary>
public class GameRenderer
{
    private readonly GraphicsResources _gfx;

    public GameRenderer(GraphicsResources gfx)
    {
        _gfx = gfx;
    }

    public void Render(GameState state)
    {
        _gfx.GraphicsDevice.Clear(Color.Black);

        _gfx.SpriteBatch.Begin(samplerState: _gfx.HighQualitySampler);
        _gfx.SpriteBatch.Draw(
            _gfx.BackgroundTexture,
            new Rectangle(0, 0, _gfx.GraphicsDevice.Viewport.Width, _gfx.GraphicsDevice.Viewport.Height),
            Color.White
        );
        _gfx.SpriteBatch.End();
    }
}
