using System.Collections.Generic;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Events;

// --- Card events ---
public record CardPurchased(Player Buyer, Card Card, int Price);
public record CardSold(Player Seller, Card Card, int Price);
public record CardPlayed(Player Player, Card Card);
public record CardFlipped(Card Card);
public record CardPlacedOnBoard(Player Player, Card Card);
public record CardTakenBack(Player Player, Card Card);

// --- Turn events ---
public record TurnAdvanced(int NewPlayerIndex);
public record TurnPhaseChanged(TurnPhase NewPhase);
public record TurnValidated(Player Player);
public record RoundResolved(IReadOnlyDictionary<Player, int> Damages);
public record PlayerEliminated(Player Player);

// --- Network events ---
public record PlayerDisconnected(int PlayerIndex, string PlayerName);
public record NetworkDisconnected(string Reason);

// --- Menu events ---
public record ReturnedToMenu();

// --- Effect events ---
public record EffectApplied(Card Source, ICardEffect Effect);
public record EffectExpired(Card Source, ICardEffect Effect);
