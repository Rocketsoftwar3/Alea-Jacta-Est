namespace Alea_Jacta_Est.Entities;

public enum CardRank
{
    Un = 1,
    Deux,
    Trois,
    Quatre,
    Cinq,
    Six,
    Sept,
    Huit,
    Neuf,
    Dix,
    Valet,
    Cavalier,
    Reine,
    Roi
}

public static class CardRankExtensions
{
    /// <summary>Damage value for numbered cards (1-10). Face cards return 0.</summary>
    public static int DamageValue(this CardRank rank) => rank switch
    {
        CardRank.Un      => 1,
        CardRank.Deux    => 2,
        CardRank.Trois   => 3,
        CardRank.Quatre  => 4,
        CardRank.Cinq    => 5,
        CardRank.Six     => 6,
        CardRank.Sept    => 7,
        CardRank.Huit    => 8,
        CardRank.Neuf    => 9,
        CardRank.Dix     => 10,
        _                => 0  // Face cards contribute via multiplier, not direct damage
    };

    /// <summary>Damage multiplier applied to same-suit numbered cards total.</summary>
    public static float Multiplier(this CardRank rank) => rank switch
    {
        CardRank.Valet   => 1.5f,
        CardRank.Cavalier => 2f,
        CardRank.Reine   => 3f,
        CardRank.Roi     => 5f,
        _                => 1f
    };

    public static bool IsFaceCard(this CardRank rank) =>
        rank is CardRank.Valet or CardRank.Cavalier or CardRank.Reine or CardRank.Roi;

    public static string ToFrench(this CardRank rank) => rank switch
    {
        CardRank.Un      => "As",
        CardRank.Deux    => "2",
        CardRank.Trois   => "3",
        CardRank.Quatre  => "4",
        CardRank.Cinq    => "5",
        CardRank.Six     => "6",
        CardRank.Sept    => "7",
        CardRank.Huit    => "8",
        CardRank.Neuf    => "9",
        CardRank.Dix     => "10",
        CardRank.Valet   => "Valet",
        CardRank.Cavalier => "Cavalier",
        CardRank.Reine   => "Reine",
        CardRank.Roi     => "Roi",
        _                => rank.ToString()
    };
}
