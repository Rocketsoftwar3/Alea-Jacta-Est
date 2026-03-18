using Alea_Jacta_Est.ImGuiBackend;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Services;
using Alea_Jacta_Est.Config;

namespace Alea_Jacta_Est;

/// <summary>Main game class - orchestrates game loop and services.</summary>
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private GameContext _context;
    private InputService _inputService;
    private ImGuiRenderer _imGuiRenderer;

    private bool _isResizing = false;

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
        if (_isResizing)
            return;

        _isResizing = true;

        // Update backbuffer to match window size
        _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
        _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
        _graphics.ApplyChanges();

        // Update viewport for letterboxing calculation
        if (_context != null)
        {
            _context.Viewport.Update(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        }

        _isResizing = false;
    }

    protected override void LoadContent()
    {
        var spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load textures
        var backgroundTexture = Content.Load<Texture2D>("main_background");
        var cardRectoTexture = Content.Load<Texture2D>("card_recto_placeholder");
        var cardVersoTexture = Content.Load<Texture2D>("cards/tarot_dos");

        // Initialize GameContext
        _context = new GameContext("LocalPlayer");
        _context.InitGraphics(spriteBatch, GraphicsDevice, Content, backgroundTexture, cardRectoTexture, cardVersoTexture);

        // Initialize demo data
        DemoDataService.InitializeDemoDecks(_context);
    }

    protected override void Update(GameTime gameTime)
    {
        // Handle input
        _inputService.Update();

        if (_inputService.IsExitRequested())
            Exit();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _context.Render();

        _imGuiRenderer.BeforeLayout(gameTime);
        ImGuiOverlayService.Render(_context, _imGuiRenderer);
        _imGuiRenderer.AfterLayout();

        base.Draw(gameTime);
    }
}
