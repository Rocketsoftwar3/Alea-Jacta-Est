using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Alea_Jacta_Est.Entities;

public enum DeckDisplayMode
{
    FanUp,
    FanDown,
    Stacked
}

public class Deck
{
    public List<Card> Cards { get; }
    public DeckDisplayMode DisplayMode { get; set; }
    public Vector2 Position { get; set; }
    public float FanSpread { get; set; }
    public float FanRadius { get; set; }
    public bool FaceUp { get; set; }
    public float StackOffset { get; set; }
    public float CardScale { get; set; }

    public Deck(DeckDisplayMode displayMode, Vector2 position, bool faceUp = true)
    {
        Cards = new List<Card>();
        DisplayMode = displayMode;
        Position = position;
        FaceUp = faceUp;
        FanSpread = MathHelper.ToRadians(60f);
        FanRadius = 200f;
        StackOffset = 20f;
        CardScale = 1.0f;
    }

    public void AddCard(Card card)
    {
        Cards.Add(card);
        UpdateCardPositions();
    }

    public void AddCards(IEnumerable<Card> cards)
    {
        Cards.AddRange(cards);
        UpdateCardPositions();
    }

    public void UpdateCardPositions()
    {
        if (Cards.Count == 0) return;

        for (int i = 0; i < Cards.Count; i++)
        {
            Cards[i].IsFaceUp = FaceUp;
            Cards[i].Scale = CardScale;
        }

        switch (DisplayMode)
        {
            case DeckDisplayMode.FanUp:
                LayoutFan(isUp: true);
                break;
            case DeckDisplayMode.FanDown:
                LayoutFan(isUp: false);
                break;
            case DeckDisplayMode.Stacked:
                LayoutStacked();
                break;
        }
    }

    private void LayoutFan(bool isUp)
    {
        int count = Cards.Count;
        if (count == 0) return;

        float angleStep = count > 1 ? FanSpread / (count - 1) : 0;
        float startAngle = -FanSpread / 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * angleStep;
            float offsetX = (float)Math.Sin(angle) * FanRadius;
            float offsetY, displayAngle;

            if (isUp)
            {
                offsetY = -(float)Math.Cos(angle) * FanRadius + FanRadius;
                displayAngle = angle;
            }
            else
            {
                offsetY = (float)Math.Cos(angle) * FanRadius - FanRadius;
                displayAngle = -angle;
            }

            Cards[i].Position = Position + new Vector2(offsetX, offsetY);
            Cards[i].Rotation = displayAngle;
        }
    }

    private void LayoutStacked()
    {
        for (int i = 0; i < Cards.Count; i++)
        {
            Cards[i].Position = Position + new Vector2(0, -i * StackOffset);
            Cards[i].Rotation = 0f;
        }
    }
}
