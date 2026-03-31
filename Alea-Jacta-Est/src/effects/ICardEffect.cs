using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

public interface ICardEffect
{
    void OnPlay(GameState state, ArcanaCard card);
    void OnTurnStart(GameState state, ArcanaCard card) { }
    void OnTurnEnd(GameState state, ArcanaCard card) { }
    void OnRemove(GameState state, ArcanaCard card) { }
    int Duration => 0; // 0 = one-shot, >0 = number of turns

    /// <summary>Returns true when this effect needs a target before OnPlay is called.
    /// The selected target will be available via state.PendingActivation.SelectedTarget.</summary>
    bool RequiresTarget(ArcanaCard card) => false;
}
