using System;
using System.Linq;
using System.Reflection;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Main;
using Microsoft.Xna.Framework.Content;

namespace Alea_Jacta_Est.Services;

public class CardFactory
{
    private static readonly Random _random = new();
    private static readonly MethodInfo[] _creatorMethods;

    private readonly ContentManager _content;
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
        _content = gfx.Content;
    }

    [CardCreator]
    public static Card.CardBuilder CreateBateleur(bool isUpright = true)
    {
        return Card.Use(new BateleurEffect())
            .TextureRecto("cards/tarot_bateleur")
            .WithPrice(15)
            .Upright(isUpright);
    }

    [CardCreator]
    public static Card.CardBuilder CreatePapesse(bool isUpright = true)
    {
        return Card.Use(new PapesseEffect())
            .TextureRecto("cards/tarot_papesse")
            .WithPrice(20)
            .Upright(isUpright);
    }

    public Card.CardBuilder CreateRandom(bool isUpright = true)
    {
        var randomMethod = _creatorMethods[_random.Next(_creatorMethods.Length)];
        return (Card.CardBuilder)randomMethod.Invoke(null, new object[] { isUpright })!;
    }

    public Card BuildRandom(bool isUpright = true)
    {
        return CreateRandom(isUpright).Build(_gfx);
    }
}
