using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Entities;

/// <summary>A major arcana card with a number, name, and card effects.</summary>
public class ArcanaCard : Card
{
    public int ArcanaNumber { get; }
    public string ArcanaName { get; }

    public IReadOnlyList<ICardEffect> Effects => _effects;
    private readonly List<ICardEffect> _effects;

    public ArcanaCard(int arcanaNumber, string arcanaName, Texture2D recto, Texture2D verso,
                      bool isUpright, List<ICardEffect> effects)
        : base(recto, verso, isUpright)
    {
        ArcanaNumber = arcanaNumber;
        ArcanaName = arcanaName;
        _effects = effects;
    }

    /// <summary>Starts building an arcana card with the specified effect.</summary>
    public static ArcanaCardBuilder Use(int arcanaNumber, string arcanaName, ICardEffect effect)
    {
        return new ArcanaCardBuilder(arcanaNumber, arcanaName, effect);
    }

    public class ArcanaCardBuilder
    {
        private readonly int _arcanaNumber;
        private readonly string _arcanaName;
        private readonly ICardEffect _effect;
        private string? _rectoPath;
        private string? _versoPath;
        private bool _isUpright = true;
        private int _price = 0;

        public ArcanaCardBuilder(int arcanaNumber, string arcanaName, ICardEffect effect)
        {
            _arcanaNumber = arcanaNumber;
            _arcanaName = arcanaName;
            _effect = effect;
        }

        public ArcanaCardBuilder TextureRecto(string path) { _rectoPath = path; return this; }
        public ArcanaCardBuilder TextureVerso(string path) { _versoPath = path; return this; }
        public ArcanaCardBuilder Upright(bool upright) { _isUpright = upright; return this; }
        public ArcanaCardBuilder WithPrice(int price) { _price = price; return this; }

        public ArcanaCard Build(GraphicsResources gfx)
        {
            if (_rectoPath == null)
                throw new InvalidOperationException("TextureRecto must be specified");

            var recto = gfx.Content.Load<Texture2D>(_rectoPath);
            var verso = _versoPath != null
                ? gfx.Content.Load<Texture2D>(_versoPath)
                : gfx.CardVersoTexture;

            return new ArcanaCard(_arcanaNumber, _arcanaName, recto, verso, _isUpright,
                                  new List<ICardEffect> { _effect })
            {
                Price = _price
            };
        }
    }
}
