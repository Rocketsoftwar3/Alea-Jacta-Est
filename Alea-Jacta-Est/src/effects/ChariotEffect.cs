using System.Linq;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// Le Chariot - VII
/// Endroit (1 tour) : Le tour suivant, tous les adversaires ne peuvent utiliser qu'une seule
///                    carte arcanique.
/// Envers           : L'adversaire ciblé est obligé de déposer toutes ses cartes à points sur
///                    le terrain au début de son prochain tour (seules les arcaniques restent
///                    manipulables).
///                    RequiresTarget = true.
/// </summary>
public class ChariotEffect : ICardEffect
{
    public int Duration => 1;

    public bool RequiresTarget(ArcanaCard card) => !card.IsUpright;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        var activator = state.PendingActivation?.Activator ?? state.CurrentPlayer;
        if (activator == null) return;

        int activatorIdx = state.Players.IndexOf(activator);

        if (card.IsUpright)
        {
            // Limiter à 1 arcanique pour tous les adversaires au prochain tour
            foreach (var (idx, ts) in state.TurnStates)
                if (idx != activatorIdx)
                    ts.MaxArcanasPerTurn = 1;
        }
        else
        {
            var target = state.PendingActivation?.SelectedTarget;
            if (target == null) return;

            int targetIdx = state.Players.IndexOf(target);
            if (state.TurnStates.TryGetValue(targetIdx, out var ts))
                ts.MustPlayAllPointCards = true;
        }
    }

    public void OnRemove(GameState state, ArcanaCard card)
    {
        if (!card.IsUpright) return;

        // Retirer la limite d'arcaniques (reset à illimité)
        foreach (var ts in state.TurnStates.Values)
            if (ts.MaxArcanasPerTurn == 1)
                ts.MaxArcanasPerTurn = int.MaxValue;
    }
}
