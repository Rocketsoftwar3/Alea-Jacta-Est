using System.Collections.Generic;
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

    public void PlayCard(GameState state, Card card)
    {
        foreach (var effect in card.Effects)
        {
            effect.OnPlay(state, card);

            if (effect.Duration > 0)
                _active.Add(new ActiveEffect(card, effect, effect.Duration));

            _events.Publish(new EffectApplied(card, effect));
        }
    }

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
        public Card Card { get; }
        public ICardEffect Effect { get; }
        public int RemainingTurns { get; set; }

        public ActiveEffect(Card card, ICardEffect effect, int duration)
        {
            Card = card;
            Effect = effect;
            RemainingTurns = duration;
        }
    }
}
