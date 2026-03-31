namespace Alea_Jacta_Est.Main;

/// <summary>Per-player state that resets each round.</summary>
public class PlayerTurnState
{
    public bool HasValidated { get; set; }
    public int DamageTotal { get; set; }
    public int MoneyEarned { get; set; }
    public bool HasSoldThisRound { get; set; }

    public void Reset()
    {
        HasValidated = false;
        DamageTotal = 0;
        MoneyEarned = 0;
        HasSoldThisRound = false;
    }
}
