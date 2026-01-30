using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Alea_Jacta_Est.Services;

/// <summary>Handles input and window state management.</summary>
public class InputService
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GameWindow _window;
    private readonly Form _gameForm;

    private KeyboardState _previousKeyboardState;
    private bool _manualFullscreenExit = false;

    public InputService(GraphicsDeviceManager graphics, GameWindow window)
    {
        _graphics = graphics;
        _window = window;
        _gameForm = (Form)Control.FromHandle(window.Handle);
        _previousKeyboardState = Keyboard.GetState();
    }

    /// <summary>Updates input state and handles fullscreen toggle.</summary>
    public void Update()
    {
        var keyboardState = Keyboard.GetState();

        // Handle fullscreen toggle with F11
        if (_gameForm != null)
        {
            if (keyboardState.IsKeyDown(Keys.F11) && !_previousKeyboardState.IsKeyDown(Keys.F11))
            {
                if (_graphics.IsFullScreen)
                {
                    _graphics.ToggleFullScreen();
                    _manualFullscreenExit = true;
                }
                else if (_gameForm.WindowState == FormWindowState.Maximized)
                {
                    _gameForm.WindowState = FormWindowState.Normal;
                }
                else
                {
                    _gameForm.WindowState = FormWindowState.Maximized;
                }
            }

            if (_manualFullscreenExit && !_graphics.IsFullScreen)
            {
                _gameForm.WindowState = FormWindowState.Normal;
                _manualFullscreenExit = false;
            }

            if (!_graphics.IsFullScreen && _gameForm.WindowState == FormWindowState.Maximized)
            {
                _graphics.ToggleFullScreen();
            }
        }

        _previousKeyboardState = keyboardState;
    }

    /// <summary>Checks if the escape key is pressed.</summary>
    public bool IsExitRequested()
    {
        var keyboardState = Keyboard.GetState();
        return keyboardState.IsKeyDown(Keys.Escape) ||
               GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
    }
}
