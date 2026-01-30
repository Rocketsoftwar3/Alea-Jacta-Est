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
    private static readonly Random _random = new Random();

    public void Resolve(GameContext context, Card card)
    {
        Action<GameContext, Card> resolver = card.IsUpright switch
        {
            true => ResolveEndroit,
            false => ResolveEnvers
        };
        resolver(context, card);
    }

    /// <summary>
    /// Endroit : Ajoute 10 pièces, pioche une carte bonus,
    /// et offre un rabais aléatoire (1-2 pièces) pour chaque carte dans la boutique.
    /// </summary>
    private void ResolveEndroit(GameContext context, Card card)
    {
        var currentPlayer = context.CurrentPlayer;
        if (currentPlayer == null) return;

        // Ajoute 10 pièces au porte-monnaie
        currentPlayer.Wallet += 10;

        // Pioche une carte bonus (depuis CardFactory, mais pour l'instant on skip)
        // TODO: Implémenter pioche de carte bonus depuis CardFactory

        // Offre un rabais aléatoire (1-2 pièces) pour chaque carte dans la boutique
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
    private void ResolveEnvers(GameContext context, Card card)
    {
        var currentPlayer = context.CurrentPlayer;
        if (currentPlayer == null) return;

        // Sélectionne un adversaire aléatoire
        var opponents = context.Players.Where(p => p != currentPlayer).ToList();
        if (opponents.Count == 0) return;

        var targetOpponent = opponents[_random.Next(opponents.Count)];

        // Sélectionne un deck aléatoire de l'adversaire qui a des cartes
        var opponentDecks = targetOpponent.Decks.Values.Where(d => d.Cards.Count > 0).ToList();
        if (opponentDecks.Count == 0) return;

        var targetDeck = opponentDecks[_random.Next(opponentDecks.Count)];

        // Crée un deck temporaire pour "supprimer" la carte
        if (!targetOpponent.Decks.ContainsKey("RemovedCards"))
        {
            targetOpponent.Decks["RemovedCards"] = new Deck();
        }

        // Déplace une carte aléatoire vers le deck de cartes supprimées
        var removedCard = Helper.MoveRandomCard(targetDeck, targetOpponent.Decks["RemovedCards"]);

        // TODO: Vérifier si la carte est arcanique
        // Si oui, supprimer aussi le Bateleur
        // if (removedCard.IsArcanic)
        // {
        //     currentPlayer.Decks["MainDeck"].RemoveCard(card);
        //     if (!currentPlayer.Decks.ContainsKey("RemovedCards"))
        //         currentPlayer.Decks["RemovedCards"] = new Deck();
        //     currentPlayer.Decks["RemovedCards"].AddCard(card);
        // }
    }
}
