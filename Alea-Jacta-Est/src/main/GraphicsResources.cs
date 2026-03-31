using Alea_Jacta_Est.Utils;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Alea_Jacta_Est.Main;

public class GraphicsResources
{
    public SpriteBatch SpriteBatch { get; }
    public GraphicsDevice GraphicsDevice { get; }
    public ContentManager Content { get; }
    public GameViewport Viewport { get; }
    public SamplerState HighQualitySampler { get; }

    public Texture2D BackgroundTexture { get; }
    public Texture2D BackgroundLayer0 { get; }
    public Texture2D BackgroundLayer1 { get; }
    public Texture2D CardRectoTexture { get; }
    public Texture2D CardVersoTexture { get; }
    public Texture2D GoldCoin { get; }

    public GraphicsResources(
        SpriteBatch spriteBatch,
        GraphicsDevice graphicsDevice,
        ContentManager content,
        Texture2D background,
        Texture2D backgroundLayer0,
        Texture2D backgroundLayer1,
        Texture2D cardRecto,
        Texture2D cardVerso,
        Texture2D goldCoin)
    {
        SpriteBatch = spriteBatch;
        GraphicsDevice = graphicsDevice;
        Content = content;
        BackgroundTexture = background;
        BackgroundLayer0 = backgroundLayer0;
        BackgroundLayer1 = backgroundLayer1;
        CardRectoTexture = cardRecto;
        CardVersoTexture = cardVerso;
        GoldCoin = goldCoin;

        Viewport = new GameViewport();
        Viewport.Update(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);

        HighQualitySampler = new SamplerState
        {
            Filter = TextureFilter.Anisotropic,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            MaxAnisotropy = 16
        };
    }
}
