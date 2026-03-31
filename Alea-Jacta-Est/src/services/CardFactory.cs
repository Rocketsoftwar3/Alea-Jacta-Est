using System;
using System.Linq;
using System.Reflection;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Main;
using Microsoft.Xna.Framework.Graphics;

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
            .Upright(isUpright)
            .WithDescription(
                "+10 pieces, pioche une carte bonus, rabais aleatoire par carte en boutique",
                "Supprime une carte face-cachee d'un adversaire (si arcanique, Le Bateleur est aussi supprime)");
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreatePapesse(bool isUpright = true)
    {
        return ArcanaCard.Use(2, "La Papesse", new PapesseEffect())
            .TextureRecto("cards/tarot_papesse")
            .WithPrice(20)
            .Upright(isUpright)
            .WithDescription(
                "Pendant 3 tours, ta main est preservee (pas de defausse) et tu gagnes +5 pieces/tour",
                "Tous les adversaires voient tes cartes uniquement par leur valeur monetaire");
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateImperatrice(bool isUpright = true)
    {
        return ArcanaCard.Use(3, "L'Imperatrice", new ImperatriceEffect())
            .TextureRecto("cards/tarot_imperatrice")
            .WithPrice(18)
            .Upright(isUpright)
            .WithDescription(
                "Voir la main complete de tous les joueurs pendant 1 tour",
                "Piocher une carte aleatoire depuis la defausse arcanique au prochain tour");
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateEmpereur(bool isUpright = true)
    {
        return ArcanaCard.Use(4, "L'Empereur", new EmpereurEffect())
            .TextureRecto("cards/tarot_empereur")
            .WithPrice(22)
            .Upright(isUpright)
            .WithDescription(
                "Double les multiplicateurs de toutes tes cartes a face ce tour",
                "Vole 1/3 des degats d'un adversaire cible (arrondi superieur)");
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreatePape(bool isUpright = true)
    {
        return ArcanaCard.Use(5, "Le Pape", new PapeEffect())
            .TextureRecto("cards/tarot_pape")
            .WithPrice(18)
            .Upright(isUpright)
            .WithDescription(
                "Soigne du montant des degats infliges aux adversaires ce tour",
                "Renvoie la moitie des degats subis a l'ennemi qui les a infliges");
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateAmoureux(bool isUpright = true)
    {
        return ArcanaCard.Use(6, "L'Amoureux", new AmoureuxEffect())
            .TextureRecto("cards/tarot_amoureux")
            .WithPrice(20)
            .Upright(isUpright)
            .WithDescription(
                "Perd 1/4 de PV mais choisit une carte arcanique qui apparait en boutique ce tour",
                "Designe un joueur dont le tour sera replique integralement au tour suivant");
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateChariot(bool isUpright = true)
    {
        return ArcanaCard.Use(7, "Le Chariot", new ChariotEffect())
            .TextureRecto("cards/tarot_chariot")
            .WithPrice(25)
            .Upright(isUpright)
            .WithDescription(
                "Tous les adversaires sont limites a 1 carte arcanique au tour suivant",
                "L'adversaire cible doit deposer toutes ses cartes a points (seules les arcaniques restent)");
    }

    [CardCreator]
    public static ArcanaCard.ArcanaCardBuilder CreateMort(bool isUpright = true)
    {
        return ArcanaCard.Use(13, "La Mort", new MortEffect())
            .TextureRecto("cards/tarot_mort")
            .WithPrice(30)
            .Upright(isUpright)
            .WithDescription(
                "Defausse la main ciblee et repioche autant de cartes (arcaniques posees restent)",
                "Adversaire cible pioche moitie moins de cartes a points + 1 arcanique au tour suivant");
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

    /// <summary>Returns the content path for a suit-specific recto texture, or null to use the placeholder.</summary>
    private static string? GetValueCardTexturePath(CardSuit suit, CardRank rank)
    {
        if (suit != CardSuit.Bâton) return null;
        string? rankPrefix = rank switch
        {
            CardRank.Un       => "1",
            CardRank.Deux     => "2",
            CardRank.Trois    => "3",
            CardRank.Quatre   => "4",
            CardRank.Cinq     => "5",
            CardRank.Six      => "6",
            CardRank.Sept     => "7",
            CardRank.Huit     => "8",
            CardRank.Neuf     => "9",
            CardRank.Dix      => "10",
            CardRank.Cavalier => "chevalier",
            CardRank.Reine    => "reine",
            CardRank.Roi      => "roi",
            _                 => null
        };
        return rankPrefix != null ? $"cards/stick/{rankPrefix}Baton" : null;
    }

    /// <summary>Creates a ValueCard using the suit-specific texture when available, falling back to the generic placeholder.</summary>
    public ValueCard CreateValueCard(CardSuit suit, CardRank rank)
    {
        var path  = GetValueCardTexturePath(suit, rank);
        var recto = path != null
            ? _gfx.Content.Load<Texture2D>(path)
            : _gfx.CardRectoTexture;
        return new ValueCard(suit, rank, recto, _gfx.CardVersoTexture);
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
