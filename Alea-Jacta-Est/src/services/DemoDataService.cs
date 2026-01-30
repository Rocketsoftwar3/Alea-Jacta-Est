using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

/// <summary>Initializes demo data for testing.</summary>
public static class DemoDataService
{
    /// <summary>Initializes demo decks for local player and opponents.</summary>
    public static void InitializeDemoDecks(GameContext context)
    {
        // Initialize local player decks
        var localPlayer = context.LocalPlayer;
        for (int i = 0; i < 10; i++)
        {
            localPlayer.Decks["MainDeck"].AddCard(new Card(context.CardRectoTexture, context.CardVersoTexture));
        }

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                localPlayer.Decks[$"BoardDeck{i}"].AddCard(new Card(context.CardRectoTexture, context.CardVersoTexture));
            }
        }

        for (int i = 0; i < 5; i++)
        {
            localPlayer.Decks["SpecialDeck"].AddCard(new Card(context.CardRectoTexture, context.CardVersoTexture));
            localPlayer.Decks["DiscardDeck"].AddCard(new Card(context.CardRectoTexture, context.CardVersoTexture));
        }

        // Add 4 opponent players
        for (int opponentIndex = 0; opponentIndex < 4; opponentIndex++)
        {
            var opponent = new Player($"Opponent {opponentIndex + 1}", opponentIndex + 1, isLocalPlayer: false);

            // Add cards to opponent decks
            for (int i = 0; i < 8; i++)
            {
                opponent.Decks["MainDeck"].AddCard(new Card(context.CardRectoTexture, context.CardVersoTexture));
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    opponent.Decks[$"BoardDeck{i}"].AddCard(new Card(context.CardRectoTexture, context.CardVersoTexture));
                }
            }

            for (int i = 0; i < 3; i++)
            {
                opponent.Decks["SpecialDeck"].AddCard(new Card(context.CardRectoTexture, context.CardVersoTexture));
                opponent.Decks["DiscardDeck"].AddCard(new Card(context.CardRectoTexture, context.CardVersoTexture));
            }

            context.Players.Add(opponent);
        }
    }
}
