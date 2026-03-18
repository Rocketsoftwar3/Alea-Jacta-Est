using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

public interface ICardEffect
{
    void OnPlay(GameState state, Card card);
    void OnTurnStart(GameState state, Card card) { }
    void OnTurnEnd(GameState state, Card card) { }
    void OnRemove(GameState state, Card card) { }
    int Duration => 0; // 0 = one-shot, >0 = number of turns
}
