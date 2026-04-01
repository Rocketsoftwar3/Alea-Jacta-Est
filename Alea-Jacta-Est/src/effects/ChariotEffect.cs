using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// Le Chariot - VII
/// Endroit (2 tours) : Le tour suivant, tous les adversaires ne peuvent utiliser qu'une seule
///                    carte arcanique.
/// Envers           : L'adversaire ciblé est obligé de déposer toutes ses cartes à points sur
///                    le terrain au début de son prochain tour (seules les arcaniques et les
///                    cartes à face restent manipulables).
///                    RequiresTarget = true.
/// </summary>
public class ChariotEffect : ICardEffect
{
    public int Duration => 2;

    public bool RequiresTarget(ArcanaCard card) => !card.IsUpright;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        if (card.IsUpright)
        {
            // Endroit: flag set in OnTurnStart (next turn) instead of here
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

    public void OnTurnStart(GameState state, ArcanaCard card)
    {
        if (!card.IsUpright) return;

        var activator = state.CurrentPlayer;
        if (activator == null) return;

        int activatorIdx = state.Players.IndexOf(activator);

        // Limiter à 1 arcanique pour tous les adversaires
        foreach (var (idx, ts) in state.TurnStates)
            if (idx != activatorIdx)
                ts.MaxArcanasPerTurn = 1;
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
