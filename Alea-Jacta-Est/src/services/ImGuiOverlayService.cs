using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class ImGuiOverlayService
{
    private bool _marketOpen;

    private readonly CardInteractionService _cardInteraction;
    private readonly HUDService _hud;
    private readonly MarketWindowService _marketWindow;
    private readonly VictoryScreenService _victoryScreen;

    public ImGuiOverlayService(
        CardInteractionService cardInteraction,
        HUDService hud,
        MarketWindowService marketWindow,
        VictoryScreenService victoryScreen)
    {
        _cardInteraction = cardInteraction;
        _hud = hud;
        _marketWindow = marketWindow;
        _victoryScreen = victoryScreen;
    }

    public void Render(GameState state, GraphicsResources gfx, ImGuiRenderer imGuiRenderer)
    {
        _cardInteraction.Render(state, gfx, imGuiRenderer);
        _hud.Render(state, ref _marketOpen);
        _marketWindow.Render(state, imGuiRenderer, ref _marketOpen);
        _victoryScreen.Render(state);
    }
}
