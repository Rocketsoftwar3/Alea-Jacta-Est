using System;
using System.Collections.Generic;
using System.Linq;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class TurnService
{
    private readonly EffectManager _effectManager;
    private readonly DamageCalculationService _damageCalc;
    private readonly EventBus _events;

    public TurnService(EffectManager effectManager, DamageCalculationService damageCalc, EventBus events)
    {
        _effectManager = effectManager;
        _damageCalc = damageCalc;
        _events = events;
    }

    // ── Draw Phase ───────────────────────────────────────────────────────────

    public void ExecuteDrawPhase(GameState state)
    {
        if (state.CurrentTurnPhase != TurnPhase.DrawPhase) return;

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

        // Solo: auto-validate all non-local players (AI slots).
        if (state.IsSinglePlayer)
        {
            for (int i = 0; i < state.Players.Count; i++)
            {
                if (!state.Players[i].IsLocalPlayer)
                    state.TurnStates[i].HasValidated = true;
            }
        }

        // ── 1. Calculate raw damage per player ───────────────────────────────
        for (int i = 0; i < state.Players.Count; i++)
        {
            var player = state.Players[i];
            var ts = state.TurnStates[i];

            // In solo mode only the local player has real cards; other slots have none.
            bool hasRealCards = !state.IsSinglePlayer || player.IsLocalPlayer;

            if (hasRealCards)
            {
                // Amoureux envers replicated turn: use stored damage instead of recalculating.
                if (ts.ReplicatePreviousTurn)
                {
                    ts.DamageTotal = ts.ReplicatedDamageTotal;
                    ts.ReplicatePreviousTurn = false;
                }
                else
                {
                    float boost = ts.MultiplierDoubled ? 2f : 1f;
                    ts.MultiplierDoubled = false;
                    ts.DamageTotal = _damageCalc.CalculateDamage(player.Decks["BoardDeck0"].Cards, boost);
                    ts.MoneyEarned = _damageCalc.CalculateMoney(player.Decks["HandDeck"].Cards);
                }
            }
            else
            {
                ts.DamageTotal = 0;
                ts.MoneyEarned = 0;
            }
        }

        // ── 2. Empereur envers: steal 1/3 of target's damage ─────────────────
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

        // ── 3. Distribute damage ──────────────────────────────────────────────
        int maxDamage = state.TurnStates.Values.Max(ts => ts.DamageTotal);
        var damageTaken = new Dictionary<int, int>();

        for (int i = 0; i < state.Players.Count; i++)
        {
            damageTaken[i] = Math.Max(0, maxDamage - state.TurnStates[i].DamageTotal);
        }

        // ── 4. Apply HP changes ───────────────────────────────────────────────
        for (int i = 0; i < state.Players.Count; i++)
        {
            state.Players[i].Health -= damageTaken[i];
            state.Players[i].Wallet += state.TurnStates[i].MoneyEarned;
        }

        // ── 5. Pape envers: reflect half damage back to attacker ─────────────
        int attackerIdx = Array.IndexOf(
            Enumerable.Range(0, state.Players.Count)
                .Select(i => state.TurnStates[i].DamageTotal)
                .ToArray(),
            maxDamage);

        for (int i = 0; i < state.Players.Count; i++)
        {
            if (!state.TurnStates[i].ReflectDamage) continue;
            if (damageTaken[i] <= 0) continue;

            int reflected = damageTaken[i] / 2;
            if (attackerIdx >= 0 && attackerIdx < state.Players.Count && attackerIdx != i)
                state.Players[attackerIdx].Health -= reflected;
        }

        // ── 6. Pape endroit: heal by total damage dealt to opponents ──────────
        for (int i = 0; i < state.Players.Count; i++)
        {
            if (!state.TurnStates[i].HealFromDamageDealt) continue;

            int healAmount = 0;
            for (int j = 0; j < state.Players.Count; j++)
                if (j != i) healAmount += damageTaken[j];

            state.Players[i].Health += healAmount;
        }

        // ── 7. Store replicated damage for Amoureux envers ───────────────────
        for (int i = 0; i < state.Players.Count; i++)
            state.TurnStates[i].ReplicatedDamageTotal = state.TurnStates[i].DamageTotal;

        // ── 8. Accumulate TotalDamageDealt ────────────────────────────────────
        for (int i = 0; i < state.Players.Count; i++)
            state.TurnStates[i].TotalDamageDealt += state.TurnStates[i].DamageTotal;

        _effectManager.OnTurnEnd(state);

        _events.Publish(new RoundResolved(
            Enumerable.Range(0, state.Players.Count)
                .ToDictionary(i => state.Players[i], i => damageTaken[i])));

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

        foreach (var player in state.Players)
        {
            var hand = player.Decks["HandDeck"];
            var arcanaHand = player.Decks["ArcanaHandDeck"];
            var board = player.Decks["BoardDeck0"];
            var discard = player.Decks["DiscardDeck"];
            var arcanaDiscard = player.Decks["ArcanaDiscardDeck"];

            // Papesse endroit: OnTurnEnd already moved hand → Papesse_Temporary
            // so hand may already be empty here — just clear whatever is left
            discard.AddCards(board.Cards.ToList());
            board.Cards.Clear();

            discard.AddCards(hand.Cards.ToList());
            hand.Cards.Clear();

            // Arcana cards stay in hand between turns — only played ones were already moved to arcanaDiscard
        }

        state.AdvanceTurnPhase(); // CleanupPhase → ShopPhase
        _events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void DrawValueCards(Player player, int count)
    {
        var mainDeck = player.Decks["MainDeck"];
        var hand = player.Decks["HandDeck"];
        var discard = player.Decks["DiscardDeck"];

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
        var specialDeck = player.Decks["SpecialDeck"];
        var arcanaHand = player.Decks["ArcanaHandDeck"];
        var arcanaDiscard = player.Decks["ArcanaDiscardDeck"];

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
        var arcanaDiscard = player.Decks["ArcanaDiscardDeck"];
        var arcanaHand = player.Decks["ArcanaHandDeck"];

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
        var hand = player.Decks["HandDeck"];
        var board = player.Decks["BoardDeck0"];

        var valueCards = hand.Cards.OfType<ValueCard>().ToList();
        foreach (var card in valueCards)
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
