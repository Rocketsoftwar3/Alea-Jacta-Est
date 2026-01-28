using System;
using System.Linq;
using System.Reflection;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Effects;

namespace Alea_Jacta_Est.Services;

/// <summary>Factory for creating game card builders.</summary>
public static class CardFactory
{
    private static readonly Random _random = new Random();

    /// <summary>Creates Le Bateleur card builder.</summary>
    [CardCreator]
    public static Card.CardBuilder CreateBateleur(bool isUpright = true)
    {
        return Card.Use(new BateleurEffect())
            .TextureRecto("cards/tarot_bateleur")
            .WithPrice(15)
            .Upright(isUpright);
    }

    /// <summary>Creates La Papesse card builder.</summary>
    [CardCreator]
    public static Card.CardBuilder CreatePapesse(bool isUpright = true)
    {
        return Card.Use(new PapesseEffect())
            .TextureRecto("cards/tarot_papesse")
            .WithPrice(20)
            .Upright(isUpright);
    }

    /// <summary>Creates a random card builder using reflection on [CardCreator] methods.</summary>
    public static Card.CardBuilder CreateRandom(bool isUpright = true)
    {
        // Get all methods marked with [CardCreator] attribute
        var createMethods = typeof(CardFactory)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<CardCreatorAttribute>() != null)
            .ToArray();

        if (createMethods.Length == 0)
            throw new InvalidOperationException("No [CardCreator] methods found.");

        // Select random method
        var randomMethod = createMethods[_random.Next(createMethods.Length)];

        // Invoke with isUpright parameter
        return (Card.CardBuilder)randomMethod.Invoke(null, new object[] { isUpright })!;
    }

    // TODO: Ajouter les cartes "normales" (non-arcaniques) pour les bonus
}
