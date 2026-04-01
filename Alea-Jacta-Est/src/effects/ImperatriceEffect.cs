using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// L'Impératrice - III
/// Endroit (2 tours) : Le joueur verra la main de tous les autres joueurs au prochain tour.
/// Envers  (2 tours) : Tire une carte aléatoire de la défausse arcanique au prochain tour.
///                    L'Impératrice peut être piochée dans la défausse arcanique.
/// </summary>
public class ImperatriceEffect : ICardEffect
{
    public int Duration => 2;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        // Endroit: flag set in OnTurnStart (next turn) instead of here
        // Envers: flag set here but consumed at next draw phase (survives Reset)
        if (!card.IsUpright)
        {
            var player = state.CurrentPlayer;
            if (player == null) return;

            int idx = state.Players.IndexOf(player);
            if (state.TurnStates.TryGetValue(idx, out var ts))
                ts.DrawFromArcanaDiscard = true;
        }
    }

    public void OnTurnStart(GameState state, ArcanaCard card)
    {
        if (!card.IsUpright) return;

        var player = state.CurrentPlayer;
        if (player == null) return;

        int idx = state.Players.IndexOf(player);
        if (state.TurnStates.TryGetValue(idx, out var ts))
            ts.CanSeeOpponentHands = true;
    }

    public void OnRemove(GameState state, ArcanaCard card)
    {
        if (!card.IsUpright) return;

        var player = state.CurrentPlayer;
        if (player == null) return;

        int idx = state.Players.IndexOf(player);
        if (state.TurnStates.TryGetValue(idx, out var ts))
            ts.CanSeeOpponentHands = false;
    }
}
