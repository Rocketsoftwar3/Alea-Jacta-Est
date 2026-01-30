using System;
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
    /// Endroit : Pendant 3 tours, les cartes en main vont dans une pile à part (pas défausse),
    /// +5 pièces/tour. Au 4ème tour, cartes de la pile à part retournent en main.
    /// </summary>
    private void ResolveEndroit(GameContext context, Card card)
    {
        var currentPlayer = context.CurrentPlayer;
        if (currentPlayer == null) return;

        // Créer la pile à part si elle n'existe pas
        const string tempDeckName = "Papesse_Temporary";
        if (!currentPlayer.Decks.ContainsKey(tempDeckName))
        {
            currentPlayer.Decks[tempDeckName] = new Deck();
        }

        // Enregistrer l'effet pour 3 tours
        // TODO: Implémenter système d'effets persistants
        // - À chaque fin de tour pendant 3 tours :
        //   * Cartes en main -> pile à part au lieu de défausse
        //   * +5 pièces
        // - Au 4ème tour :
        //   * Cartes de pile à part -> MainDeck
        //   * Nettoyer la pile temporaire

        // Pour l'instant, juste marquer le début de l'effet
        int activationTurn = context.CurrentTurn;

        // Stocker les métadonnées de l'effet (on pourrait utiliser un système plus robuste)
        // currentPlayer.ActiveEffects["Papesse_Endroit"] = new EffectMetadata
        // {
        //     ActivationTurn = activationTurn,
        //     Duration = 3
        // };
    }

    /// <summary>
    /// Envers : Tour suivant, tous les adversaires voient leurs cartes
    /// uniquement par leur valeur monétaire.
    /// </summary>
    private void ResolveEnvers(GameContext context, Card card)
    {
        // Activer le flag pour masquer les cartes adverses
        // TODO: Ajouter un flag dans GameContext pour gérer l'affichage
        // context.OpponentsSeePriceOnly = true;
        // context.OpponentsSeePriceOnlyUntilTurn = context.CurrentTurn + 1;

        // Ce flag devra être vérifié dans le renderer pour afficher
        // uniquement le prix au lieu de la texture des cartes
    }
}
