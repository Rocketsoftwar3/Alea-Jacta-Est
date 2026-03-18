using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Entities;

/// <summary>Represents a playing card with textures, orientation, and effects.</summary>
public class Card
{
    public const float BaseWidth = 216f;
    public const float BaseHeight = 360f;

    public Texture2D TextureRecto { get; set; }
    public Texture2D TextureVerso { get; set; }
    public bool IsHovered { get; set; }
    public bool IsFlipped { get; set; }   // true = shows verso (face-down)
    public bool IsUpright { get; set; }   // true = endroit, false = envers (180° rotation)
    public int Price { get; set; }

    private readonly Queue<ICardEffect> _effects;

    /// <summary>Creates a new card with the specified textures.</summary>
    public Card(Texture2D textureRecto, Texture2D textureVerso, bool isUpright = true)
    {
        TextureRecto = textureRecto;
        TextureVerso = textureVerso;
        IsUpright = isUpright;
        _effects = new Queue<ICardEffect>();
    }

    /// <summary>Creates a card with specific effects.</summary>
    public Card(Texture2D textureRecto, Texture2D textureVerso, bool isUpright, Queue<ICardEffect> effects)
    {
        TextureRecto = textureRecto;
        TextureVerso = textureVerso;
        IsUpright = isUpright;
        _effects = effects;
    }

    /// <summary>Resolve the card, resolving all its effects.</summary>
    public void Resolve(GameContext context)
    {
        while (_effects.Count > 0)
        {
            var effect = _effects.Dequeue();
            effect.Resolve(context, this);
        }
    }

    /// <summary>Starts building a card with the specified effect.</summary>
    public static CardBuilder Use(ICardEffect effect)
    {
        return new CardBuilder(effect);
    }

    /// <summary>Builder for creating cards with a fluent API.</summary>
    public class CardBuilder
    {
        private readonly ICardEffect _effect;
        private string? _rectoPath;
        private string? _versoPath;
        private bool _isUpright = true;
        private int _price = 0;

        public CardBuilder(ICardEffect effect)
        {
            _effect = effect;
        }

        public CardBuilder TextureRecto(string path)
        {
            _rectoPath = path;
            return this;
        }

        public CardBuilder TextureVerso(string path)
        {
            _versoPath = path;
            return this;
        }

        public CardBuilder Upright(bool upright)
        {
            _isUpright = upright;
            return this;
        }

        public CardBuilder WithPrice(int price)
        {
            _price = price;
            return this;
        }

        public Card Build(GameContext context)
        {
            if (_rectoPath == null)
                throw new InvalidOperationException("TextureRecto must be specified");

            var recto = context.Content.Load<Texture2D>(_rectoPath);
            var verso = _versoPath != null
                ? context.Content.Load<Texture2D>(_versoPath)
                : context.CardVersoTexture;

            var effects = new Queue<ICardEffect>();
            effects.Enqueue(_effect);

            return new Card(recto, verso, _isUpright, effects)
            {
                Price = _price
            };
        }
    }
}
