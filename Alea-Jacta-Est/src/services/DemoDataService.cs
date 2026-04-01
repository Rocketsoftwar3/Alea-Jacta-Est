using System;
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
        var rng = new Random();
        var localPlayer = state.LocalPlayer;
        localPlayer.Wallet = 100;

        // Main deck: 20 unique value cards (no duplicates)
        foreach (var card in _cardFactory.CreateUniqueValueCards(20, rng))
            localPlayer.Decks[DeckType.MainDeck].AddCard(card);

        // Special deck: 5 arcana cards
        for (int i = 0; i < 5; i++)
            localPlayer.Decks[DeckType.SpecialDeck].AddCard(_cardFactory.BuildRandom());

        // Market: 12 arcana cards for purchase
        for (int i = 0; i < 12; i++)
            localPlayer.Market.Deck.AddCard(_cardFactory.BuildRandom());

        // Add 3 opponent players (MaxPlayers=4, so 3 opponents)
        for (int opponentIndex = 0; opponentIndex < 3; opponentIndex++)
        {
            var opponent = new Player($"Joueur {opponentIndex + 2}", opponentIndex + 1, isLocalPlayer: false);

            foreach (var card in _cardFactory.CreateUniqueValueCards(15, rng))
                opponent.Decks[DeckType.MainDeck].AddCard(card);

            for (int i = 0; i < 5; i++)
                opponent.Decks[DeckType.SpecialDeck].AddCard(_cardFactory.BuildRandom());

            state.Players.Add(opponent);
        }
    }
}
