using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Alea_Jacta_Est.Entities;

public class Card
{
    public Texture2D Texture { get; set; }
    public Vector2 Position { get; set; }
    public float Scale { get; set; }
    public float Rotation { get; set; }
    public Color Tint { get; set; }

    public Card(Texture2D texture, Vector2 position)
    {
        Texture = texture;
        Position = position;
        Scale = 1.0f;
        Rotation = 0f;
        Tint = Color.White;
    }

    public Rectangle GetBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            (int)(Texture.Width * Scale),
            (int)(Texture.Height * Scale)
        );
    }

    public Vector2 GetOrigin()
    {
        return new Vector2(Texture.Width / 2f, Texture.Height / 2f);
    }
}
