using System;
using System.IO;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Networking;
using Alea_Jacta_Est.Rendering;
using Alea_Jacta_Est.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;

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

    // Gameplay services
    private GameRenderer _gameRenderer;
    private ImGuiOverlayService _overlay;
    private EffectManager _effectManager;
    private TurnService _turnService;
    private DamageCalculationService _damageCalc;
    private CardFactory _cardFactory;
    private DemoDataService _demoData;

    // Network / lobby
    private NetworkManager _network;
    private LanDiscovery _lan;
    private LobbyManager _lobby;
    private NetworkCommandQueue _netCommandQueue;
    private HostGameController? _hostController;
    private ClientGameController? _clientController;
    private MainMenuService _mainMenu;
    private LobbyScreenService _lobbyScreen;
    private NetworkErrorOverlay _netErrorOverlay;
    private IntroDialogueService _introDialogue;

    private bool _isResizing;
    public static SoundEffectInstance BgmInstance { get; private set; }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.HardwareModeSwitch = false;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;

        _graphics.PreferredBackBufferWidth  = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        _introDialogue = new IntroDialogueService();
    }

    protected override void Initialize()
    {
        _inputService = new InputService(_graphics, Window);

        base.Initialize();

        _imGuiRenderer = new ImGuiRenderer(this);
        _imGuiRenderer.RebuildFontAtlas();

        _introDialogue.LoadAvatar(GraphicsDevice, _imGuiRenderer);
    }

    private void OnClientSizeChanged(object sender, System.EventArgs e)
    {
        if (_isResizing) return;
        _isResizing = true;

        _graphics.PreferredBackBufferWidth  = Window.ClientBounds.Width;
        _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
        _graphics.ApplyChanges();

        _gfx?.Viewport.Update(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

        _isResizing = false;
    }

    protected override void LoadContent()
    {
        // Diagnostic — write paths to a log file next to the exe
        var diagPath = Path.Combine(AppContext.BaseDirectory, "diag.txt");
        File.WriteAllText(diagPath,
            $"AppContext.BaseDirectory: {AppContext.BaseDirectory}\n" +
            $"Environment.CurrentDirectory: {Environment.CurrentDirectory}\n" +
            $"Content.RootDirectory: {Content.RootDirectory}\n" +
            $"Content dir exists: {Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Content"))}\n" +
            $"main_background exists: {File.Exists(Path.Combine(AppContext.BaseDirectory, "Content", "main_background.xnb"))}\n" +
            $"TitleContainer check: {Microsoft.Xna.Framework.TitleContainer.OpenStream("Content/main_background.xnb") != null}\n");

        var spriteBatch   = new SpriteBatch(GraphicsDevice);
        var backgroundTex = Content.Load<Texture2D>("main_background");
        var bgLayer0      = Content.Load<Texture2D>("background/background_layer0");
        var bgLayer1      = Content.Load<Texture2D>("background/background_layer1");
        var cardRectoTex  = Content.Load<Texture2D>("card_recto_placeholder");
        var cardVersoTex  = Content.Load<Texture2D>("cards/tarot_dos");
        var goldCoinTex   = Content.Load<Texture2D>("goldcoin");

        // Game state starts in WaitingForPlayers — main menu drives initialization
        _state    = new GameState("Joueur");
        _gfx      = new GraphicsResources(spriteBatch, GraphicsDevice, Content, backgroundTex, bgLayer0, bgLayer1, cardRectoTex, cardVersoTex, goldCoinTex);
        _eventBus = new EventBus();
        _commands = new CommandQueue();

        _gameRenderer  = new GameRenderer(_gfx);
        _effectManager = new EffectManager(_eventBus);
        _damageCalc    = new DamageCalculationService();
        _cardFactory   = new CardFactory(_gfx);
        _turnService   = new TurnService(_effectManager, _damageCalc, _cardFactory, _eventBus);

        _eventBus.Subscribe<Events.CardPlacedOnBoard>(_ => _gameRenderer.TriggerShake());
        _eventBus.Subscribe<Events.CardPlayed>(_ => _gameRenderer.TriggerShake());
        _eventBus.Subscribe<Events.TurnValidated>(_ => _gameRenderer.TriggerShake());
        _demoData     = new DemoDataService(_cardFactory, _gfx);

        // Network / lobby (created before services so _netCommandQueue can be passed)
        _network         = new NetworkManager();
        _lan             = new LanDiscovery();
        _lobby           = new LobbyManager(_network, _lan);
        _netCommandQueue = new NetworkCommandQueue(_commands, _network);

        var marketWindow  = new MarketWindowService(_netCommandQueue);
        var victoryScreen = new VictoryScreenService(_netCommandQueue, _demoData);
        var gameTable     = new GameTableService(_netCommandQueue, _effectManager, _damageCalc);
        _overlay          = new ImGuiOverlayService(gameTable, marketWindow, victoryScreen);

        _mainMenu        = new MainMenuService(_lobby, _lan, _netCommandQueue, _demoData);
        _lobbyScreen     = new LobbyScreenService(_lobby, _network);
        _netErrorOverlay = new NetworkErrorOverlay(_netCommandQueue, _eventBus);

        _eventBus.Subscribe<Events.ReturnedToMenu>(_ => OnReturnedToMenu());

        // Background Music
        try
        {
            string baseDir = AppContext.BaseDirectory;
            string[] musicPaths = {
                Path.Combine(baseDir, "Content", "music", "AleaJactaEstMusique.wav"),
                Path.Combine(baseDir, "AleaJactaEstMusique.wav"),
                Path.Combine("Content", "music", "AleaJactaEstMusique.wav"),
                "AleaJactaEstMusique.wav",
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Content\music\AleaJactaEstMusique.wav")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\AleaJactaEstMusique.wav")),
            };

            string musicPath = null;
            foreach (var mp in musicPaths)
            {
                if (File.Exists(mp)) { musicPath = mp; break; }
            }

            if (musicPath != null)
            {
                using var stream = File.OpenRead(musicPath);
                var bgm = SoundEffect.FromStream(stream);
                BgmInstance = bgm.CreateInstance();
                BgmInstance.IsLooped = true;
                BgmInstance.Volume = 0.5f;
                BgmInstance.Play();
            }
        }
        catch { /* ignored if no audio device */ }
    }

    protected override void Update(GameTime gameTime)
    {
        try { UpdateInner(gameTime); }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.txt"), ex.ToString());
            throw;
        }
    }

    private void UpdateInner(GameTime gameTime)
    {
        _inputService.Update();

        if (_inputService.IsExitRequested())
            Exit();

        if (!_introDialogue.IsFinished)
        {
            _introDialogue.Update(gameTime);
            base.Update(gameTime);
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // ── Lobby / network update ────────────────────────────────────────────
        if (_state.Phase == GamePhase.WaitingForPlayers)
        {
            _lobby.Update(dt); // includes _network.PollEvents() + LAN discovery

            if (_lobby.IsGameReady)
                StartMultiplayerGame();
        }
        else if (_hostController != null || _clientController != null)
        {
            // Lobby.Update() is no longer called in-game, but we still need to pump
            // the network so clients receive GameStateSync packets.
            _network.PollEvents();
        }

        // ── Command execution ─────────────────────────────────────────────────
        int executedCount = _commands.ExecuteAll(_state, _eventBus);

        // ── Host: broadcast snapshot after processing commands ────────────────
        // ConsumePendingBroadcast() catches commands that arrived FROM clients.
        // executedCount > 0 catches commands the HOST itself enqueued (e.g. validate own turn).
        if (_hostController != null && _state.Phase == GamePhase.InProgress)
        {
            bool fromClient = _hostController.ConsumePendingBroadcast();
            if (fromClient || executedCount > 0)
                _hostController.BroadcastSnapshot();
        }

        // ── Host: tick for auto-validate + shop advancement ───────────────────
        _hostController?.Update(dt);

        // ── Turn phases (host runs these; clients receive them via snapshots) ──
        if (_state.Phase == GamePhase.InProgress)
        {
            bool runPhases = _state.IsSinglePlayer || _hostController != null;
            if (runPhases)
            {
                switch (_state.CurrentTurnPhase)
                {
                    case TurnPhase.DrawPhase:
                        _turnService.ExecuteDrawPhase(_state);
                        if (!_state.IsSinglePlayer)
                            _hostController!.BroadcastSnapshot();
                        break;
                    case TurnPhase.ResolutionPhase:
                        _turnService.ExecuteResolutionPhase(_state);
                        if (!_state.IsSinglePlayer)
                            _hostController!.BroadcastSnapshot();
                        break;
                    case TurnPhase.CleanupPhase:
                        _turnService.ExecuteCleanupPhase(_state);
                        if (!_state.IsSinglePlayer)
                            _hostController!.BroadcastSnapshot();
                        break;
                }
            }
        }

        base.Update(gameTime);
    }

    private void OnReturnedToMenu()
    {
        _network.Disconnect();
        _lobby.LeaveLobby();
        _hostController   = null;
        _clientController = null;
        _netCommandQueue.IsClient = false;
        _netCommandQueue.State    = _state;

        // Rebuild a fresh GameState with just the local player
        _state = new GameState("Joueur");
        _netCommandQueue.State = _state;
    }

    private void StartMultiplayerGame()
    {
        _state = _lobby.BuildGameState();
        _netCommandQueue.State = _state;

        var deckService = new MultiplayerDeckService(_cardFactory, _gfx);
        deckService.InitializeDecks(_state, _lobby.GameSeed);
        _state.StartGame();

        if (_network.IsHost)
        {
            _netCommandQueue.IsClient = false;
            _hostController = new HostGameController(
                _network, _commands, _state, _eventBus, _effectManager, _cardFactory, _gfx);

            // Register peer → player index mappings.
            // LobbyManager.Players are ordered by PlayerId; host is index 0, clients follow.
            int playerIdx = 1;
            foreach (int peerId in _network.ConnectedPeerIds)
                _hostController.RegisterPeer(peerId, playerIdx++);
        }
        else
        {
            _netCommandQueue.IsClient = true;
            _clientController = new ClientGameController(
                _network, _state, _eventBus, _cardFactory, _gfx);

            // Ask the host for its current snapshot so we don't miss the initial DrawPhase.
            _network.SendToHost(new byte[] { NetMsgType.RequestSync });
        }

        _eventBus.Publish(new Events.TurnPhaseChanged(_state.CurrentTurnPhase));
    }

    protected override void Draw(GameTime gameTime)
    {
        try { DrawInner(gameTime); }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.txt"), ex.ToString());
            throw;
        }
    }

    private void DrawInner(GameTime gameTime)
    {
        _gameRenderer.Render(_state, gameTime);

        _imGuiRenderer.BeforeLayout(gameTime);

        if (!_introDialogue.IsFinished)
        {
            _introDialogue.Render((float)gameTime.ElapsedGameTime.TotalSeconds);
        }
        else if (_state.Phase == GamePhase.WaitingForPlayers)
        {
            // Show main menu or lobby screen
            if (_lobby.State == LobbyState.Disconnected)
                _mainMenu.Render(_state);
            else
                _lobbyScreen.Render(_state);
        }
        else
        {
            _overlay.Render(_state, _gfx, _imGuiRenderer);
            _netErrorOverlay.Render(_state);
        }

        _imGuiRenderer.AfterLayout();

        base.Draw(gameTime);
    }
}
