using System;
using MessagePack;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Config;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Main;
using Alea_Jacta_Est.Networking;

namespace Alea_Jacta_Est.Networking;

// ── Command type discriminator ────────────────────────────────────────────────
public static class CmdType
{
    public const byte PlayCard           = 1;
    public const byte TakeBack           = 2;
    public const byte ActivateArcana     = 3;
    public const byte ActivateArcanaTarget = 4;
    public const byte BuyCard            = 5;
    public const byte SellCard           = 6;
    public const byte ValidateTurn       = 7;
    public const byte EndShop            = 8;
    public const byte FlipCard           = 9;
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
[MessagePackObject] public class PlayCardMsg        { [Key(0)] public int PlayerIndex; [Key(1)] public int CardIndex; }
[MessagePackObject] public class TakeBackMsg        { [Key(0)] public int PlayerIndex; [Key(1)] public int CardIndex; }
[MessagePackObject] public class ActivateArcanaMsg  { [Key(0)] public int PlayerIndex; [Key(1)] public int CardIndex; }
[MessagePackObject] public class ActivateTargetMsg  { [Key(0)] public int PlayerIndex; [Key(1)] public int CardIndex; [Key(2)] public int TargetPlayerIndex; }
[MessagePackObject] public class BuyCardMsg         { [Key(0)] public int PlayerIndex; [Key(1)] public int CardIndex; }
[MessagePackObject] public class SellCardMsg        { [Key(0)] public int PlayerIndex; [Key(1)] public int CardIndex; [Key(2)] public string DeckName = ""; }
[MessagePackObject] public class ValidateTurnMsg    { [Key(0)] public int PlayerIndex; }
[MessagePackObject] public class EndShopMsg         { [Key(0)] public int PlayerIndex; }
[MessagePackObject] public class FlipCardMsg_Net    { [Key(0)] public int PlayerIndex; [Key(1)] public string DeckName = ""; [Key(2)] public int CardIndex; }

// ── Serializer ────────────────────────────────────────────────────────────────
public static class CommandSerializer
{
    /// <summary>Serializes a command into [NetMsgType.GameCommand][CmdType][MessagePack payload].</summary>
    public static byte[] Serialize(IGameCommand command, GameState state)
    {
        byte cmdType;
        byte[] payload;

        switch (command)
        {
            case PlayCardCommand c:
            {
                int pi = state.Players.IndexOf(c.Player);
                int ci = c.Player.Decks[DeckType.HandDeck].Cards.IndexOf(c.Card);
                cmdType = CmdType.PlayCard;
                payload = MessagePackSerializer.Serialize(new PlayCardMsg { PlayerIndex = pi, CardIndex = ci });
                break;
            }
            case TakeBackCardCommand c:
            {
                int pi = state.Players.IndexOf(c.Player);
                int ci = c.Player.Decks[DeckType.BoardDeck0].Cards.IndexOf(c.Card);
                cmdType = CmdType.TakeBack;
                payload = MessagePackSerializer.Serialize(new TakeBackMsg { PlayerIndex = pi, CardIndex = ci });
                break;
            }
            case ActivateArcanaCommand c:
            {
                int pi = state.Players.IndexOf(c.Player);
                int ci = c.Player.Decks[DeckType.ArcanaHandDeck].Cards.IndexOf(c.Card);
                cmdType = CmdType.ActivateArcana;
                payload = MessagePackSerializer.Serialize(new ActivateArcanaMsg { PlayerIndex = pi, CardIndex = ci });
                break;
            }
            case ActivateArcanaWithTargetCommand c:
            {
                int pi  = state.Players.IndexOf(c.Activator);
                int ci  = c.Activator.Decks[DeckType.ArcanaHandDeck].Cards.IndexOf(c.Card);
                int ti  = state.Players.IndexOf(c.Target);
                cmdType = CmdType.ActivateArcanaTarget;
                payload = MessagePackSerializer.Serialize(new ActivateTargetMsg { PlayerIndex = pi, CardIndex = ci, TargetPlayerIndex = ti });
                break;
            }
            case BuyCardCommand c:
            {
                int pi = state.Players.IndexOf(c.Buyer);
                int ci = c.Buyer.Market.Deck.Cards.IndexOf(c.Card);
                cmdType = CmdType.BuyCard;
                payload = MessagePackSerializer.Serialize(new BuyCardMsg { PlayerIndex = pi, CardIndex = ci });
                break;
            }
            case SellCardCommand c:
            {
                int pi = c.PlayerIndex;
                // Find card across all decks
                string deckName = "";
                int cardIdx = -1;
                foreach (var (dt, deck) in state.Players[pi].Decks)
                {
                    cardIdx = deck.Cards.IndexOf(c.Card);
                    if (cardIdx >= 0) { deckName = dt.ToString(); break; }
                }
                cmdType = CmdType.SellCard;
                payload = MessagePackSerializer.Serialize(new SellCardMsg { PlayerIndex = pi, CardIndex = cardIdx, DeckName = deckName });
                break;
            }
            case ValidateTurnCommand c:
                cmdType = CmdType.ValidateTurn;
                payload = MessagePackSerializer.Serialize(new ValidateTurnMsg { PlayerIndex = c.PlayerIndex });
                break;

            case EndShopPhaseCommand c:
                cmdType = CmdType.EndShop;
                payload = MessagePackSerializer.Serialize(new EndShopMsg { PlayerIndex = c.PlayerIndex });
                break;

            case FlipCardCommand c:
            {
                // FlipCard: locate card among all players/decks
                int pi = -1; string dn = ""; int ci2 = -1;
                for (int p = 0; p < state.Players.Count; p++)
                {
                    foreach (var (dt, deck) in state.Players[p].Decks)
                    {
                        int idx = deck.Cards.IndexOf(c.Card);
                        if (idx >= 0) { pi = p; dn = dt.ToString(); ci2 = idx; break; }
                    }
                    if (pi >= 0) break;
                }
                cmdType = CmdType.FlipCard;
                payload = MessagePackSerializer.Serialize(new FlipCardMsg_Net { PlayerIndex = pi, DeckName = dn, CardIndex = ci2 });
                break;
            }
            default:
                throw new NotSupportedException($"Cannot serialize command type {command.GetType().Name}");
        }

        // Build envelope: [GameCommand byte][cmdType byte][payload...]
        var result = new byte[2 + payload.Length];
        result[0] = NetMsgType.GameCommand;
        result[1] = cmdType;
        Buffer.BlockCopy(payload, 0, result, 2, payload.Length);
        return result;
    }

    /// <summary>Deserializes a command from [GameCommand][CmdType][payload]. State + EffectManager used to resolve references.</summary>
    public static IGameCommand Deserialize(byte[] data, GameState state, EffectManager effectManager)
    {
        if (data.Length < 2) throw new ArgumentException("Packet too short");
        // data[0] == NetMsgType.GameCommand (caller already verified)
        byte cmdType = data[1];
        var payload = new byte[data.Length - 2];
        Buffer.BlockCopy(data, 2, payload, 0, payload.Length);

        switch (cmdType)
        {
            case CmdType.PlayCard:
            {
                var m = MessagePackSerializer.Deserialize<PlayCardMsg>(payload);
                var player = state.Players[m.PlayerIndex];
                var card   = (Alea_Jacta_Est.Entities.ValueCard)player.Decks[DeckType.HandDeck].Cards[m.CardIndex];
                return new PlayCardCommand(player, card);
            }
            case CmdType.TakeBack:
            {
                var m = MessagePackSerializer.Deserialize<TakeBackMsg>(payload);
                var player = state.Players[m.PlayerIndex];
                var card   = (Alea_Jacta_Est.Entities.ValueCard)player.Decks[DeckType.BoardDeck0].Cards[m.CardIndex];
                return new TakeBackCardCommand(player, card);
            }
            case CmdType.ActivateArcana:
            {
                var m = MessagePackSerializer.Deserialize<ActivateArcanaMsg>(payload);
                var player = state.Players[m.PlayerIndex];
                var card   = (Alea_Jacta_Est.Entities.ArcanaCard)player.Decks[DeckType.ArcanaHandDeck].Cards[m.CardIndex];
                return new ActivateArcanaCommand(player, card, null, effectManager);
            }
            case CmdType.ActivateArcanaTarget:
            {
                var m      = MessagePackSerializer.Deserialize<ActivateTargetMsg>(payload);
                var player = state.Players[m.PlayerIndex];
                var card   = (Alea_Jacta_Est.Entities.ArcanaCard)player.Decks[DeckType.ArcanaHandDeck].Cards[m.CardIndex];
                var target = state.Players[m.TargetPlayerIndex];
                return new ActivateArcanaWithTargetCommand(player, card, target, effectManager);
            }
            case CmdType.BuyCard:
            {
                var m      = MessagePackSerializer.Deserialize<BuyCardMsg>(payload);
                var player = state.Players[m.PlayerIndex];
                var card   = player.Market.Deck.Cards[m.CardIndex];
                int price  = player.Market.GetDiscountedPrice(card.Price);
                return new BuyCardCommand(player, card, price);
            }
            case CmdType.SellCard:
            {
                var m      = MessagePackSerializer.Deserialize<SellCardMsg>(payload);
                var player = state.Players[m.PlayerIndex];
                var dt     = Enum.Parse<DeckType>(m.DeckName);
                var deck   = player.Decks[dt];
                var card   = deck.Cards[m.CardIndex];
                return new SellCardCommand(player, card, deck, m.PlayerIndex);
            }
            case CmdType.ValidateTurn:
            {
                var m = MessagePackSerializer.Deserialize<ValidateTurnMsg>(payload);
                return new ValidateTurnCommand(m.PlayerIndex);
            }
            case CmdType.EndShop:
            {
                var m = MessagePackSerializer.Deserialize<EndShopMsg>(payload);
                return new EndShopPhaseCommand(m.PlayerIndex);
            }
            case CmdType.FlipCard:
            {
                var m    = MessagePackSerializer.Deserialize<FlipCardMsg_Net>(payload);
                var dt   = Enum.Parse<DeckType>(m.DeckName);
                var card = state.Players[m.PlayerIndex].Decks[dt].Cards[m.CardIndex];
                return new FlipCardCommand(card);
            }
            default:
                throw new NotSupportedException($"Unknown command type byte {cmdType}");
        }
    }
}
