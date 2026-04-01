using System;
using System.Linq;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Services;

namespace Alea_Jacta_Est.Networking;

/// <summary>
/// Initializes all player decks for a multiplayer game using a shared seed
/// so every client and host generates identical card distributions.
/// </summary>
public class MultiplayerDeckService
{
    private readonly CardFactory      _factory;
    private readonly GraphicsResources _gfx;

    public MultiplayerDeckService(CardFactory factory, GraphicsResources gfx)
    {
        _factory = factory;
        _gfx     = gfx;
    }

    public void InitializeDecks(GameState state, long seed)
    {
        var rng = new Random((int)(seed & 0x7FFFFFFF));

        foreach (var player in state.Players)
        {
            player.Wallet = 100;

            // Main deck: 20 random value cards
            for (int i = 0; i < 20; i++)
                player.Decks[DeckType.MainDeck].AddCard(CreateRandomValue(rng));

            // Special deck: 5 arcana cards
            for (int i = 0; i < 5; i++)
                player.Decks[DeckType.SpecialDeck].AddCard(CreateRandomArcana(rng));

            // Market: 12 arcana cards for purchase
            for (int i = 0; i < 12; i++)
                player.Market.Deck.AddCard(CreateRandomArcana(rng));
        }
    }

    private Alea_Jacta_Est.Entities.ValueCard CreateRandomValue(Random rng)
    {
        var suits = (Alea_Jacta_Est.Entities.CardSuit[])Enum.GetValues(typeof(Alea_Jacta_Est.Entities.CardSuit));
        var ranks = (Alea_Jacta_Est.Entities.CardRank[])Enum.GetValues(typeof(Alea_Jacta_Est.Entities.CardRank));
        return _factory.CreateValueCard(suits[rng.Next(suits.Length)], ranks[rng.Next(ranks.Length)]);
    }

    private Alea_Jacta_Est.Entities.ArcanaCard CreateRandomArcana(Random rng)
    {
        bool isUpright = rng.Next(2) == 0;
        var builder    = _factory.CreateRandomArcanaBuilder(isUpright);
        return builder.Build(_gfx);
    }
}
