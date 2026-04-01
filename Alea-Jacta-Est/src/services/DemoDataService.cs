using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class DemoDataService
{
    private readonly CardFactory _cardFactory;
    private readonly GraphicsResources _gfx;

    public DemoDataService(CardFactory cardFactory, GraphicsResources gfx)
    {
        _cardFactory = cardFactory;
        _gfx = gfx;
    }

    public void InitializeDemoDecks(GameState state)
    {
        var localPlayer = state.LocalPlayer;
        localPlayer.Wallet = 100;

        // Main deck: 20 random value cards (draw pile, shown face-down)
        for (int i = 0; i < 20; i++)
            localPlayer.Decks[DeckType.MainDeck].AddCard(_cardFactory.CreateRandomValueCard());

        // Special deck: 5 arcana cards
        for (int i = 0; i < 5; i++)
            localPlayer.Decks[DeckType.SpecialDeck].AddCard(_cardFactory.BuildRandom());

        // Market: 6 arcana cards for purchase
        for (int i = 0; i < 6; i++)
            localPlayer.Market.Deck.AddCard(_cardFactory.BuildRandom());

        // Add 3 opponent players (MaxPlayers=4, so 3 opponents)
        for (int opponentIndex = 0; opponentIndex < 3; opponentIndex++)
        {
            var opponent = new Player($"Joueur {opponentIndex + 2}", opponentIndex + 1, isLocalPlayer: false);

            for (int i = 0; i < 15; i++)
                opponent.Decks[DeckType.MainDeck].AddCard(_cardFactory.CreateRandomValueCard());

            for (int i = 0; i < 5; i++)
                opponent.Decks[DeckType.SpecialDeck].AddCard(_cardFactory.BuildRandom());

            state.Players.Add(opponent);
        }
    }
}
