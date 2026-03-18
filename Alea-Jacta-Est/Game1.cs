using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Rendering;
using Alea_Jacta_Est.Services;
using Alea_Jacta_Est.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Alea_Jacta_Est;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private ImGuiRenderer _imGuiRenderer;
    private InputService _inputService;

    // Core state
    private GameState _state;
    private GraphicsResources _gfx;
    private EventBus _eventBus;
    private CommandQueue _commands;

    // Services
    private GameRenderer _gameRenderer;
    private ImGuiOverlayService _overlay;
    private EffectManager _effectManager;

    private bool _isResizing;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.HardwareModeSwitch = false;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;

        _graphics.PreferredBackBufferWidth = PositionConfig.ReferenceWidth;
        _graphics.PreferredBackBufferHeight = PositionConfig.ReferenceHeight;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        _inputService = new InputService(_graphics, Window);

        base.Initialize();

        _imGuiRenderer = new ImGuiRenderer(this);
        _imGuiRenderer.RebuildFontAtlas();
    }

    private void OnClientSizeChanged(object sender, System.EventArgs e)
    {
        if (_isResizing) return;
        _isResizing = true;

        _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
        _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
        _graphics.ApplyChanges();

        _gfx?.Viewport.Update(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

        _isResizing = false;
    }

    protected override void LoadContent()
    {
        var spriteBatch = new SpriteBatch(GraphicsDevice);

        var backgroundTexture = Content.Load<Texture2D>("main_background");
        var cardRectoTexture = Content.Load<Texture2D>("card_recto_placeholder");
        var cardVersoTexture = Content.Load<Texture2D>("cards/tarot_dos");

        // Core state
        _state = new GameState("LocalPlayer");
        _gfx = new GraphicsResources(spriteBatch, GraphicsDevice, Content, backgroundTexture, cardRectoTexture, cardVersoTexture);
        _eventBus = new EventBus();
        _commands = new CommandQueue();

        // Services (DI manuelle)
        var deckRenderer = new DeckRenderer();
        _gameRenderer = new GameRenderer(_gfx, deckRenderer);
        _effectManager = new EffectManager(_eventBus);

        var cardInteraction = new CardInteractionService(deckRenderer, _commands);
        var marketWindow = new MarketWindowService(_commands);
        _overlay = new ImGuiOverlayService(cardInteraction, marketWindow);

        // Initialize demo data
        var cardFactory = new CardFactory(_gfx);
        var demoData = new DemoDataService(cardFactory, _gfx);
        demoData.InitializeDemoDecks(_state);
    }

    protected override void Update(GameTime gameTime)
    {
        _inputService.Update();

        if (_inputService.IsExitRequested())
            Exit();

        // Execute all commands queued during previous frame's render
        _commands.ExecuteAll(_state, _eventBus);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _gameRenderer.Render(_state);

        _imGuiRenderer.BeforeLayout(gameTime);
        _overlay.Render(_state, _gfx, _imGuiRenderer);
        _imGuiRenderer.AfterLayout();

        base.Draw(gameTime);
    }
}
