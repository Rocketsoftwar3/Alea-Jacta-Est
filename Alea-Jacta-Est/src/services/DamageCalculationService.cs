using System;
using System.Collections.Generic;
using System.Linq;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Services;

public class DamageCalculationService
{
    /// <summary>
    /// Calculates total damage from a collection of played cards.
    /// Formula per suit:
    ///   numberedSum = Σ DamageValue of numbered ValueCards for this suit
    ///   faceMultiplier = Π Multiplier of each face ValueCard of this suit (1.0 if none)
    ///   suitTotal = (int)(numberedSum × faceMultiplier)
    /// Total = Σ suitTotal across all suits
    /// Non-ValueCards (ArcanaCards) are ignored here — their effects are handled by EffectManager.
    /// </summary>
    public int CalculateDamage(IEnumerable<Card> cards)
    {
        return CalculateScore(cards);
    }

    /// <summary>Calculates money earned from cards remaining in hand. Same formula as damage.</summary>
    public int CalculateMoney(IEnumerable<Card> handCards)
    {
        return CalculateScore(handCards);
    }

    private static int CalculateScore(IEnumerable<Card> cards)
    {
        var valueCards = cards.OfType<ValueCard>().ToList();
        int total = 0;

        foreach (CardSuit suit in Enum.GetValues<CardSuit>())
        {
            var suitCards = valueCards.Where(c => c.Suit == suit).ToList();
            if (suitCards.Count == 0) continue;

            int numberedSum = suitCards
                .Where(c => !c.IsFaceCard)
                .Sum(c => c.DamageValue);

            float faceMultiplier = suitCards
                .Where(c => c.IsFaceCard)
                .Aggregate(1f, (acc, c) => acc * c.Multiplier);

            total += (int)(numberedSum * faceMultiplier);
        }

        return total;
    }
}
