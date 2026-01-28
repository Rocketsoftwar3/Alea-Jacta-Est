using System.Collections.Generic;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Utils;
using Alea_Jacta_Est.Services;
using Alea_Jacta_Est.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Alea_Jacta_Est.Main;

/// <summary>Represents the current state of a game.</summary>
public enum GameState
{
    WaitingForPlayers,
    InProgress,
    Finished
}

/// <summary>Holds all game state - like a Bundle in Android.</summary>
public class GameContext
{
    public const int MaxPlayers = 5;

    // Game state
    public List<Player> Players;
    public GameState State;
    public int CurrentPlayerIndex;
    public int CurrentTurn;
    public Player LocalPlayer;

    // Graphics
    public SpriteBatch SpriteBatch;
    public GraphicsDevice GraphicsDevice;
    public ContentManager Content;
    public GameViewport Viewport;
    public SamplerState HighQualitySampler;

    // Textures
    public Texture2D BackgroundTexture;
    public Texture2D CardRectoTexture;
    public Texture2D CardVersoTexture;

    public Player? CurrentPlayer => Players.Count > 0 && CurrentPlayerIndex < Players.Count
        ? Players[CurrentPlayerIndex]
        : null;

    public GameContext(string localPlayerName)
    {
        Players = new List<Player>(MaxPlayers);
        State = GameState.WaitingForPlayers;
        CurrentPlayerIndex = 0;

        LocalPlayer = new Player(localPlayerName, isLocalPlayer: true);
        Players.Add(LocalPlayer);

        Viewport = new GameViewport();
    }

    /// <summary>Initializes graphics resources.</summary>
    public void InitGraphics(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice,
        ContentManager content, Texture2D background, Texture2D cardRecto, Texture2D cardVerso)
    {
        SpriteBatch = spriteBatch;
        GraphicsDevice = graphicsDevice;
        Content = content;
        BackgroundTexture = background;
        CardRectoTexture = cardRecto;
        CardVersoTexture = cardVerso;

        // Initialize viewport
        Viewport.Update(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);

        // Create high quality sampler state (reused every frame)
        HighQualitySampler = new SamplerState
        {
            Filter = TextureFilter.Anisotropic,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            MaxAnisotropy = 16
        };
    }

    /// <summary>Renders the game state.</summary>
    public void Render()
    {
        GraphicsDevice.Clear(new Color(0x5c, 0x00, 0x00));

        // Draw background fullscreen (covers letterbox areas)
        SpriteBatch.Begin(samplerState: HighQualitySampler);
        SpriteBatch.Draw(
            BackgroundTexture,
            new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            Color.White
        );
        SpriteBatch.End();

        // Draw game content in the 16:9 viewport
        SpriteBatch.Begin(samplerState: HighQualitySampler);

        // Render all players' decks
        foreach (var player in Players)
        {
            DrawPlayerDecks(player);
        }

        SpriteBatch.End();
    }

    /// <summary>Draws all decks for a player.</summary>
    private void DrawPlayerDecks(Player player)
    {
        // Draw all deck types
        DrawDeck(player, DeckType.MainDeck);
        DrawDeck(player, DeckType.SpecialDeck);
        DrawDeck(player, DeckType.DiscardDeck);
        DrawDeck(player, DeckType.BoardDeck0);
        DrawDeck(player, DeckType.BoardDeck1);
        DrawDeck(player, DeckType.BoardDeck2);
        DrawDeck(player, DeckType.BoardDeck3);
    }

    /// <summary>Generic helper to draw any deck for any player.</summary>
    private void DrawDeck(Player player, DeckType deckType)
    {
        var deck = PositionConfig.GetDeck(player, deckType);
        var config = PositionConfig.GetDeckConfig(player, deckType);
        float scale = Viewport.Scale;

        DeckRenderer.Draw(
            SpriteBatch,
            deck,
            Viewport.RelativeToScreen(config.RelativePosition),
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
