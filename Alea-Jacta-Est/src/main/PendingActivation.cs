using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Main;

/// <summary>Holds an arcana card activation that requires a target to be selected.</summary>
public class PendingActivation
{
    public ArcanaCard Card { get; }
    public Player Activator { get; }
    public Player? SelectedTarget { get; set; }

    public PendingActivation(ArcanaCard card, Player activator)
    {
        Card = card;
        Activator = activator;
    }
}
