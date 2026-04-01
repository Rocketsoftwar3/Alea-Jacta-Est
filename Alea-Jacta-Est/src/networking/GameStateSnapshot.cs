using System.Collections.Generic;
using MessagePack;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Services;

namespace Alea_Jacta_Est.Networking;

// ── Card snapshots ─────────────────────────────────────────────────────────────
[MessagePackObject]
public class CardSnapshot
{
    [Key(0)] public byte Type     { get; set; } // 0 = ValueCard, 1 = ArcanaCard
    [Key(1)] public int  Suit     { get; set; } // CardSuit (ValueCard only)
    [Key(2)] public int  Rank     { get; set; } // CardRank (ValueCard only)
    [Key(3)] public int  ArcanaNumber { get; set; } // ArcanaCard only
    [Key(4)] public bool IsUpright { get; set; }
    [Key(5)] public bool IsFlipped { get; set; }
    [Key(6)] public int  Price    { get; set; }
}

// ── Player turn state snapshot ────────────────────────────────────────────────
[MessagePackObject]
public class TurnStateSnapshot
{
    [Key(0)]  public bool HasValidated           { get; set; }
    [Key(1)]  public int  DamageTotal            { get; set; }
    [Key(2)]  public int  MoneyEarned            { get; set; }
    [Key(3)]  public bool HasSoldThisRound       { get; set; }
    [Key(4)]  public bool HasEndedShop           { get; set; }
    [Key(5)]  public int  TotalDamageDealt       { get; set; }
    [Key(6)]  public bool MultiplierDoubled      { get; set; }
    [Key(7)]  public int  DamageStealTargetIndex { get; set; }
    [Key(8)]  public bool HealFromDamageDealt    { get; set; }
    [Key(9)]  public bool ReflectDamage          { get; set; }
    [Key(10)] public bool CanSeeOpponentHands    { get; set; }
    [Key(11)] public bool DrawFromArcanaDiscard  { get; set; }
    [Key(12)] public bool HandVisibleAsMoneyOnly { get; set; }
    [Key(13)] public int  MaxArcanasPerTurn      { get; set; }
    [Key(14)] public int  ArcanasPlayedThisTurn  { get; set; }
    [Key(15)] public bool MustPlayAllPointCards  { get; set; }
    [Key(16)] public int  BlockedPointCardDraws  { get; set; }
    [Key(17)] public bool BlockArcanaNextTurn    { get; set; }
    [Key(18)] public bool ReplicatePreviousTurn  { get; set; }
    [Key(19)] public int  ReplicatedDamageTotal  { get; set; }
}

// ── Player snapshot ───────────────────────────────────────────────────────────
[MessagePackObject]
public class PlayerSnapshot
{
    [Key(0)] public string Name          { get; set; } = "";
    [Key(1)] public int    Id            { get; set; }
    [Key(2)] public int    Wallet        { get; set; }
    [Key(3)] public int    Health        { get; set; }
    [Key(4)] public bool   IsLocalPlayer { get; set; }
    [Key(5)] public Dictionary<string, List<CardSnapshot>> Decks { get; set; } = new();
    [Key(6)] public List<CardSnapshot> MarketCards { get; set; } = new();
    [Key(7)] public float  MarketDiscount { get; set; }
}

// ── Full game state snapshot ──────────────────────────────────────────────────
[MessagePackObject]
public class GameStateSnapshot
{
    [Key(0)] public List<PlayerSnapshot>           Players          { get; set; } = new();
    [Key(1)] public int                            CurrentTurn      { get; set; }
    [Key(2)] public int                            CurrentTurnPhase { get; set; }
    [Key(3)] public int                            Phase            { get; set; }
    [Key(4)] public int                            CurrentPlayerIndex { get; set; }
    [Key(5)] public Dictionary<int, TurnStateSnapshot> TurnStates  { get; set; } = new();

    // ── Serialization ─────────────────────────────────────────────────────────

    public static GameStateSnapshot FromGameState(GameState state)
    {
        var snap = new GameStateSnapshot
        {
            CurrentTurn        = state.CurrentTurn,
            CurrentTurnPhase   = (int)state.CurrentTurnPhase,
            Phase              = (int)state.Phase,
            CurrentPlayerIndex = state.CurrentPlayerIndex,
        };

        foreach (var (idx, ts) in state.TurnStates)
        {
            snap.TurnStates[idx] = new TurnStateSnapshot
            {
                HasValidated           = ts.HasValidated,
                DamageTotal            = ts.DamageTotal,
                MoneyEarned            = ts.MoneyEarned,
                HasSoldThisRound       = ts.HasSoldThisRound,
                HasEndedShop           = ts.HasEndedShop,
                TotalDamageDealt       = ts.TotalDamageDealt,
                MultiplierDoubled      = ts.MultiplierDoubled,
                DamageStealTargetIndex = ts.DamageStealTargetIndex,
                HealFromDamageDealt    = ts.HealFromDamageDealt,
                ReflectDamage          = ts.ReflectDamage,
                CanSeeOpponentHands    = ts.CanSeeOpponentHands,
                DrawFromArcanaDiscard  = ts.DrawFromArcanaDiscard,
                HandVisibleAsMoneyOnly = ts.HandVisibleAsMoneyOnly,
                MaxArcanasPerTurn      = ts.MaxArcanasPerTurn,
                ArcanasPlayedThisTurn  = ts.ArcanasPlayedThisTurn,
                MustPlayAllPointCards  = ts.MustPlayAllPointCards,
                BlockedPointCardDraws  = ts.BlockedPointCardDraws,
                BlockArcanaNextTurn    = ts.BlockArcanaNextTurn,
                ReplicatePreviousTurn  = ts.ReplicatePreviousTurn,
                ReplicatedDamageTotal  = ts.ReplicatedDamageTotal,
            };
        }

        for (int i = 0; i < state.Players.Count; i++)
        {
            var p = state.Players[i];
            var ps = new PlayerSnapshot
            {
                Name           = p.Name,
                Id             = p.Id,
                Wallet         = p.Wallet,
                Health         = p.Health,
                IsLocalPlayer  = p.IsLocalPlayer,
                MarketDiscount = p.Market.Discount,
            };

            foreach (var (deckName, deck) in p.Decks)
            {
                var cards = new List<CardSnapshot>(deck.Cards.Count);
                foreach (var card in deck.Cards)
                    cards.Add(SnapCard(card));
                ps.Decks[deckName] = cards;
            }

            foreach (var card in p.Market.Deck.Cards)
                ps.MarketCards.Add(SnapCard(card));

            snap.Players.Add(ps);
        }

        return snap;
    }

    public static void ApplyToGameState(GameStateSnapshot snap, GameState state,
                                        CardFactory factory, GraphicsResources gfx)
    {
        state.CurrentTurn        = snap.CurrentTurn;
        state.CurrentTurnPhase   = (TurnPhase)snap.CurrentTurnPhase;
        state.Phase              = (GamePhase)snap.Phase;
        state.CurrentPlayerIndex = snap.CurrentPlayerIndex;

        // Sync TurnStates
        state.TurnStates.Clear();
        foreach (var (idx, tss) in snap.TurnStates)
        {
            state.TurnStates[idx] = new PlayerTurnState
            {
                HasValidated           = tss.HasValidated,
                DamageTotal            = tss.DamageTotal,
                MoneyEarned            = tss.MoneyEarned,
                HasSoldThisRound       = tss.HasSoldThisRound,
                HasEndedShop           = tss.HasEndedShop,
                TotalDamageDealt       = tss.TotalDamageDealt,
                MultiplierDoubled      = tss.MultiplierDoubled,
                DamageStealTargetIndex = tss.DamageStealTargetIndex,
                HealFromDamageDealt    = tss.HealFromDamageDealt,
                ReflectDamage          = tss.ReflectDamage,
                CanSeeOpponentHands    = tss.CanSeeOpponentHands,
                DrawFromArcanaDiscard  = tss.DrawFromArcanaDiscard,
                HandVisibleAsMoneyOnly = tss.HandVisibleAsMoneyOnly,
                MaxArcanasPerTurn      = tss.MaxArcanasPerTurn,
                ArcanasPlayedThisTurn  = tss.ArcanasPlayedThisTurn,
                MustPlayAllPointCards  = tss.MustPlayAllPointCards,
                BlockedPointCardDraws  = tss.BlockedPointCardDraws,
                BlockArcanaNextTurn    = tss.BlockArcanaNextTurn,
                ReplicatePreviousTurn  = tss.ReplicatePreviousTurn,
                ReplicatedDamageTotal  = tss.ReplicatedDamageTotal,
            };
        }

        // Sync players — assume same count (lobby guarantees this)
        for (int i = 0; i < snap.Players.Count && i < state.Players.Count; i++)
        {
            var ps = snap.Players[i];
            var p  = state.Players[i];

            p.Wallet         = ps.Wallet;
            p.Health         = ps.Health;
            p.Market.Discount = ps.MarketDiscount;

            // Rebuild decks
            foreach (var (deckName, cardSnaps) in ps.Decks)
            {
                if (!p.Decks.TryGetValue(deckName, out var deck))
                {
                    deck = new Deck();
                    p.Decks[deckName] = deck;
                }
                deck.Cards.Clear();
                foreach (var cs in cardSnaps)
                    deck.Cards.Add(RestoreCard(cs, factory));
            }

            // Remove decks not in snapshot (cleaned up by host)
            var toRemove = new System.Collections.Generic.List<string>();
            foreach (var key in p.Decks.Keys)
                if (!ps.Decks.ContainsKey(key)) toRemove.Add(key);
            foreach (var key in toRemove)
                p.Decks.Remove(key);

            // Rebuild market
            p.Market.Deck.Cards.Clear();
            foreach (var cs in ps.MarketCards)
                p.Market.Deck.Cards.Add(RestoreCard(cs, factory));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CardSnapshot SnapCard(Card card)
    {
        if (card is ValueCard vc)
        {
            return new CardSnapshot
            {
                Type     = 0,
                Suit     = (int)vc.Suit,
                Rank     = (int)vc.Rank,
                IsUpright = vc.IsUpright,
                IsFlipped = vc.IsFlipped,
                Price    = vc.Price,
            };
        }
        if (card is ArcanaCard ac)
        {
            return new CardSnapshot
            {
                Type        = 1,
                ArcanaNumber = ac.ArcanaNumber,
                IsUpright   = ac.IsUpright,
                IsFlipped   = ac.IsFlipped,
                Price       = ac.Price,
            };
        }
        // Fallback: plain card (placeholder)
        return new CardSnapshot
        {
            Type     = 0,
            IsUpright = card.IsUpright,
            IsFlipped = card.IsFlipped,
            Price    = card.Price,
        };
    }

    private static Card RestoreCard(CardSnapshot cs, CardFactory factory)
    {
        Card card;
        if (cs.Type == 0)
        {
            card = factory.CreateValueCard((CardSuit)cs.Suit, (CardRank)cs.Rank);
        }
        else
        {
            card = factory.BuildArcanaByNumber(cs.ArcanaNumber, cs.IsUpright);
        }
        card.IsUpright = cs.IsUpright;
        card.IsFlipped = cs.IsFlipped;
        card.Price     = cs.Price;
        return card;
    }
}
