using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Alea_Jacta_Est.Entities;

public class Card
{
    public Texture2D TextureRecto { get; set; }
    public Texture2D TextureVerso { get; set; }
    public bool IsFaceUp { get; set; }
    public Texture2D CurrentTexture => IsFaceUp ? TextureRecto : TextureVerso;

    public Vector2 Position { get; set; }
    public float Scale { get; set; }
    public float Rotation { get; set; }
    public Color Tint { get; set; }

    public Card(Texture2D textureRecto, Texture2D textureVerso, Vector2 position, bool isFaceUp = true)
    {
        TextureRecto = textureRecto;
        TextureVerso = textureVerso;
        IsFaceUp = isFaceUp;
        Position = position;
        Scale = 1.0f;
        Rotation = 0f;
        Tint = Color.White;
    }

    public Rectangle GetBounds()
    {
        var texture = CurrentTexture;
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            (int)(texture.Width * Scale),
            (int)(texture.Height * Scale)
        );
    }

    public Vector2 GetOrigin()
    {
        var texture = CurrentTexture;
        return new Vector2(texture.Width / 2f, texture.Height / 2f);
    }
}
