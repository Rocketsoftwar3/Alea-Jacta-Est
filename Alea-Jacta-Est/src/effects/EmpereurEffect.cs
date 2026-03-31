using System;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// L'Empereur - IV
/// Endroit (1 tour) : Double les multiplicateurs des cartes à face pour ce tour.
/// Envers           : Cible un joueur et lui vole 1/3 de ses dégâts (arrondi supérieur).
///                    Ces dégâts sont soustraits de la cible et ajoutés au joueur.
///                    Résolu pendant la phase de résolution par TurnService.
/// </summary>
public class EmpereurEffect : ICardEffect
{
    public int Duration => 1;

    public bool RequiresTarget(ArcanaCard card) => !card.IsUpright;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        var activator = state.PendingActivation?.Activator ?? state.CurrentPlayer;
        if (activator == null) return;

        int activatorIdx = state.Players.IndexOf(activator);
        if (!state.TurnStates.TryGetValue(activatorIdx, out var ts)) return;

        if (card.IsUpright)
        {
            ts.MultiplierDoubled = true;
        }
        else
        {
            // Envers: store target index so TurnService can steal damage at resolution
            var target = state.PendingActivation?.SelectedTarget;
            if (target == null) return;

            int targetIdx = state.Players.IndexOf(target);
            ts.DamageStealTargetIndex = targetIdx;
        }
    }

    public void OnRemove(GameState state, ArcanaCard card)
    {
        // MultiplierDoubled is consumed by DamageCalculationService during resolution
    }
}
