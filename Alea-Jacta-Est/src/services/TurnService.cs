using System;
using System.Collections.Generic;
using System.Linq;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class TurnService
{
    private readonly EffectManager _effectManager;
    private readonly DamageCalculationService _damageCalc;
    private readonly CardFactory _cardFactory;
    private readonly EventBus _events;

    public TurnService(EffectManager effectManager, DamageCalculationService damageCalc, CardFactory cardFactory, EventBus events)
    {
        _effectManager = effectManager;
        _damageCalc = damageCalc;
        _cardFactory = cardFactory;
        _events = events;
    }

    // ── Draw Phase ───────────────────────────────────────────────────────────

    public void ExecuteDrawPhase(GameState state)
    {
        if (state.CurrentTurnPhase != TurnPhase.DrawPhase) return;

        // Restock market with fresh cards each turn
        foreach (var player in state.Players)
        {
            player.Market.Deck.Cards.Clear();
            for (int j = 0; j < 12; j++)
                player.Market.Deck.AddCard(_cardFactory.BuildRandom());
        }

        _effectManager.OnTurnStart(state);

        for (int i = 0; i < state.Players.Count; i++)
        {
            var player = state.Players[i];
            var ts = state.TurnStates[i];

            // Chariot envers: auto-place all value cards on board at start of turn
            if (ts.MustPlayAllPointCards)
            {
                AutoPlayAllPointCards(player);
                ts.MustPlayAllPointCards = false;
            }

            // Mort envers: draw fewer point cards, optionally skip arcana
            int pointCardsToDraw = 5 - ts.BlockedPointCardDraws;
            ts.BlockedPointCardDraws = 0;

            bool skipArcana = ts.BlockArcanaNextTurn;
            ts.BlockArcanaNextTurn = false;

            DrawValueCards(player, Math.Max(0, pointCardsToDraw));

            // Impératrice envers: draw from arcana discard instead of special deck
            if (ts.DrawFromArcanaDiscard)
            {
                DrawArcanaCardFromDiscard(player);
                ts.DrawFromArcanaDiscard = false;
            }
            else if (!skipArcana)
            {
                DrawArcanaCard(player);
            }
        }

        // Reset active player to first alive player at the start of each play phase.
        state.CurrentPlayerIndex = 0;
        while (state.CurrentPlayerIndex < state.Players.Count
               && state.Players[state.CurrentPlayerIndex].Health <= 0)
            state.CurrentPlayerIndex++;

        state.AdvanceTurnPhase(); // DrawPhase → PlayPhase
        _events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }

    // ── Resolution Phase ─────────────────────────────────────────────────────

    public void ExecuteResolutionPhase(GameState state)
    {
        if (state.CurrentTurnPhase != TurnPhase.ResolutionPhase) return;

        AutoValidateAIPlayers(state);

        var ctx = new ResolutionContext { State = state };
        CalculateRawDamage(ctx);
        ApplyEmpereurDamageSteal(ctx);
        DistributeRelativeDamage(ctx);
        ApplyHpChanges(ctx);
        ApplyPapeReflection(ctx);
        ApplyPapeHealing(ctx);
        StoreReplicatedDamage(ctx);
        AccumulateTotalDamageDealt(ctx);

        _effectManager.OnTurnEnd(state);

        // Amoureux endroit: add a random arcana card to the player's market
        for (int i = 0; i < state.Players.Count; i++)
        {
            if (state.TurnStates.TryGetValue(i, out var ats) && ats.AmoureuxShopPending)
            {
                state.Players[i].Market.Deck.AddCard(_cardFactory.BuildRandom());
                ats.AmoureuxShopPending = false;
            }
        }

        PublishResolutionResults(ctx);
    }

    private static void AutoValidateAIPlayers(GameState state)
    {
        if (!state.IsSinglePlayer) return;
        for (int i = 0; i < state.Players.Count; i++)
        {
            if (!state.Players[i].IsLocalPlayer)
                state.TurnStates[i].HasValidated = true;
        }
    }

    private void CalculateRawDamage(ResolutionContext ctx)
    {
        var state = ctx.State;
        for (int i = 0; i < state.Players.Count; i++)
        {
            var player = state.Players[i];
            var ts = state.TurnStates[i];

            bool hasRealCards = !state.IsSinglePlayer || player.IsLocalPlayer;

            if (hasRealCards)
            {
                if (ts.ReplicatePreviousTurn)
                {
                    ts.DamageTotal = ts.ReplicatedDamageTotal;
                    ts.ReplicatePreviousTurn = false;
                }
                else
                {
                    float boost = ts.MultiplierDoubled ? 2f : 1f;
                    ts.MultiplierDoubled = false;
                    ts.DamageTotal = _damageCalc.CalculateDamage(player.Decks[DeckType.BoardDeck0].Cards, boost);
                    ts.MoneyEarned = _damageCalc.CalculateMoney(player.Decks[DeckType.HandDeck].Cards);
                }
            }
            else
            {
                ts.DamageTotal = 0;
                ts.MoneyEarned = 0;
            }
        }
    }

    private static void ApplyEmpereurDamageSteal(ResolutionContext ctx)
    {
        var state = ctx.State;
        for (int i = 0; i < state.Players.Count; i++)
        {
            var ts = state.TurnStates[i];
            if (ts.DamageStealTargetIndex < 0) continue;

            int targetIdx = ts.DamageStealTargetIndex;
            ts.DamageStealTargetIndex = -1;

            if (!state.TurnStates.TryGetValue(targetIdx, out var targetTs)) continue;

            int stolen = (int)Math.Ceiling(targetTs.DamageTotal / 3.0);
            targetTs.DamageTotal = Math.Max(0, targetTs.DamageTotal - stolen);
            ts.DamageTotal += stolen;
        }
    }

    private static void DistributeRelativeDamage(ResolutionContext ctx)
    {
        var state = ctx.State;
        ctx.MaxDamage = state.TurnStates.Values.Max(ts => ts.DamageTotal);

        for (int i = 0; i < state.Players.Count; i++)
            ctx.DamageTaken[i] = Math.Max(0, ctx.MaxDamage - state.TurnStates[i].DamageTotal);
    }

    private static void ApplyHpChanges(ResolutionContext ctx)
    {
        var state = ctx.State;
        for (int i = 0; i < state.Players.Count; i++)
        {
            state.Players[i].Health -= ctx.DamageTaken[i];
            state.Players[i].Wallet += state.TurnStates[i].MoneyEarned;
        }
    }

    private static void ApplyPapeReflection(ResolutionContext ctx)
    {
        var state = ctx.State;
        ctx.AttackerIndex = Array.IndexOf(
            Enumerable.Range(0, state.Players.Count)
                .Select(i => state.TurnStates[i].DamageTotal)
                .ToArray(),
            ctx.MaxDamage);

        for (int i = 0; i < state.Players.Count; i++)
        {
            if (!state.TurnStates[i].ReflectDamage) continue;
            if (ctx.DamageTaken[i] <= 0) continue;

            int reflected = ctx.DamageTaken[i] / 2;
            if (ctx.AttackerIndex >= 0 && ctx.AttackerIndex < state.Players.Count && ctx.AttackerIndex != i)
                state.Players[ctx.AttackerIndex].Health -= reflected;
        }
    }

    private static void ApplyPapeHealing(ResolutionContext ctx)
    {
        var state = ctx.State;
        for (int i = 0; i < state.Players.Count; i++)
        {
            if (!state.TurnStates[i].HealFromDamageDealt) continue;

            int healAmount = 0;
            for (int j = 0; j < state.Players.Count; j++)
                if (j != i) healAmount += ctx.DamageTaken[j];

            state.Players[i].Health += healAmount;
        }
    }

    private static void StoreReplicatedDamage(ResolutionContext ctx)
    {
        var state = ctx.State;
        for (int i = 0; i < state.Players.Count; i++)
            state.TurnStates[i].ReplicatedDamageTotal = state.TurnStates[i].DamageTotal;
    }

    private static void AccumulateTotalDamageDealt(ResolutionContext ctx)
    {
        var state = ctx.State;
        for (int i = 0; i < state.Players.Count; i++)
            state.TurnStates[i].TotalDamageDealt += state.TurnStates[i].DamageTotal;
    }

    private void PublishResolutionResults(ResolutionContext ctx)
    {
        var state = ctx.State;

        _events.Publish(new RoundResolved(
            Enumerable.Range(0, state.Players.Count)
                .ToDictionary(i => state.Players[i], i => ctx.DamageTaken[i])));

        foreach (var player in state.Players.ToList())
            if (player.Health <= 0)
                _events.Publish(new PlayerEliminated(player));

        if (state.CheckVictory()) return;

        state.AdvanceTurnPhase(); // ResolutionPhase → CleanupPhase
        _events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }

    // ── Cleanup Phase ────────────────────────────────────────────────────────

    public void ExecuteCleanupPhase(GameState state)
    {
        if (state.CurrentTurnPhase != TurnPhase.CleanupPhase) return;

        for (int i = 0; i < state.Players.Count; i++)
        {
            var player = state.Players[i];
            var hand = player.Decks[DeckType.HandDeck];
            var board = player.Decks[DeckType.BoardDeck0];
            var discard = player.Decks[DeckType.DiscardDeck];

            discard.AddCards(board.Cards.ToList());
            board.Cards.Clear();

            // Papesse endroit: SkipHandDiscard preserves the hand
            bool skipHand = state.TurnStates.TryGetValue(i, out var ts) && ts.SkipHandDiscard;
            if (!skipHand)
            {
                discard.AddCards(hand.Cards.ToList());
                hand.Cards.Clear();
            }
        }

        state.AdvanceTurnPhase(); // CleanupPhase → ShopPhase
        _events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void DrawValueCards(Player player, int count)
    {
        var mainDeck = player.Decks[DeckType.MainDeck];
        var hand = player.Decks[DeckType.HandDeck];
        var discard = player.Decks[DeckType.DiscardDeck];

        if (mainDeck.Cards.Count < count && discard.Cards.Count > 0)
        {
            mainDeck.AddCards(discard.Cards.ToList());
            discard.Cards.Clear();
            Shuffle(mainDeck.Cards);
        }

        int toDraw = Math.Min(count, mainDeck.Cards.Count);
        for (int i = 0; i < toDraw; i++)
        {
            var card = mainDeck.DrawCard();
            if (card != null) hand.AddCard(card);
        }
    }

    private static void DrawArcanaCard(Player player)
    {
        var specialDeck = player.Decks[DeckType.SpecialDeck];
        var arcanaHand = player.Decks[DeckType.ArcanaHandDeck];
        var arcanaDiscard = player.Decks[DeckType.ArcanaDiscardDeck];

        if (specialDeck.Cards.Count == 0 && arcanaDiscard.Cards.Count > 0)
        {
            specialDeck.AddCards(arcanaDiscard.Cards.ToList());
            arcanaDiscard.Cards.Clear();
            Shuffle(specialDeck.Cards);
        }

        var card = specialDeck.DrawCard();
        if (card != null) arcanaHand.AddCard(card);
    }

    private static void DrawArcanaCardFromDiscard(Player player)
    {
        var arcanaDiscard = player.Decks[DeckType.ArcanaDiscardDeck];
        var arcanaHand = player.Decks[DeckType.ArcanaHandDeck];

        if (arcanaDiscard.Cards.Count == 0)
        {
            // Fallback to normal draw if discard is empty
            DrawArcanaCard(player);
            return;
        }

        // Pick a random card from arcana discard
        var rng = new Random();
        int idx = rng.Next(arcanaDiscard.Cards.Count);
        var card = arcanaDiscard.Cards[idx];
        arcanaDiscard.Cards.RemoveAt(idx);
        arcanaHand.AddCard(card);
    }

    /// <summary>Chariot envers: auto-move all value cards from hand to board.</summary>
    private static void AutoPlayAllPointCards(Player player)
    {
        var hand = player.Decks[DeckType.HandDeck];
        var board = player.Decks[DeckType.BoardDeck0];

        var pointCards = hand.Cards.OfType<ValueCard>().Where(c => !c.IsFaceCard).ToList();
        foreach (var card in pointCards)
        {
            hand.RemoveCard(card);
            board.AddCard(card);
        }
    }

    private static readonly Random _rng = new();
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
