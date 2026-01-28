namespace Alea_Jacta_Est.Effects;

using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

/// <summary>Represents an effect that can be applied when a card is played.</summary>
public interface ICardEffect
{
    /// <summary>Resolves the effect in the given game context.</summary>
    void Resolve(GameContext context, Card card);
}
