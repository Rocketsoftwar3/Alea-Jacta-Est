using System;
using Microsoft.Xna.Framework.Graphics;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Entities;

/// <summary>Base class for all playing cards. Holds visual state only.</summary>
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

    public Card(Texture2D textureRecto, Texture2D textureVerso, bool isUpright = true)
    {
        TextureRecto = textureRecto;
        TextureVerso = textureVerso;
        IsUpright = isUpright;
    }

    /// <summary>Builder for creating plain placeholder cards.</summary>
    public class CardBuilder
    {
        private string? _rectoPath;
        private string? _versoPath;
        private bool _isUpright = true;
        private int _price = 0;

        public CardBuilder TextureRecto(string path) { _rectoPath = path; return this; }
        public CardBuilder TextureVerso(string path) { _versoPath = path; return this; }
        public CardBuilder Upright(bool upright) { _isUpright = upright; return this; }
        public CardBuilder WithPrice(int price) { _price = price; return this; }

        public Card Build(GraphicsResources gfx)
        {
            if (_rectoPath == null)
                throw new InvalidOperationException("TextureRecto must be specified");

            var recto = gfx.Content.Load<Texture2D>(_rectoPath);
            var verso = _versoPath != null
                ? gfx.Content.Load<Texture2D>(_versoPath)
                : gfx.CardVersoTexture;

            return new Card(recto, verso, _isUpright) { Price = _price };
        }
    }
}
