using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// Le Pape - V
/// Endroit (0) : Soigne le joueur du montant des dégâts infligés à ses adversaires ce tour.
///               Résolu pendant la phase de résolution par TurnService.
/// Envers  (1) : Renvoie la moitié des dégâts subis à l'ennemi les ayant infligés.
///               Résolu pendant la phase de résolution par TurnService.
/// </summary>
public class PapeEffect : ICardEffect
{
    public int Duration => 1;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        var player = state.CurrentPlayer;
        if (player == null) return;

        int idx = state.Players.IndexOf(player);
        if (!state.TurnStates.TryGetValue(idx, out var ts)) return;

        if (card.IsUpright)
            ts.HealFromDamageDealt = true;
        else
            ts.ReflectDamage = true;
    }

    public void OnRemove(GameState state, ArcanaCard card)
    {
        // Flags are consumed by TurnService during resolution — no persistent state to clear
    }
}
