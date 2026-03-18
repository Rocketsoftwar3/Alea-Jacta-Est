using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Events;

public record CardPurchased(Player Buyer, Card Card, int Price);
public record CardPlayed(Player Player, Card Card);
public record CardFlipped(Card Card);
public record TurnAdvanced(int NewPlayerIndex);
public record EffectApplied(Card Source, ICardEffect Effect);
public record EffectExpired(Card Source, ICardEffect Effect);
