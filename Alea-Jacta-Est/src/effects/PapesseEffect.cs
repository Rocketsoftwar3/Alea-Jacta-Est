using System.Linq;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// La Papesse - II
/// Endroit (3 tours) : Les cartes gardées en main à la fin de chaque manche vont dans une pile à part
///                     (pas à la défausse). +5 pièces/tour. Au 4e tour, la pile retourne en main.
///                     Ces cartes comptent quand même pour le gain d'argent.
/// Envers  (1 tour)  : Le tour suivant, tous les adversaires voient leurs cartes uniquement par
///                     leur valeur monétaire.
/// </summary>
public class PapesseEffect : ICardEffect
{
    private const string TempDeck = "Papesse_Temporary";

    // Endroit = Duration 3, Envers = Duration 1
    public int Duration => 3;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        var player = state.CurrentPlayer;
        if (player == null) return;

        if (card.IsUpright)
        {
            // Créer la pile temporaire si elle n'existe pas
            if (!player.Decks.ContainsKey(TempDeck))
                player.Decks[TempDeck] = new Deck();
        }
        else
        {
            // Envers: durée effective = 1 tour — on coupe court via OnRemove immédiat.
            // Signaler aux adversaires qu'ils sont en mode "valeur monétaire uniquement".
            int localIdx = state.Players.IndexOf(player);
            foreach (var (idx, ts) in state.TurnStates)
                if (idx != localIdx)
                    ts.HandVisibleAsMoneyOnly = true;
        }
    }

    public void OnTurnEnd(GameState state, ArcanaCard card)
    {
        if (!card.IsUpright) return;

        var player = state.CurrentPlayer;
        if (player == null) return;

        // +5 pièces par tour
        player.Wallet += 5;

        // Déplacer toutes les cartes de la main vers la pile temporaire
        // AVANT que CleanupPhase ne les mette à la défausse
        if (!player.Decks.ContainsKey(TempDeck))
            player.Decks[TempDeck] = new Deck();

        var hand = player.Decks["HandDeck"];
        var temp = player.Decks[TempDeck];
        temp.AddCards(hand.Cards.ToList());
        hand.Cards.Clear();
    }

    public void OnRemove(GameState state, ArcanaCard card)
    {
        var player = state.CurrentPlayer;
        if (player == null) return;

        if (card.IsUpright)
        {
            // Retourner la pile temporaire en main pour ce tour
            if (player.Decks.TryGetValue(TempDeck, out var temp))
            {
                player.Decks["HandDeck"].AddCards(temp.Cards.ToList());
                player.Decks.Remove(TempDeck);
            }
        }
        else
        {
            // Fin de l'effet envers : retirer le flag sur tous les adversaires
            foreach (var ts in state.TurnStates.Values)
                ts.HandVisibleAsMoneyOnly = false;
        }
    }
}
