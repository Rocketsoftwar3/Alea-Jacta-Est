using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Services;

public class CardRenderer
{
    private SpriteBatch _spriteBatch;

    public CardRenderer(SpriteBatch spriteBatch)
    {
        _spriteBatch = spriteBatch;
    }

    public void Draw(Card card)
    {
        _spriteBatch.Draw(
            card.Texture,
            card.Position,
            null,
            card.Tint,
            card.Rotation,
            card.GetOrigin(),
            card.Scale,
            SpriteEffects.None,
            0f
        );
    }

    public void DrawMultiple(params Card[] cards)
    {
        foreach (var card in cards)
        {
            Draw(card);
        }
    }
}
