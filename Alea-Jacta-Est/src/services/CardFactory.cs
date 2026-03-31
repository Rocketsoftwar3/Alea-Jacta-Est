using System;
using System.Linq;
using System.Reflection;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class CardFactory
{
    private static readonly Random _random = new();
    private static readonly MethodInfo[] _creatorMethods;

    private readonly GraphicsResources _gfx;

    static CardFactory()
    {
        _creatorMethods = typeof(CardFactory)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<CardCreatorAttribute>() != null)
            .ToArray();

        if (_creatorMethods.Length == 0)
            throw new InvalidOperationException("No [CardCreator] methods found.");
    }

    public CardFactory(GraphicsResources gfx)
    {
        _gfx = gfx;
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateBateleur(bool isUpright = true)
    {
        return ArcanaCard.Use(1, "Le Bateleur", new BateleurEffect())
            .TextureRecto("cards/tarot_bateleur")
            .WithPrice(15)
            .Upright(isUpright);
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreatePapesse(bool isUpright = true)
    {
        return ArcanaCard.Use(2, "La Papesse", new PapesseEffect())
            .TextureRecto("cards/tarot_papesse")
            .WithPrice(20)
            .Upright(isUpright);
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateImperatrice(bool isUpright = true)
    {
        return ArcanaCard.Use(3, "L'Impératrice", new ImperatriceEffect())
            .TextureRecto("cards/tarot_imperatrice")
            .WithPrice(18)
            .Upright(isUpright);
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateEmpereur(bool isUpright = true)
    {
        return ArcanaCard.Use(4, "L'Empereur", new EmpereurEffect())
            .TextureRecto("cards/tarot_empereur")
            .WithPrice(22)
            .Upright(isUpright);
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreatePape(bool isUpright = true)
    {
        return ArcanaCard.Use(5, "Le Pape", new PapeEffect())
            .TextureRecto("cards/tarot_pape")
            .WithPrice(18)
            .Upright(isUpright);
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateAmoureux(bool isUpright = true)
    {
        return ArcanaCard.Use(6, "L'Amoureux", new AmoureuxEffect())
            .TextureRecto("cards/tarot_amoureux")
            .WithPrice(20)
            .Upright(isUpright);
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateChariot(bool isUpright = true)
    {
        return ArcanaCard.Use(7, "Le Chariot", new ChariotEffect())
            .TextureRecto("cards/tarot_chariot")
            .WithPrice(25)
            .Upright(isUpright);
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateMort(bool isUpright = true)
    {
        return ArcanaCard.Use(13, "La Mort", new MortEffect())
            .TextureRecto("cards/tarot_mort")
            .WithPrice(30)
            .Upright(isUpright);
    }

    public ArcanaCard.ArcanaCardBuilder CreateRandomArcanaBuilder(bool isUpright = true)
    {
        var randomMethod = _creatorMethods[_random.Next(_creatorMethods.Length)];
        return (ArcanaCard.ArcanaCardBuilder)randomMethod.Invoke(null, new object[] { isUpright })!;
    }

    public ArcanaCard BuildRandom(bool isUpright = true)
    {
        return CreateRandomArcanaBuilder(isUpright).Build(_gfx);
    }

    /// <summary>Creates a ValueCard using the generic card recto texture.</summary>
    public ValueCard CreateValueCard(CardSuit suit, CardRank rank)
    {
        return new ValueCard(suit, rank, _gfx.CardRectoTexture, _gfx.CardVersoTexture);
    }

    /// <summary>Creates a random ValueCard with a random suit and rank.</summary>
    public ValueCard CreateRandomValueCard()
    {
        var suits = (CardSuit[])Enum.GetValues(typeof(CardSuit));
        var ranks = (CardRank[])Enum.GetValues(typeof(CardRank));
        var suit = suits[_random.Next(suits.Length)];
        var rank = ranks[_random.Next(ranks.Length)];
        return CreateValueCard(suit, rank);
    }
}
