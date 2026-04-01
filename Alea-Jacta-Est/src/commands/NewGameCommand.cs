using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Services;

namespace Alea_Jacta_Est.Commands;

/// <summary>Resets the game state and starts a new game with the same players.</summary>
public record NewGameCommand(DemoDataService DemoData) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        // Reset each player's stats and decks
        foreach (var player in state.Players)
        {
            player.Health = 100;
            player.Wallet = 100;
            player.Market.Deck.Cards.Clear();
            player.Market.Discount = 0f;

            foreach (var deck in player.Decks.Values)
                deck.Cards.Clear();

            // Remove any temporary decks (e.g. Papesse_Temporary, RemovedCards)
            var extraKeys = new System.Collections.Generic.List<string>();
            foreach (var key in player.Decks.Keys)
                if (key is not ("MainDeck" or "DiscardDeck" or "HandDeck"
                    or "SpecialDeck" or "ArcanaHandDeck" or "ArcanaDiscardDeck"
                    or "BoardDeck0"))
                    extraKeys.Add(key);
            foreach (var key in extraKeys)
                player.Decks.Remove(key);
        }

        // Remove any non-local players added by DemoDataService on previous run;
        // DemoDataService.InitializeDemoDecks adds opponents — avoid duplicating them.
        // Since InitializeDemoDecks always adds exactly 3 opponents, trim back to 1 (local only).
        while (state.Players.Count > 1)
            state.Players.RemoveAt(state.Players.Count - 1);

        state.IsSinglePlayer = true;
        DemoData.InitializeDemoDecks(state);
        state.StartGame();

        events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }
}
