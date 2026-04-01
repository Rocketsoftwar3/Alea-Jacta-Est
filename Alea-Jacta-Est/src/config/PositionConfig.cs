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
        return player.Decks[deckType];
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

    // HUD panel width in relative coords (270px / 960px reference)
    // All card positions must be > HudLeft to avoid rendering under the HUD.
    private const float HudLeft = 0.30f;

    // Local player deck positions (bottom of screen, shifted right of HUD)
    public static class LocalPlayer
    {
        // Draw pile (face-down, stacked — far left of content area)
        public static readonly DeckConfig MainDeck = new(
            relativePosition: new Vector2(0.33f, 0.80f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: false,
            baseCardScale: 0.7f,
            stackOffset: 4f
        );

        // Arcana draw pile (face-down)
        public static readonly DeckConfig SpecialDeck = new(
            relativePosition: new Vector2(0.38f, 0.82f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: false,
            baseCardScale: 0.9f,
            stackOffset: 20f
        );

        // Arcana hand (face-up, left of center)
        public static readonly DeckConfig ArcanaHandDeck = new(
            relativePosition: new Vector2(0.44f, 0.78f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: true,
            baseCardScale: 0.9f,
            stackOffset: 4f
        );

        // Playable hand — fan centered in content area (x=0.63 keeps all cards right of HUD)
        public static readonly DeckConfig HandDeck = new(
            relativePosition: new Vector2(0.63f, 0.80f),
            displayMode: DeckDisplayMode.FanUp,
            isFrontVisible: true,
            baseCardScale: 0.7f,
            fanSpreadDegrees: 75f,
            fanRadius: 290f
        );

        // Arcana discard (face-down, far right)
        public static readonly DeckConfig ArcanaDiscardDeck = new(
            relativePosition: new Vector2(0.93f, 0.82f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: false,
            baseCardScale: 0.9f,
            stackOffset: 4f
        );

        // Value discard (face-up, right side)
        public static readonly DeckConfig DiscardDeck = new(
            relativePosition: new Vector2(0.88f, 0.80f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: true,
            baseCardScale: 0.9f,
            stackOffset: 20f
        );

        // Board decks — spread around center of content area at mid-height
        private const float BoardDecksY = 0.52f;
        private const float BoardDecksBaseCardScale = 0.55f;

        public static readonly DeckConfig[] BoardDecks =
        {
            new DeckConfig(
                relativePosition: new Vector2(0.50f, BoardDecksY),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: true,
                baseCardScale: BoardDecksBaseCardScale,
                stackOffset: 10f
            ),
            new DeckConfig(
                relativePosition: new Vector2(0.57f, BoardDecksY),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: true,
                baseCardScale: BoardDecksBaseCardScale,
                stackOffset: 10f
            ),
            new DeckConfig(
                relativePosition: new Vector2(0.64f, BoardDecksY),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: true,
                baseCardScale: BoardDecksBaseCardScale,
                stackOffset: 10f
            ),
            new DeckConfig(
                relativePosition: new Vector2(0.71f, BoardDecksY),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: true,
                baseCardScale: BoardDecksBaseCardScale,
                stackOffset: 10f
            )
        };
    }

    public static class Opponents
    {
        // 4 opponents evenly distributed in the content area (right of HUD)
        // Content area: ~0.30 to ~0.97  →  centers at 0.38, 0.55, 0.72, 0.89
        private static readonly float[] MainX    = { 0.38f, 0.55f, 0.72f, 0.89f };
        private static readonly float[] SpecialX = { 0.32f, 0.49f, 0.66f, 0.83f };
        private static readonly float[] DiscardX = { 0.44f, 0.61f, 0.78f, 0.95f };

        public static DeckConfig GetMainDeck(int opponentIndex) => new(
            relativePosition: new Vector2(MainX[opponentIndex], 0.15f),
            displayMode: DeckDisplayMode.FanDown,
            isFrontVisible: false,
            baseCardScale: 0.3f,
            fanSpreadDegrees: 40f,
            fanRadius: 60f
        );

        public static DeckConfig GetSpecialDeck(int opponentIndex) => new(
            relativePosition: new Vector2(SpecialX[opponentIndex], 0.10f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: false,
            baseCardScale: 0.3f,
            stackOffset: 10f
        );

        public static DeckConfig GetDiscardDeck(int opponentIndex) => new(
            relativePosition: new Vector2(DiscardX[opponentIndex], 0.10f),
            displayMode: DeckDisplayMode.Stacked,
            isFrontVisible: false,
            baseCardScale: 0.3f,
            stackOffset: 10f
        );

        public static DeckConfig GetBoardDeck(int opponentIndex, int boardDeckIndex)
        {
            float baseX = MainX[opponentIndex < MainX.Length ? opponentIndex : 0];
            float xOffset = (boardDeckIndex - 1.5f) * 0.035f;
            return new DeckConfig(
                relativePosition: new Vector2(baseX + xOffset, 0.27f),
                displayMode: DeckDisplayMode.Stacked,
                isFrontVisible: false,
                baseCardScale: 0.3f,
                stackOffset: 6f
            );
        }
    }
}
