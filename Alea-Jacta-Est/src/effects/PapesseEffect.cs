using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// La Papesse - II
/// Endroit : Fécondité, spiritualité, amour, patience, richesse intérieure
/// Envers : Secret, retard, intuition bloquée
/// </summary>
public class PapesseEffect : ICardEffect
{
    public int Duration => 3;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        var currentPlayer = state.CurrentPlayer;
        if (currentPlayer == null) return;

        if (card.IsUpright)
        {
            // Create temporary deck for Papesse Endroit effect
            const string tempDeckName = "Papesse_Temporary";
            if (!currentPlayer.Decks.ContainsKey(tempDeckName))
            {
                currentPlayer.Decks[tempDeckName] = new Deck();
            }
        }
    }

    public void OnTurnStart(GameState state, ArcanaCard card)
    {
        var currentPlayer = state.CurrentPlayer;
        if (currentPlayer == null) return;

        if (card.IsUpright)
        {
            // +5 pièces par tour
            currentPlayer.Wallet += 5;
            // TODO: Cartes en main -> pile à part au lieu de défausse
        }
    }

    public void OnRemove(GameState state, ArcanaCard card)
    {
        var currentPlayer = state.CurrentPlayer;
        if (currentPlayer == null) return;

        if (card.IsUpright)
        {
            // Retourner les cartes de la pile temporaire vers MainDeck
            const string tempDeckName = "Papesse_Temporary";
            if (currentPlayer.Decks.TryGetValue(tempDeckName, out var tempDeck))
            {
                currentPlayer.Decks["MainDeck"].AddCards(tempDeck.Cards);
                currentPlayer.Decks.Remove(tempDeckName);
            }
        }
    }
}
