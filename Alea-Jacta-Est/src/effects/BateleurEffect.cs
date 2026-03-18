using System;
using System.Linq;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Utils;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// Le Bateleur - I
/// Endroit : Commencement, ressource, enthousiasme, énergie, motivation
/// Envers : Malhonnêteté, violence, blocage, égoïsme, maladresse
/// </summary>
public class BateleurEffect : ICardEffect
{
    private static readonly Random _random = new();

    public int Duration => 0; // one-shot

    public void OnPlay(GameState state, Card card)
    {
        if (card.IsUpright)
            ResolveEndroit(state, card);
        else
            ResolveEnvers(state, card);
    }

    /// <summary>
    /// Endroit : Ajoute 10 pièces, pioche une carte bonus,
    /// et offre un rabais aléatoire (1-2 pièces) pour chaque carte dans la boutique.
    /// </summary>
    private void ResolveEndroit(GameState state, Card card)
    {
        var currentPlayer = state.CurrentPlayer;
        if (currentPlayer == null) return;

        currentPlayer.Wallet += 10;

        // TODO: Implémenter pioche de carte bonus depuis CardFactory

        foreach (var marketCard in currentPlayer.Market.Deck.Cards)
        {
            int discount = Helper.RandomDiscount(1, 2);
            currentPlayer.Market.AddFlatDiscount(discount, marketCard.Price);
        }
    }

    /// <summary>
    /// Envers : Sélectionne une carte d'un adversaire face caché et la supprime.
    /// Si la carte est arcanique, le Bateleur est aussi supprimé.
    /// </summary>
    private void ResolveEnvers(GameState state, Card card)
    {
        var currentPlayer = state.CurrentPlayer;
        if (currentPlayer == null) return;

        var opponents = state.Players.Where(p => p != currentPlayer).ToList();
        if (opponents.Count == 0) return;

        var targetOpponent = opponents[_random.Next(opponents.Count)];

        var opponentDecks = targetOpponent.Decks.Values.Where(d => d.Cards.Count > 0).ToList();
        if (opponentDecks.Count == 0) return;

        var targetDeck = opponentDecks[_random.Next(opponentDecks.Count)];

        if (!targetOpponent.Decks.ContainsKey("RemovedCards"))
        {
            targetOpponent.Decks["RemovedCards"] = new Deck();
        }

        Helper.MoveRandomCard(targetDeck, targetOpponent.Decks["RemovedCards"]);

        // TODO: Vérifier si la carte est arcanique
    }
}
