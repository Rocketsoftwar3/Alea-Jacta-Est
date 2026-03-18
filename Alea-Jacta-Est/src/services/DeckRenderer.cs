using System;
using System.Collections.Generic;
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

/// <summary>Screen-space info for a rendered card (used for hit-testing).</summary>
public record CardScreenInfo(Card Card, Vector2 Center, float Rotation, float RenderScale);

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

    /// <summary>Returns the screen-space center, rotation and scale of each card in a deck.</summary>
    public static List<CardScreenInfo> GetCardPositions(
        Deck deck,
        Vector2 position,
        DeckDisplayMode displayMode,
        float globalScale = 1.0f,
        float cardScale = 1.0f,
        float fanSpread = 60f,
        float fanRadius = 200f,
        float stackOffset = 20f)
    {
        var result = new List<CardScreenInfo>(deck.Cards.Count);
        if (deck.Cards.Count == 0) return result;

        float scaledCardScale = cardScale * globalScale;
        float scaledFanRadius = fanRadius * globalScale;
        float scaledStackOffset = stackOffset * globalScale;

        switch (displayMode)
        {
            case DeckDisplayMode.FanUp:
                ComputeFanPositions(deck, position, scaledCardScale, fanSpread, scaledFanRadius, isUp: true, result);
                break;
            case DeckDisplayMode.FanDown:
                ComputeFanPositions(deck, position, scaledCardScale, fanSpread, scaledFanRadius, isUp: false, result);
                break;
            case DeckDisplayMode.Stacked:
                ComputeStackedPositions(deck, position, scaledCardScale, scaledStackOffset, result);
                break;
        }

        return result;
    }

    private static void ComputeFanPositions(Deck deck, Vector2 position, float scale, float fanSpreadDegrees, float radius, bool isUp, List<CardScreenInfo> result)
    {
        int count = deck.Cards.Count;
        float fanSpread = MathHelper.ToRadians(fanSpreadDegrees);
        float angleStep = count > 1 ? fanSpread / (count - 1) : 0;
        float startAngle = -fanSpread / 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * angleStep;
            float offsetX = (float)Math.Sin(angle) * radius;
            float offsetY = isUp
                ? -(float)Math.Cos(angle) * radius + radius
                :  (float)Math.Cos(angle) * radius - radius;
            float displayAngle = isUp ? angle : -angle;
            result.Add(new CardScreenInfo(deck.Cards[i], position + new Vector2(offsetX, offsetY), displayAngle, scale));
        }
    }

    private static void ComputeStackedPositions(Deck deck, Vector2 position, float scale, float stackOffset, List<CardScreenInfo> result)
    {
        for (int i = 0; i < deck.Cards.Count; i++)
            result.Add(new CardScreenInfo(deck.Cards[i], position + new Vector2(0, -i * stackOffset), 0f, scale));
    }

    private static void DrawFan(SpriteBatch spriteBatch, Deck deck, Vector2 position, bool isFrontVisible, float scale, float fanSpreadDegrees, float radius, bool isUp)
    {
        var infos = new List<CardScreenInfo>();
        ComputeFanPositions(deck, position, scale, fanSpreadDegrees, radius, isUp, infos);
        foreach (var info in infos)
            DrawCard(spriteBatch, info.Card, info.Center, isFrontVisible, info.RenderScale, info.Rotation);
    }

    private static void DrawStacked(SpriteBatch spriteBatch, Deck deck, Vector2 position, bool isFrontVisible, float scale, float stackOffset)
    {
        var infos = new List<CardScreenInfo>();
        ComputeStackedPositions(deck, position, scale, stackOffset, infos);
        foreach (var info in infos)
            DrawCard(spriteBatch, info.Card, info.Center, isFrontVisible, info.RenderScale, info.Rotation);
    }

    private static void DrawCard(SpriteBatch spriteBatch, Card card, Vector2 position, bool isFrontVisible, float scale, float rotation)
    {
        // card.IsFlipped lets the player individually flip a card regardless of deck config
        bool showFront = isFrontVisible ^ card.IsFlipped;
        var texture = showFront ? card.TextureRecto : card.TextureVerso;
        var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
        float normalizedScale = Card.BaseWidth / texture.Width;
        float finalScale = scale * normalizedScale;
        float finalRotation = rotation + (card.IsUpright ? 0f : MathHelper.Pi);
        spriteBatch.Draw(texture, position, null, Color.White, finalRotation, origin, finalScale, SpriteEffects.None, 0f);
    }
}
