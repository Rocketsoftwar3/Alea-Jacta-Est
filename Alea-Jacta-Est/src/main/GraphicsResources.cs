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
    public Texture2D CardRectoTexture { get; }
    public Texture2D CardVersoTexture { get; }

    public GraphicsResources(
        SpriteBatch spriteBatch,
        GraphicsDevice graphicsDevice,
        ContentManager content,
        Texture2D background,
        Texture2D cardRecto,
        Texture2D cardVerso)
    {
        SpriteBatch = spriteBatch;
        GraphicsDevice = graphicsDevice;
        Content = content;
        BackgroundTexture = background;
        CardRectoTexture = cardRecto;
        CardVersoTexture = cardVerso;

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
