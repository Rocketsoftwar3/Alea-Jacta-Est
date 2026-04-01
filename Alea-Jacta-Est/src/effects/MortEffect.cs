using System;
using System.Collections.Generic;
using System.Linq;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// La Mort - XIII
/// Endroit : Défausse toutes les cartes de la main et du plateau visés.
///           Le joueur pioche ensuite le même nombre de cartes (valeur + 1 arcanique).
///           Si le joueur se vise lui-même, les cartes arcaniques posées en même temps
///           que La Mort restent sur le terrain.
///           RequiresTarget = true (peut se viser soi-même).
/// Envers  : Cible un joueur : au prochain tour il ne pioche pas la moitié de ses cartes
///           à points et ne pioche pas sa carte arcanique.
///           RequiresTarget = true.
/// </summary>
public class MortEffect : ICardEffect
{
    public int Duration => 0;

    public bool RequiresTarget(ArcanaCard card) => true;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        if (card.IsUpright)
            ResolveEndroit(state, card);
        else
            ResolveEnvers(state);
    }

    private static void ResolveEndroit(GameState state, ArcanaCard mort)
    {
        var activator = state.PendingActivation?.Activator ?? state.CurrentPlayer;
        var target = state.PendingActivation?.SelectedTarget;
        if (target == null || activator == null) return;

        bool selfTarget = target == activator;

        var targetHand = target.Decks[DeckType.HandDeck];
        var targetBoard = target.Decks[DeckType.BoardDeck0];
        var targetDiscard = target.Decks[DeckType.DiscardDeck];
        var targetSpecial = target.Decks[DeckType.SpecialDeck];
        var targetArcanaHand = target.Decks[DeckType.ArcanaHandDeck];
        var targetArcanaDiscard = target.Decks[DeckType.ArcanaDiscardDeck];

        // Count cards to discard (value cards only, not La Mort itself)
        var handValueCards = targetHand.Cards.OfType<ValueCard>().ToList();
        var boardValueCards = targetBoard.Cards.OfType<ValueCard>().ToList();
        int discardCount = handValueCards.Count + boardValueCards.Count;

        // Défausser les cartes à valeur de la main
        foreach (var c in handValueCards)
        {
            targetHand.RemoveCard(c);
            targetDiscard.AddCard(c);
        }

        // Défausser les cartes à valeur du plateau
        foreach (var c in boardValueCards)
        {
            targetBoard.RemoveCard(c);
            targetDiscard.AddCard(c);
        }

        // Si self-target : les arcaniques de la main restent (déjà le cas car on n'a touché que ValueCards)
        // Les arcaniques posées SIMULTANÉMENT à La Mort sont celles sur le plateau (ArcanaCard sur BoardDeck0)
        // On laisse les arcana du plateau intactes pour qu'elles soient jouées ce tour

        if (!selfTarget)
        {
            // Contre un adversaire : défausser aussi leurs arcaniques en main
            var arcanaInHand = targetArcanaHand.Cards.ToList();
            foreach (var c in arcanaInHand)
            {
                targetArcanaHand.RemoveCard(c);
                targetArcanaDiscard.AddCard(c);
            }
        }

        // Piocher le même nombre de cartes à valeur
        var targetMain = target.Decks[DeckType.MainDeck];
        var shuffledDiscard = target.Decks[DeckType.DiscardDeck];

        if (targetMain.Cards.Count < discardCount && shuffledDiscard.Cards.Count > 0)
        {
            targetMain.AddCards(shuffledDiscard.Cards.ToList());
            shuffledDiscard.Cards.Clear();
            Shuffle(targetMain.Cards);
        }

        int toDraw = Math.Min(discardCount, targetMain.Cards.Count);
        for (int i = 0; i < toDraw; i++)
        {
            var drawn = targetMain.DrawCard();
            if (drawn != null) targetHand.AddCard(drawn);
        }

        // Piocher 1 arcanique
        if (targetSpecial.Cards.Count == 0 && targetArcanaDiscard.Cards.Count > 0)
        {
            targetSpecial.AddCards(targetArcanaDiscard.Cards.ToList());
            targetArcanaDiscard.Cards.Clear();
            Shuffle(targetSpecial.Cards);
        }

        var arcana = targetSpecial.DrawCard();
        if (arcana != null) targetArcanaHand.AddCard(arcana);
    }

    private static void ResolveEnvers(GameState state)
    {
        var target = state.PendingActivation?.SelectedTarget;
        if (target == null) return;

        int targetIdx = state.Players.IndexOf(target);
        if (!state.TurnStates.TryGetValue(targetIdx, out var ts)) return;

        // Bloquer la moitié des cartes à points (floor(5/2) = 2) + la carte arcanique
        ts.BlockedPointCardDraws = 5 / 2; // = 2
        ts.BlockArcanaNextTurn = true;
    }

    private static readonly Random _rng = new();
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
