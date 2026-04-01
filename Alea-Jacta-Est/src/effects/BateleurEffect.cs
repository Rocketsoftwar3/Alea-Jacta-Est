using System;
using System.Linq;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Utils;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// Le Bateleur - I
/// Endroit : +10 pièces, pioche 1 carte bonus, rabais aléatoire (1-2) par carte du marché.
/// Envers   : Supprime une carte face-cachée de l'adversaire ciblé.
///            Si la carte supprimée est arcanique, le Bateleur est également supprimé.
/// </summary>
public class BateleurEffect : ICardEffect
{
    private static readonly Random _rng = new();

    public int Duration => 0;

    public bool RequiresTarget(ArcanaCard card) => !card.IsUpright;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        if (card.IsUpright)
            ResolveEndroit(state);
        else
            ResolveEnvers(state, card);
    }

    private static void ResolveEndroit(GameState state)
    {
        var player = state.CurrentPlayer;
        if (player == null) return;

        // +10 pièces
        player.Wallet += 10;

        // Pioche 1 carte bonus depuis la pioche principale
        var mainDeck = player.Decks[DeckType.MainDeck];
        var hand = player.Decks[DeckType.HandDeck];
        if (mainDeck.Cards.Count > 0)
        {
            var bonus = mainDeck.DrawCard();
            if (bonus != null) hand.AddCard(bonus);
        }

        // Rabais aléatoire (1-2 pièces) pour chaque carte du marché
        foreach (var marketCard in player.Market.Deck.Cards)
        {
            int discount = Helper.RandomDiscount(1, 2);
            player.Market.AddFlatDiscount(discount, marketCard.Price);
        }
    }

    private static void ResolveEnvers(GameState state, ArcanaCard bateleur)
    {
        var target = state.PendingActivation?.SelectedTarget;
        var activator = state.PendingActivation?.Activator ?? state.CurrentPlayer;
        if (target == null || activator == null) return;

        // Choisir un deck face-cachée (MainDeck ou SpecialDeck)
        var faceDownTypes = new[] { DeckType.MainDeck, DeckType.SpecialDeck };
        var faceDownDecks = faceDownTypes
            .Select(k => target.Decks.TryGetValue(k, out var d) ? d : null)
            .Where(d => d != null && d!.Cards.Count > 0)
            .ToList();

        if (faceDownDecks.Count == 0) return;

        var targetDeck = faceDownDecks[_rng.Next(faceDownDecks.Count)]!;

        if (!target.Decks.ContainsKey(DeckType.RemovedCards))
            target.Decks[DeckType.RemovedCards] = new Deck();

        var removed = Helper.MoveRandomCard(targetDeck, target.Decks[DeckType.RemovedCards]);

        // Si la carte supprimée est arcanique → supprimer aussi le Bateleur
        if (removed is ArcanaCard)
        {
            if (!activator.Decks.ContainsKey(DeckType.RemovedCards))
                activator.Decks[DeckType.RemovedCards] = new Deck();
            activator.Decks[DeckType.ArcanaDiscardDeck].RemoveCard(bateleur);
            activator.Decks[DeckType.RemovedCards].AddCard(bateleur);
        }
    }
}
