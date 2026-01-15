using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Services;
using Alea_Jacta_Est.utils;

namespace Alea_Jacta_Est;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private CardRenderer _cardRenderer;
    private DeckRenderer _deckRenderer;

    private Texture2D _backgroundTexture;
    private Texture2D _cardRectoTexture;
    private Texture2D _cardVersoTexture;

    private Deck _deck1;
    private Deck _deck2;
    private List<Deck> _deck3Piles;

    private static class Zones
    {
        internal static ViewportSpace Top => ViewportSpace.Viewport.From(0, 1, 0, 1).To(1, 1, 1, 5);
        internal static ViewportSpace Middle => ViewportSpace.Viewport.From(0, 1, 1, 5).To(1, 1, 3, 5);
        internal static ViewportSpace Bottom => ViewportSpace.Viewport.From(0, 1, 3, 5).To(1, 1, 1, 1);
    }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        ViewportSpace.Bind(GraphicsDevice.Viewport);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _cardRenderer = new CardRenderer(_spriteBatch);
        _deckRenderer = new DeckRenderer(_cardRenderer);

        _backgroundTexture = Content.Load<Texture2D>("main_background");
        _cardRectoTexture = Content.Load<Texture2D>("card_recto_placeholder");
        _cardVersoTexture = Content.Load<Texture2D>("card_verso");

        InitializeDecks();
    }

    private void InitializeDecks()
    {
        _deck1 = new Deck(DeckDisplayMode.FanUp, Zones.Bottom.At(0.5f, 0.5f), faceUp: true)
        {
            FanSpread = MathHelper.ToRadians(60f),
            FanRadius = 160f,
            CardScale = 0.5f
        };
        for (int i = 0; i < 10; i++)
            _deck1.AddCard(new Card(_cardRectoTexture, _cardVersoTexture, Vector2.Zero, true));

        _deck2 = new Deck(DeckDisplayMode.FanDown, Zones.Top.At(0.5f, 0.6f), faceUp: false)
        {
            FanSpread = MathHelper.ToRadians(50f),
            FanRadius = 100f,
            CardScale = 0.35f
        };
        for (int i = 0; i < 10; i++)
            _deck2.AddCard(new Card(_cardRectoTexture, _cardVersoTexture, Vector2.Zero, false));

        _deck3Piles = new List<Deck>();
        float[] xPositions = { 0.2f, 0.4f, 0.6f, 0.8f };

        for (int pileIndex = 0; pileIndex < 4; pileIndex++)
        {
            var pile = new Deck(DeckDisplayMode.Stacked, Zones.Middle.At(xPositions[pileIndex], 0.5f), faceUp: true)
            {
                StackOffset = 15f,
                CardScale = 0.4f
            };
            for (int cardIndex = 0; cardIndex < 3; cardIndex++)
                pile.AddCard(new Card(_cardRectoTexture, _cardVersoTexture, Vector2.Zero, true));

            _deck3Piles.Add(pile);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkGreen);

        _spriteBatch.Begin();

        _spriteBatch.Draw(
            _backgroundTexture,
            new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            Color.White
        );

        _deckRenderer.Draw(_deck2);
        _deckRenderer.Draw(_deck3Piles);
        _deckRenderer.Draw(_deck1);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
