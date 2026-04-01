using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Commands;

/// <summary>Executes an arcana activation after a target has been selected.</summary>
public record ActivateArcanaWithTargetCommand(
    Player Activator,
    ArcanaCard Card,
    Player Target,
    EffectManager EffectManager) : IGameCommand
{
    public void Execute(GameState state, EventBus events)
    {
        if (state.PendingActivation == null) return;

        // Store selected target so effects can read it via state.PendingActivation.SelectedTarget
        state.PendingActivation.SelectedTarget = Target;

        Activator.Decks[DeckType.ArcanaHandDeck].RemoveCard(Card);
        Activator.Decks[DeckType.ArcanaDiscardDeck].AddCard(Card);

        EffectManager.PlayCard(state, Card);
        events.Publish(new CardPlayed(Activator, Card));

        state.PendingActivation = null;
    }
}
