using System;

namespace Alea_Jacta_Est.Entities;

/// <summary>Represents a player's market with available cards and discount.</summary>
public class Market
{
    /// <summary>Cards available for purchase in this market.</summary>
    public Deck Deck { get; }

    /// <summary>Discount percentage (0.0 to 1.0). 0.1 = 10% discount.</summary>
    public float Discount { get; set; }

    public Market()
    {
        Deck = new Deck();
        Discount = 0f;
    }

    /// <summary>Calculates the final price after applying discount.</summary>
    public int GetDiscountedPrice(int basePrice)
    {
        return (int)(basePrice * (1f - Discount));
    }

    /// <summary>Adds a flat discount amount (in coins) to the market.</summary>
    public void AddFlatDiscount(int discountAmount, int basePrice)
    {
        if (basePrice <= 0) return;
        float additionalDiscount = (float)discountAmount / basePrice;
        Discount = Math.Min(1f, Discount + additionalDiscount);
    }
}
