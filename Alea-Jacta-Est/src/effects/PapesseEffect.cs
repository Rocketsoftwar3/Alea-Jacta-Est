using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// La Papesse - II
/// Endroit (3 tours) : La main est préservée (pas de défausse) et +5 pièces/tour.
/// Envers  (1 tour)  : Tous les adversaires voient leurs cartes uniquement par
///                     leur valeur monétaire.
/// </summary>
public class PapesseEffect : ICardEffect
{
    public int Duration => 3;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        var player = state.CurrentPlayer;
        if (player == null) return;

        if (!card.IsUpright)
        {
            int localIdx = state.Players.IndexOf(player);
            foreach (var (idx, ts) in state.TurnStates)
                if (idx != localIdx)
                    ts.HandVisibleAsMoneyOnly = true;
        }
    }

    public void OnTurnEnd(GameState state, ArcanaCard card)
    {
        if (!card.IsUpright) return;

        var player = state.CurrentPlayer;
        if (player == null) return;

        // Flag cleanup phase to skip discarding this player's hand
        int playerIdx = state.Players.IndexOf(player);
        if (state.TurnStates.TryGetValue(playerIdx, out var ts))
            ts.SkipHandDiscard = true;

        // +5 pièces par tour
        player.Wallet += 5;
    }

    public void OnRemove(GameState state, ArcanaCard card)
    {
        if (!card.IsUpright)
        {
            foreach (var ts in state.TurnStates.Values)
                ts.HandVisibleAsMoneyOnly = false;
        }
    }
}
