using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Services;

namespace Alea_Jacta_Est;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private CardRenderer _cardRenderer;
    private Card _cardVerso;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _cardRenderer = new CardRenderer(_spriteBatch);

        Texture2D cardVersoTexture = Content.Load<Texture2D>("card_verso");
        Vector2 centerPosition = new Vector2(
            GraphicsDevice.Viewport.Width / 2f,
            GraphicsDevice.Viewport.Height / 2f
        );
        _cardVerso = new Card(cardVersoTexture, centerPosition);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkGreen);

        _spriteBatch.Begin();
        _cardRenderer.Draw(_cardVerso);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}