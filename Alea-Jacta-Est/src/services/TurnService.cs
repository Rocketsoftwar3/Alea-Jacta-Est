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

    /// <summary>
    /// Draws 5 value cards + 1 arcana for each player, then advances to PlayPhase.
    /// Reshuffles DiscardDeck into MainDeck if the draw pile is exhausted.
    /// </summary>
    public void ExecuteDrawPhase(GameState state)
    {
        foreach (var player in state.Players)
        {
            DrawValueCards(player, 5);
            DrawArcanaCard(player);
            _effectManager.OnTurnStart(state);
        }

        state.AdvanceTurnPhase(); // DrawPhase → PlayPhase
        _events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }

    /// <summary>
    /// Resolves damage for all players, applies HP changes, adds money.
    /// Opponents auto-validate with 0 damage (proto — no AI).
    /// </summary>
    public void ExecuteResolutionPhase(GameState state)
    {
        // Calculate scores for each player
        for (int i = 0; i < state.Players.Count; i++)
        {
            var player = state.Players[i];
            var ts = state.TurnStates[i];

            if (player.IsLocalPlayer)
            {
                var boardCards = player.Decks["BoardDeck0"].Cards;
                ts.DamageTotal = _damageCalc.CalculateDamage(boardCards);
                ts.MoneyEarned = _damageCalc.CalculateMoney(player.Decks["HandDeck"].Cards);
            }
            else
            {
                // Opponents auto-validate: 0 damage, 0 money (proto)
                ts.DamageTotal = 0;
                ts.MoneyEarned = 0;
            }
        }

        // Distribute damage: each player takes (max - their own) damage
        int maxDamage = state.TurnStates.Values.Max(ts => ts.DamageTotal);
        var damages = new Dictionary<Player, int>();

        for (int i = 0; i < state.Players.Count; i++)
        {
            var player = state.Players[i];
            var ts = state.TurnStates[i];
            int damageTaken = Math.Max(0, maxDamage - ts.DamageTotal);
            damages[player] = damageTaken;

            player.Health -= damageTaken;
            player.Wallet += ts.MoneyEarned;
        }

        _effectManager.OnTurnEnd(state);

        // Publish resolution result
        _events.Publish(new RoundResolved(damages));

        // Eliminate players at 0 HP or below
        foreach (var player in state.Players.ToList())
        {
            if (player.Health <= 0)
                _events.Publish(new PlayerEliminated(player));
        }

        state.AdvanceTurnPhase(); // ResolutionPhase → CleanupPhase
        _events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }

    /// <summary>
    /// Moves all hand and played cards to the discard piles.
    /// </summary>
    public void ExecuteCleanupPhase(GameState state)
    {
        foreach (var player in state.Players)
        {
            var hand = player.Decks["HandDeck"];
            var arcanaHand = player.Decks["ArcanaHandDeck"];
            var board = player.Decks["BoardDeck0"];
            var discard = player.Decks["DiscardDeck"];
            var arcanaDiscard = player.Decks["ArcanaDiscardDeck"];

            // Played cards → discard
            discard.AddCards(board.Cards.ToList());
            board.Cards.Clear();

            // Remaining hand cards → discard (Papesse exception handled in PapesseEffect)
            discard.AddCards(hand.Cards.ToList());
            hand.Cards.Clear();

            // Arcana hand → arcana discard
            arcanaDiscard.AddCards(arcanaHand.Cards.ToList());
            arcanaHand.Cards.Clear();
        }

        state.AdvanceTurnPhase(); // CleanupPhase → ShopPhase
        _events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }

    /// <summary>
    /// Ends the shop phase and starts the next turn (resets TurnStates, increments turn counter).
    /// </summary>
    public void AdvanceToNextTurn(GameState state)
    {
        state.AdvanceToNextTurn();
        state.AdvanceTurnPhase(); // ShopPhase → DrawPhase
        _events.Publish(new TurnPhaseChanged(state.CurrentTurnPhase));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void DrawValueCards(Player player, int count)
    {
        var mainDeck = player.Decks["MainDeck"];
        var hand = player.Decks["HandDeck"];
        var discard = player.Decks["DiscardDeck"];

        // Reshuffle discard into main if needed
        if (mainDeck.Cards.Count < count && discard.Cards.Count > 0)
        {
            mainDeck.AddCards(discard.Cards.ToList());
            discard.Cards.Clear();
            // Simple in-place shuffle
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

        // Reshuffle arcana discard into special if needed
        if (specialDeck.Cards.Count == 0 && arcanaDiscard.Cards.Count > 0)
        {
            specialDeck.AddCards(arcanaDiscard.Cards.ToList());
            arcanaDiscard.Cards.Clear();
            Shuffle(specialDeck.Cards);
        }

        var card = specialDeck.DrawCard();
        if (card != null) arcanaHand.AddCard(card);
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
