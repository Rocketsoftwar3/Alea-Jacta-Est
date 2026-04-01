using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class ImGuiOverlayService
{
    private bool _marketOpen;

    private readonly GameTableService     _gameTable;
    private readonly MarketWindowService  _marketWindow;
    private readonly VictoryScreenService _victoryScreen;

    public ImGuiOverlayService(
        GameTableService gameTable,
        MarketWindowService marketWindow,
        VictoryScreenService victoryScreen)
    {
        _gameTable     = gameTable;
        _marketWindow  = marketWindow;
        _victoryScreen = victoryScreen;
    }

    public void Render(GameState state, GraphicsResources gfx, ImGuiRenderer imGuiRenderer)
    {
        _gameTable.Render(state, gfx, imGuiRenderer, ref _marketOpen);
        _marketWindow.Render(state, imGuiRenderer, ref _marketOpen);
        _victoryScreen.Render(state);
    }
}
