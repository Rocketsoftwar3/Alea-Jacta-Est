using System;
using System.Collections.Generic;
using System.Linq;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Services;

public class DamageCalculationService
{
    /// <summary>
    /// Calculates total damage from played cards.
    /// <paramref name="faceMultiplierBoost"/> multiplies all face-card multipliers (1f = normal, 2f = Empereur endroit).
    /// Formula per suit:
    ///   numberedSum = Σ DamageValue of numbered cards for this suit
    ///   faceMultiplier = Π (card.Multiplier × faceMultiplierBoost) for face cards of this suit
    ///   suitTotal = (int)(numberedSum × faceMultiplier)
    /// </summary>
    public int CalculateDamage(IEnumerable<Card> cards, float faceMultiplierBoost = 1f)
    {
        return CalculateScore(cards, faceMultiplierBoost);
    }

    /// <summary>Calculates money from cards remaining in hand. Same formula as damage.</summary>
    public int CalculateMoney(IEnumerable<Card> handCards)
    {
        return CalculateScore(handCards, 1f);
    }

    private static int CalculateScore(IEnumerable<Card> cards, float faceMultiplierBoost)
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
                .Aggregate(1f, (acc, c) => acc * (c.Multiplier * faceMultiplierBoost));

            total += (int)(numberedSum * faceMultiplier);
        }

        return total;
    }
}
