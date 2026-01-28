using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Alea_Jacta_Est.Utils;

/// <summary>Manages the game viewport with 16:9 aspect ratio scaling.</summary>
public class GameViewport
{
    public const float TargetAspectRatio = 16f / 9f;

    public Rectangle ViewportBounds { get; private set; }
    public float Scale { get; private set; }
    public Vector2 Offset { get; private set; }

    /// <summary>Updates the viewport based on the actual screen size.</summary>
    public void Update(int screenWidth, int screenHeight)
    {
        float screenAspect = (float)screenWidth / screenHeight;

        if (screenAspect > TargetAspectRatio)
        {
            // Screen is wider than 16:9 - letterbox on sides
            int viewportWidth = (int)(screenHeight * TargetAspectRatio);
            int viewportHeight = screenHeight;
            int offsetX = (screenWidth - viewportWidth) / 2;

            ViewportBounds = new Rectangle(offsetX, 0, viewportWidth, viewportHeight);
            Scale = viewportHeight / 1080f;
            Offset = new Vector2(offsetX, 0);
        }
        else
        {
            // Screen is taller than 16:9 - letterbox on top/bottom
            int viewportWidth = screenWidth;
            int viewportHeight = (int)(screenWidth / TargetAspectRatio);
            int offsetY = (screenHeight - viewportHeight) / 2;

            ViewportBounds = new Rectangle(0, offsetY, viewportWidth, viewportHeight);
            Scale = viewportWidth / 1920f;
            Offset = new Vector2(0, offsetY);
        }
    }

    /// <summary>Converts a relative position (0-1) to screen coordinates.</summary>
    public Vector2 RelativeToScreen(Vector2 relativePosition)
    {
        return new Vector2(
            ViewportBounds.X + relativePosition.X * ViewportBounds.Width,
            ViewportBounds.Y + relativePosition.Y * ViewportBounds.Height
        );
    }

    /// <summary>Scales a value based on the viewport scale.</summary>
    public float ScaleValue(float value)
    {
        return value * Scale;
    }

    /// <summary>Converts screen coordinates to relative position (0-1).</summary>
    public Vector2 ScreenToRelative(Vector2 screenPosition)
    {
        return new Vector2(
            (screenPosition.X - ViewportBounds.X) / ViewportBounds.Width,
            (screenPosition.Y - ViewportBounds.Y) / ViewportBounds.Height
        );
    }
}
