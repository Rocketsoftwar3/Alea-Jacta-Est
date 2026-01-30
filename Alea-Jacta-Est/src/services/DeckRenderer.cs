using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Services;

/// <summary>Defines how cards in a deck are displayed.</summary>
public enum DeckDisplayMode
{
    FanUp,
    FanDown,
    Stacked
}

/// <summary>Static renderer for decks.</summary>
public static class DeckRenderer
{
    /// <summary>Draws a deck with the specified parameters.</summary>
    public static void Draw(
        SpriteBatch spriteBatch,
        Deck deck,
        Vector2 position,
        DeckDisplayMode displayMode,
        bool isFrontVisible,
        float globalScale = 1.0f,
        float cardScale = 1.0f,
        float fanSpread = 60f,
        float fanRadius = 200f,
        float stackOffset = 20f)
    {
        if (deck.Cards.Count == 0) return;

        // Apply global scale to all values
        float scaledCardScale = cardScale * globalScale;
        float scaledFanRadius = fanRadius * globalScale;
        float scaledStackOffset = stackOffset * globalScale;

        switch (displayMode)
        {
            case DeckDisplayMode.FanUp:
                DrawFan(spriteBatch, deck, position, isFrontVisible, scaledCardScale, fanSpread, scaledFanRadius, isUp: true);
                break;
            case DeckDisplayMode.FanDown:
                DrawFan(spriteBatch, deck, position, isFrontVisible, scaledCardScale, fanSpread, scaledFanRadius, isUp: false);
                break;
            case DeckDisplayMode.Stacked:
                DrawStacked(spriteBatch, deck, position, isFrontVisible, scaledCardScale, scaledStackOffset);
                break;
        }
    }

    private static void DrawFan(
        SpriteBatch spriteBatch,
        Deck deck,
        Vector2 position,
        bool isFrontVisible,
        float scale,
        float fanSpreadDegrees,
        float radius,
        bool isUp)
    {
        int count = deck.Cards.Count;
        float fanSpread = MathHelper.ToRadians(fanSpreadDegrees);
        float angleStep = count > 1 ? fanSpread / (count - 1) : 0;
        float startAngle = -fanSpread / 2f;

        for (int i = 0; i < count; i++)
        {
            var card = deck.Cards[i];
            float angle = startAngle + i * angleStep;
            float offsetX = (float)Math.Sin(angle) * radius;
            float offsetY, displayAngle;

            if (isUp)
            {
                offsetY = -(float)Math.Cos(angle) * radius + radius;
                displayAngle = angle;
            }
            else
            {
                offsetY = (float)Math.Cos(angle) * radius - radius;
                displayAngle = -angle;
            }

            var cardPosition = position + new Vector2(offsetX, offsetY);
            DrawCard(spriteBatch, card, cardPosition, isFrontVisible, scale, displayAngle);
        }
    }

    private static void DrawStacked(
        SpriteBatch spriteBatch,
        Deck deck,
        Vector2 position,
        bool isFrontVisible,
        float scale,
        float stackOffset)
    {
        for (int i = 0; i < deck.Cards.Count; i++)
        {
            var card = deck.Cards[i];
            var cardPosition = position + new Vector2(0, -i * stackOffset);
            DrawCard(spriteBatch, card, cardPosition, isFrontVisible, scale, 0f);
        }
    }

    private static void DrawCard(
        SpriteBatch spriteBatch,
        Card card,
        Vector2 position,
        bool isFrontVisible,
        float scale,
        float rotation)
    {
        var texture = isFrontVisible ? card.TextureRecto : card.TextureVerso;

        // Use texture's actual center as origin
        var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

        // Normalize scale so all textures render at same final size
        // regardless of their actual resolution
        float normalizedScale = Card.BaseWidth / texture.Width;
        float finalScale = scale * normalizedScale;

        // Apply upright rotation if needed
        float finalRotation = rotation;
        if (!card.IsUpright)
        {
            finalRotation += MathHelper.Pi; // Rotate 180 degrees
        }

        spriteBatch.Draw(
            texture,
            position,
            null,
            Color.White,
            finalRotation,
            origin,
            finalScale,
            SpriteEffects.None,
            0f
        );
    }
}
