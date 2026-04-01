using System;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

/// <summary>
/// L'Amoureux - VI
/// Endroit : Le joueur perd 1/4 de ses PV (arrondi supérieur) et une carte arcanique aléatoire
///           apparaît dans sa boutique ce tour.
///           TODO: UI pour laisser le joueur CHOISIR quelle arcanique ajouter.
/// Envers  : Désigne quelqu'un — son tour sera répliqué au tour suivant
///           (ses dégâts de ce tour seront réutilisés lors de la prochaine résolution).
///           RequiresTarget = true (peut se cibler soi-même).
/// </summary>
public class AmoureuxEffect : ICardEffect
{
    private static readonly Random _rng = new();

    public int Duration => 0;

    public bool RequiresTarget(ArcanaCard card) => !card.IsUpright;

    public void OnPlay(GameState state, ArcanaCard card)
    {
        if (card.IsUpright)
            ResolveEndroit(state);
        else
            ResolveEnvers(state);
    }

    private static void ResolveEndroit(GameState state)
    {
        var player = state.CurrentPlayer;
        if (player == null) return;

        // Perd 1/4 de ses PV
        int loss = (int)Math.Ceiling(player.Health / 4.0);
        player.Health -= loss;

        // Signal TurnService to add a random arcana card to the market
        // TODO: laisser le joueur choisir l'arcanique via UI
        int idx = state.Players.IndexOf(player);
        if (state.TurnStates.TryGetValue(idx, out var ts))
            ts.AmoureuxShopPending = true;
    }

    private static void ResolveEnvers(GameState state)
    {
        var target = state.PendingActivation?.SelectedTarget;
        if (target == null) return;

        int targetIdx = state.Players.IndexOf(target);
        if (!state.TurnStates.TryGetValue(targetIdx, out var ts)) return;

        ts.ReplicatePreviousTurn = true;
        // ReplicatedDamageTotal will be set at end of this resolution in TurnService
    }
}
