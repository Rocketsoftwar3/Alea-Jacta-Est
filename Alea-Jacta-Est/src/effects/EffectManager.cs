using System.Collections.Generic;
using System.Linq;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Effects;

public class EffectManager
{
    private readonly List<ActiveEffect> _active = new();
    private readonly EventBus _events;

    public EffectManager(EventBus events)
    {
        _events = events;
    }

    public void PlayCard(GameState state, ArcanaCard card, Player owner)
    {
        foreach (var effect in card.Effects)
        {
            effect.OnPlay(state, card);

            if (effect.Duration > 0)
                _active.Add(new ActiveEffect(card, effect, effect.Duration, owner));

            _events.Publish(new EffectApplied(card, effect, owner));
        }
    }

    public IReadOnlyList<(string Name, int RemainingTurns)> GetActiveEffectSummary()
        => _active.Select(a => (a.Card.ArcanaName, a.RemainingTurns)).ToList();

    public IReadOnlyList<(ArcanaCard Card, int RemainingTurns)> GetActiveEffectsForPlayer(Player player)
        => _active.Where(a => a.Owner == player).Select(a => (a.Card, a.RemainingTurns)).ToList();

    public void OnTurnStart(GameState state)
    {
        foreach (var active in _active)
            active.Effect.OnTurnStart(state, active.Card);
    }

    public void OnTurnEnd(GameState state)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var active = _active[i];
            active.Effect.OnTurnEnd(state, active.Card);
            active.RemainingTurns--;

            if (active.RemainingTurns <= 0)
            {
                active.Effect.OnRemove(state, active.Card);
                _events.Publish(new EffectExpired(active.Card, active.Effect));
                _active.RemoveAt(i);
            }
        }
    }

    private class ActiveEffect
    {
        public ArcanaCard Card { get; }
        public ICardEffect Effect { get; }
        public int RemainingTurns { get; set; }
        public Player Owner { get; }

        public ActiveEffect(ArcanaCard card, ICardEffect effect, int duration, Player owner)
        {
            Card = card;
            Effect = effect;
            RemainingTurns = duration;
            Owner = owner;
        }
    }
}
