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

        for (int i = 0; i < 10; i++)
        {
            localPlayer.Decks["MainDeck"].AddCard(new Card(_gfx.CardRectoTexture, _gfx.CardVersoTexture));
        }

        for (int i = 0; i < 6; i++)
        {
            var card = _cardFactory.BuildRandom();
            localPlayer.Market.Deck.AddCard(card);
        }

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                localPlayer.Decks[$"BoardDeck{i}"].AddCard(new Card(_gfx.CardRectoTexture, _gfx.CardVersoTexture));
            }
        }

        for (int i = 0; i < 5; i++)
        {
            localPlayer.Decks["SpecialDeck"].AddCard(new Card(_gfx.CardRectoTexture, _gfx.CardVersoTexture));
            localPlayer.Decks["DiscardDeck"].AddCard(new Card(_gfx.CardRectoTexture, _gfx.CardVersoTexture));
        }

        // Add 4 opponent players
        for (int opponentIndex = 0; opponentIndex < 4; opponentIndex++)
        {
            var opponent = new Player($"Opponent {opponentIndex + 1}", opponentIndex + 1, isLocalPlayer: false);

            for (int i = 0; i < 8; i++)
            {
                opponent.Decks["MainDeck"].AddCard(new Card(_gfx.CardRectoTexture, _gfx.CardVersoTexture));
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    opponent.Decks[$"BoardDeck{i}"].AddCard(new Card(_gfx.CardRectoTexture, _gfx.CardVersoTexture));
                }
            }

            for (int i = 0; i < 3; i++)
            {
                opponent.Decks["SpecialDeck"].AddCard(new Card(_gfx.CardRectoTexture, _gfx.CardVersoTexture));
                opponent.Decks["DiscardDeck"].AddCard(new Card(_gfx.CardRectoTexture, _gfx.CardVersoTexture));
            }

            state.Players.Add(opponent);
        }
    }
}
