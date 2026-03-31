using Microsoft.Xna.Framework.Graphics;

namespace Alea_Jacta_Est.Entities;

/// <summary>A numbered or face card belonging to one of the four suits (minor arcana).</summary>
public class ValueCard : Card
{
    public CardSuit Suit { get; }
    public CardRank Rank { get; }

    public int DamageValue => Rank.DamageValue();
    public bool IsFaceCard => Rank.IsFaceCard();
    public float Multiplier => Rank.Multiplier();

    /// <summary>Display name e.g. "7 de Coupe" or "Roi d'Épée".</summary>
    public string DisplayName
    {
        get
        {
            string rankStr = Rank.ToFrench();
            string suitStr = Suit.ToString();
            string connector = Suit == CardSuit.Épée ? "d'" : "de ";
            return IsFaceCard ? $"{rankStr} {connector}{suitStr}" : $"{rankStr} {connector}{suitStr}";
        }
    }

    public ValueCard(CardSuit suit, CardRank rank, Texture2D recto, Texture2D verso)
        : base(recto, verso)
    {
        Suit = suit;
        Rank = rank;
    }
}
