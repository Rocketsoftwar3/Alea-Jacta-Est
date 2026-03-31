using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// L'Impératrice - III
/// Endroit (1 tour) : Le joueur verra la main de tous les autres joueurs au prochain tour.
/// Envers  (1 tour) : Tire une carte aléatoire de la défausse arcanique au prochain tour.
///                    L'Impératrice peut être piochée dans la défausse arcanique.
/// </summary>
public class ImperatriceEffect : ICardEffect
{
    public int Duration => 1;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        var player = state.CurrentPlayer;
        if (player == null) return;

        int idx = state.Players.IndexOf(player);
        if (!state.TurnStates.TryGetValue(idx, out var ts)) return;

        if (card.IsUpright)
            ts.CanSeeOpponentHands = true;
        else
            ts.DrawFromArcanaDiscard = true;
    }

    public void OnRemove(GameState state, ArcanaCard card)
    {
        var player = state.CurrentPlayer;
        if (player == null) return;

        int idx = state.Players.IndexOf(player);
        if (!state.TurnStates.TryGetValue(idx, out var ts)) return;

        if (card.IsUpright)
            ts.CanSeeOpponentHands = false;
        // DrawFromArcanaDiscard is consumed at draw time — no need to clear
    }
}
