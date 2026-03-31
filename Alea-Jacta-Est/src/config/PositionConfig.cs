using Microsoft.Xna.Framework;
using Alea_Jacta_Est.Services;
using Alea_Jacta_Est.Entities;

namespace Alea_Jacta_Est.Config;

/// <summary>Configuration for all positions and rendering.</summary>
public static class PositionConfig
{
    // Reference resolution for 16:9 aspect ratio
    public const int ReferenceWidth = 960;
    public const int ReferenceHeight = 540;

    /// <summary>Gets the deck configuration for a specific player and deck type.</summary>
    /// <summary>Returns null if the deck type has no visual config (should be skipped during rendering).</summary>
    public static DeckConfig? GetDeckConfig(Player player, DeckType deckType)
    {
        if (player.IsLocalPlayer)
        {
            return deckType switch
            {
                DeckType.MainDeck        => LocalPlayer.MainDeck,
                DeckType.SpecialDeck     => LocalPlayer.SpecialDeck,
                DeckType.DiscardDeck     => LocalPlayer.DiscardDeck,
                DeckType.BoardDeck0      => LocalPlayer.BoardDecks[0],
                DeckType.BoardDeck1      => LocalPlayer.BoardDecks[1],
                DeckType.BoardDeck2      => LocalPlayer.BoardDecks[2],
                DeckType.BoardDeck3      => LocalPlayer.BoardDecks[3],
                DeckType.HandDeck        => LocalPlayer.HandDeck,
                DeckType.ArcanaHandDeck  => LocalPlayer.ArcanaHandDeck,
                DeckType.ArcanaDiscardDeck => LocalPlayer.ArcanaDiscardDeck,
                _                        => null
            };
        }
        else
        {
            int opponentIndex = player.Id - 1;
            if (opponentIndex < 0 || opponentIndex > 3)
                opponentIndex = 0;

            return deckType switch
            {
                DeckType.MainDeck    => Opponents.GetMainDeck(opponentIndex),
                DeckType.SpecialDeck => Opponents.GetSpecialDeck(opponentIndex),
                DeckType.DiscardDeck => Opponents.GetDiscardDeck(opponentIndex),
                DeckType.BoardDeck0  => Opponents.GetBoardDeck(opponentIndex, 0),
                DeckType.BoardDeck1  => Opponents.GetBoardDeck(opponentIndex, 1),
                DeckType.BoardDeck2  => Opponents.GetBoardDeck(opponentIndex, 2),
                DeckType.BoardDeck3  => Opponents.GetBoardDeck(opponentIndex, 3),
                // HandDeck/ArcanaHandDeck/ArcanaDiscardDeck are hidden for opponents
                _                    => null
            };
        }
    }

    /// <summary>Gets the actual deck from a player based on deck type.</summary>
    public static Deck GetDeck(Player player, DeckType deckType)
    {
        string deckName = deckType.ToString();
        return player.Decks[deckName];
    }

    /// <summary>Represents a deck's visual configuration.</summary>
    public class DeckConfig
    {
        public Vector2 RelativePosition { get; set; } // 0-1 range
        public DeckDisplayMode DisplayMode { get; set; }
        public bool IsFrontVisible { get; set; }
        public float BaseCardScale { get; set; }
        public float FanSpreadDegrees { get; set; }
        public float FanRadius { get; set; }
        public float StackOffset { get; set; }

        public DeckConfig(
            Vector2 relativePosition,
            DeckDisplayMode displayMode,
            bool isFrontVisible,
            float baseCardScale = 0.5f,
            float fanSpreadDegrees = 60f,
            float fanRadius = 160f,
            float stackOffset = 15f)
        {
            RelativePosition = relativePosition;
            DisplayMode = displayMode;
            IsFrontVisible = isFrontVisible;
            BaseCardScale = baseCardScale;
            FanSpreadDegrees = fanSpreadDegrees;
            FanRadius = fanRadius;
            StackOffset = stackOffset;
        }
    }

    // Local player deck positions (bottom of screen)
    public static class LocalPlayer
    {
        // Draw pile (face-down, small stacked pile on the left)
        public static readonly DeckConfig MainDeck = new(
            relativePosition: new Vector2(0.05f, 0.75f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: false,
            baseCardScale: 0.7f,
            stackOffset: 4f
        );

        // Playable hand (face-up fan, center-bottom)
        public static readonly DeckConfig HandDeck = new(
            relativePosition: new Vector2(0.5f, 0.75f),
            displayMode: DeckDisplayMode.FanUp,
            isFrontVisible: true,
            baseCardScale: 0.7f,
            fanSpreadDegrees: 85f,
            fanRadius: 380f
        );

        // Arcana hand card (face-up stacked, next to SpecialDeck)
        public static readonly DeckConfig ArcanaHandDeck = new(
            relativePosition: new Vector2(0.17f, 0.72f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: true,
            baseCardScale: 0.9f,
            stackOffset: 4f
        );

        // Arcana discard (face-down, far right)
        public static readonly DeckConfig ArcanaDiscardDeck = new(
            relativePosition: new Vector2(0.95f, 0.72f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: false,
            baseCardScale: 0.9f,
            stackOffset: 4f
        );

        public static readonly DeckConfig SpecialDeck = new(
            relativePosition: new Vector2(0.125f, 0.72f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: false,
            baseCardScale: 0.9f,
            stackOffset: 20f
        );

        public static readonly DeckConfig DiscardDeck = new(
            relativePosition: new Vector2(0.875f, 0.72f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: true,
            baseCardScale: 0.9f,
            stackOffset: 20f
        );

        private const float BoardDecksHeight = 0.5f;
        private const float BoardDecksBaseCardScale = 0.5f;

        public static readonly DeckConfig[] BoardDecks =
        {
            new DeckConfig(
                relativePosition: new Vector2(0.4f, BoardDecksHeight),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: true,
                baseCardScale: BoardDecksBaseCardScale,
                stackOffset: 10f
            ),
            new DeckConfig(
                relativePosition: new Vector2(0.4666667f, BoardDecksHeight),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: true,
                baseCardScale: BoardDecksBaseCardScale,
                stackOffset: 10f
            ),
            new DeckConfig(
                relativePosition: new Vector2(0.5333334f, BoardDecksHeight),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: true,
                baseCardScale: BoardDecksBaseCardScale,
                stackOffset: 10f
            ),
            new DeckConfig(
                relativePosition: new Vector2(0.6f, BoardDecksHeight),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: true,
                baseCardScale: BoardDecksBaseCardScale,
                stackOffset: 10f
            )
        };
    }

    public static class Opponents
    {
        // Returns config for opponent at given index (0-3)
        public static DeckConfig GetMainDeck(int opponentIndex)
        {
            float[] xPositions = { 0.15f, 0.35f, 0.65f, 0.85f };
            return new DeckConfig(
                relativePosition: new Vector2(xPositions[opponentIndex], 0.15f),
                displayMode: DeckDisplayMode.FanDown,
                isFrontVisible: false,
                baseCardScale: 0.3f,
                fanSpreadDegrees: 40f,
                fanRadius: 60f
            );
        }

        public static DeckConfig GetSpecialDeck(int opponentIndex)
        {
            float[] xPositions = { 0.09f, 0.29f, 0.59f, 0.79f };
            return new DeckConfig(
                relativePosition: new Vector2(xPositions[opponentIndex], 0.1f),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: false,
                baseCardScale: 0.3f,
                stackOffset: 10f
            );
        }

        public static DeckConfig GetDiscardDeck(int opponentIndex)
        {
            float[] xPositions = { 0.21f, 0.41f, 0.71f, 0.91f };
            return new DeckConfig(
                relativePosition: new Vector2(xPositions[opponentIndex], 0.1f),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: false,
                baseCardScale: 0.3f,
                stackOffset: 10f
            );
        }

        public static DeckConfig GetBoardDeck(int opponentIndex, int boardDeckIndex)
        {
            // Compact board decks for opponents
            float baseX = opponentIndex switch
            {
                0 => 0.15f,
                1 => 0.35f,
                2 => 0.65f,
                3 => 0.85f,
                _ => 0.5f
            };

            float xOffset = (boardDeckIndex - 1.5f) * 0.04f;

            return new DeckConfig(
                relativePosition: new Vector2(baseX + xOffset, 0.28f),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: false,
                baseCardScale: 0.3f,
                stackOffset: 6f
            );
        }
    }
}
