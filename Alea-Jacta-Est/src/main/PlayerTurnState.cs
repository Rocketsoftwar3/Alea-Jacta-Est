namespace Alea_Jacta_Est.Main;

/// <summary>Per-player state that resets each round.</summary>
public class PlayerTurnState
{
    // ── Standard round state ─────────────────────────────────────────────────
    public bool HasValidated { get; set; }
    public int DamageTotal { get; set; }
    public int MoneyEarned { get; set; }
    public bool HasSoldThisRound { get; set; }

    /// <summary>Multiplayer: true once this player has clicked "Fin de boutique".</summary>
    public bool HasEndedShop { get; set; }

    /// <summary>Cumulative damage dealt across ALL turns (never reset).</summary>
    public int TotalDamageDealt { get; set; }

    // ── Empereur ─────────────────────────────────────────────────────────────
    /// <summary>Endroit: double face-card multipliers for this turn's damage calc.</summary>
    public bool MultiplierDoubled { get; set; }

    /// <summary>Envers: index of the opponent to steal 1/3 damage from (-1 = none).</summary>
    public int DamageStealTargetIndex { get; set; } = -1;

    // ── Pape ─────────────────────────────────────────────────────────────────
    /// <summary>Endroit: after HP changes, heal this player by total damage dealt to opponents.</summary>
    public bool HealFromDamageDealt { get; set; }

    /// <summary>Envers: reflect half of incoming damage back to attacker.</summary>
    public bool ReflectDamage { get; set; }

    // ── Impératrice ──────────────────────────────────────────────────────────
    /// <summary>Endroit: this player can see all opponents' hands next turn.</summary>
    public bool CanSeeOpponentHands { get; set; }

    /// <summary>Envers: next draw, pull 1 extra card from ArcanaDiscardDeck instead of SpecialDeck.</summary>
    public bool DrawFromArcanaDiscard { get; set; }

    // ── Papesse ───────────────────────────────────────────────────────────────
    /// <summary>Envers: this player's hand is shown only as monetary values to opponents.</summary>
    public bool HandVisibleAsMoneyOnly { get; set; }

    // ── Chariot ───────────────────────────────────────────────────────────────
    /// <summary>Endroit: max arcana activations this turn (default int.MaxValue = unlimited).</summary>
    public int MaxArcanasPerTurn { get; set; } = int.MaxValue;

    /// <summary>Tracks how many arcanics have been activated this turn.</summary>
    public int ArcanasPlayedThisTurn { get; set; }

    /// <summary>Envers: player must auto-place all value cards on the board at draw time.</summary>
    public bool MustPlayAllPointCards { get; set; }

    // ── Mort ─────────────────────────────────────────────────────────────────
    /// <summary>Envers: skip this many point-card draws next turn.</summary>
    public int BlockedPointCardDraws { get; set; }

    /// <summary>Envers: skip arcana draw next turn.</summary>
    public bool BlockArcanaNextTurn { get; set; }

    // ── Amoureux ─────────────────────────────────────────────────────────────
    /// <summary>Envers: this player's damage next resolution = ReplicatedDamageTotal.</summary>
    public bool ReplicatePreviousTurn { get; set; }

    /// <summary>Stored damage total to replay (set at end of current resolution).</summary>
    public int ReplicatedDamageTotal { get; set; }

    // ─────────────────────────────────────────────────────────────────────────

    public void Reset()
    {
        HasValidated = false;
        DamageTotal = 0;
        MoneyEarned = 0;
        HasSoldThisRound = false;
        HasEndedShop = false;
        // TotalDamageDealt is cumulative — never reset

        MultiplierDoubled = false;
        DamageStealTargetIndex = -1;
        HealFromDamageDealt = false;
        ReflectDamage = false;
        CanSeeOpponentHands = false;
        DrawFromArcanaDiscard = false;
        HandVisibleAsMoneyOnly = false;
        MaxArcanasPerTurn = int.MaxValue;
        ArcanasPlayedThisTurn = 0;
        MustPlayAllPointCards = false;
        BlockedPointCardDraws = 0;
        BlockArcanaNextTurn = false;
        ReplicatePreviousTurn = false;
        ReplicatedDamageTotal = 0;
    }
}
