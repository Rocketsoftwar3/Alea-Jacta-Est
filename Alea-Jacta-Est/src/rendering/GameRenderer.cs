using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Services;
using Microsoft.Xna.Framework;

namespace Alea_Jacta_Est.Rendering;

public class GameRenderer
{
    private readonly GraphicsResources _gfx;
    private readonly DeckRenderer _deckRenderer;

    public GameRenderer(GraphicsResources gfx, DeckRenderer deckRenderer)
    {
        _gfx = gfx;
        _deckRenderer = deckRenderer;
    }

    public void Render(GameState state)
    {
        _gfx.GraphicsDevice.Clear(new Color(0x5c, 0x00, 0x00));

        // Draw background fullscreen
        _gfx.SpriteBatch.Begin(samplerState: _gfx.HighQualitySampler);
        _gfx.SpriteBatch.Draw(
            _gfx.BackgroundTexture,
            new Rectangle(0, 0, _gfx.GraphicsDevice.Viewport.Width, _gfx.GraphicsDevice.Viewport.Height),
            Color.White
        );
        _gfx.SpriteBatch.End();

        // Draw game content in the 16:9 viewport
        _gfx.SpriteBatch.Begin(samplerState: _gfx.HighQualitySampler);
        foreach (var player in state.Players)
            DrawPlayerDecks(player);
        _gfx.SpriteBatch.End();
    }

    private void DrawPlayerDecks(Player player)
    {
        DrawDeck(player, DeckType.MainDeck);
        DrawDeck(player, DeckType.SpecialDeck);
        DrawDeck(player, DeckType.DiscardDeck);
        DrawDeck(player, DeckType.BoardDeck0);
        DrawDeck(player, DeckType.BoardDeck1);
        DrawDeck(player, DeckType.BoardDeck2);
        DrawDeck(player, DeckType.BoardDeck3);
    }

    private void DrawDeck(Player player, DeckType deckType)
    {
        var deck = PositionConfig.GetDeck(player, deckType);
        var config = PositionConfig.GetDeckConfig(player, deckType);
        float scale = _gfx.Viewport.Scale;

        _deckRenderer.Draw(
            _gfx.SpriteBatch,
            deck,
            _gfx.Viewport.RelativeToScreen(config.RelativePosition),
            config.DisplayMode,
            config.IsFrontVisible,
            globalScale: scale,
            cardScale: config.BaseCardScale,
            fanSpread: config.FanSpreadDegrees,
            fanRadius: config.FanRadius,
            stackOffset: config.StackOffset
        );
    }
}
