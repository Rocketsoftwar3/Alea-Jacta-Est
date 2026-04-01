using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Numerics;

namespace Alea_Jacta_Est.Services;

public class IntroDialogueService
{
    private readonly string[] _dialogues = 
    {
        "« Te voilà enfin éveillé… jeune damné. J’attendais ce moment. »",
        "« Je perçois ton désir de fuir ce royaume. Parfait… j’ai justement un divertissement à te proposer. »",
        "« Écoute bien, car ta destinée se joue ici et maintenant. Parmi toi et tes trois compagnons, un seul franchira ces portes… les autres tomberont dans l’oubli éternel. Trahis, combats, survis… peu m’importe. Montre-moi jusqu’où tu es prêt à sombrer pour retrouver ta vie. »"
    };

    private int _currentLine = 0;
    private float _charsVisible = 0f;
    private const float CharsPerSecond = 45f;

    private bool _isComplete = false;
    public float FadeAlpha { get; private set; } = 1f;

    private nint _avatarTextureId = IntPtr.Zero;
    private Texture2D _dynamicAvatar;
    private string _loadError = "Aucune image trouvée";

    private bool _advanceRequested = false;
    private bool _prevSpace = false;
    private bool _prevEnter = false;

    public bool IsFinished => _isComplete && FadeAlpha <= 0f;

    public void LoadAvatar(GraphicsDevice gd, Alea_Jacta_Est.ImGuiBackend.ImGuiRenderer renderer)
    {
        try
        {
            string baseDir = AppContext.BaseDirectory;
            string[] possiblePaths = {
                Path.Combine(baseDir, "Content", "dialogue_avatar.png"),
                Path.Combine(baseDir, "dialogue_avatar.png"),
                Path.Combine(baseDir, "Content", "dialog_avatar.png"),
                Path.Combine(baseDir, "dialog_avatar.png"),
                "dialogue_avatar.png",
                Path.Combine("Content", "dialogue_avatar.png"),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Content\dialogue_avatar.png")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\dialogue_avatar.png")),
                "dialog_avatar.png",
                Path.Combine("Content", "dialog_avatar.png"),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Content\dialog_avatar.png")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\dialog_avatar.png"))
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _loadError = "Image trouvee, chargement: " + path;
                    using var stream = File.OpenRead(path);
                    _dynamicAvatar = Texture2D.FromStream(gd, stream);
                    _avatarTextureId = renderer.GetOrBindTexture(_dynamicAvatar);
                    _loadError = "";
                    break;
                }
            }
        }
        catch (Exception ex) 
        { 
            _loadError = "Erreur: " + ex.Message;
        }
    }

    public void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        if (_isComplete)
        {
            FadeAlpha -= (float)gameTime.ElapsedGameTime.TotalSeconds * 0.8f;
            return;
        }

        string currentText = _dialogues[_currentLine];
        if (_charsVisible < currentText.Length)
        {
            _charsVisible += CharsPerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        var ks = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        bool spaceDown = ks.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space);
        bool enterDown = ks.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter);

        if ((spaceDown && !_prevSpace) || (enterDown && !_prevEnter))
        {
            _advanceRequested = true;
        }
        _prevSpace = spaceDown;
        _prevEnter = enterDown;
    }

    public void Render(float dt)
    {
        if (IsFinished) return;

        var io = ImGui.GetIO();
        float W = io.DisplaySize.X;
        float H = io.DisplaySize.Y;

        ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(W, H), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.SetNextWindowBgAlpha(FadeAlpha * 0.98f);

        var wFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove
                   | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
                   | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNav;

        if (ImGui.Begin("##IntroDialog", wFlags))
        {
            var dl = ImGui.GetWindowDrawList();

            // Background full-screen image cover (tinted)
            if (_avatarTextureId != IntPtr.Zero && _dynamicAvatar != null)
            {
                float screenAspect = W / H;
                float imgAspect = (float)_dynamicAvatar.Width / _dynamicAvatar.Height;
                Vector2 uv0 = Vector2.Zero;
                Vector2 uv1 = Vector2.One;

                if (screenAspect > imgAspect)
                {
                    float crop = 1f - (imgAspect / screenAspect);
                    uv0.Y = crop / 2f;
                    uv1.Y = 1f - (crop / 2f);
                }
                else
                {
                    float crop = 1f - (screenAspect / imgAspect);
                    uv0.X = crop / 2f;
                    uv1.X = 1f - (crop / 2f);
                }

                uint sceneBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.2f, 0.4f, FadeAlpha * 0.35f));
                dl.AddImage(_avatarTextureId, Vector2.Zero, new Vector2(W, H), uv0, uv1, sceneBg);
            }

            float boxW = Math.Clamp(W * 0.7f, 800f, 1200f);
            float boxH = 260f;
            float boxX = (W - boxW) / 2f;
            float boxY = H - boxH - 60f;
            
            var tl = new Vector2(boxX, boxY);
            var br = new Vector2(boxX + boxW, boxY + boxH);

            dl.AddRectFilled(tl, br, ImGui.ColorConvertFloat4ToU32(new Vector4(0.04f, 0.04f, 0.06f, FadeAlpha * 0.98f)), 8f);
            dl.AddRect(tl, br, ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.2f, 0.6f, FadeAlpha * 0.9f)), 8f, ImDrawFlags.RoundCornersAll, 3f);
            dl.AddRect(tl + new Vector2(4, 4), br - new Vector2(4, 4), ImGui.ColorConvertFloat4ToU32(new Vector4(0.7f, 0.6f, 0.2f, FadeAlpha * 0.5f)), 6f, ImDrawFlags.RoundCornersAll, 1f);

            // Avatar
            float avatarSize = 180f;
            var avatarPos = new Vector2(boxX + 40f, boxY + 40f);
            
            dl.AddRectFilled(avatarPos, avatarPos + new Vector2(avatarSize, avatarSize), ImGui.ColorConvertFloat4ToU32(new Vector4(0.02f, 0.02f, 0.02f, FadeAlpha)));
            dl.AddRect(avatarPos, avatarPos + new Vector2(avatarSize, avatarSize), ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.3f, 0.5f, FadeAlpha)), 0f, ImDrawFlags.None, 2f);

            if (_avatarTextureId != IntPtr.Zero)
            {
                dl.AddImage(_avatarTextureId, avatarPos, avatarPos + new Vector2(avatarSize, avatarSize), Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, FadeAlpha)));
            }
            else
            {
                dl.AddText(avatarPos + new Vector2(10, 80), ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.2f, 0.4f, FadeAlpha)), _loadError);
            }

            if (!_isComplete)
            {
                string fullText = _dialogues[_currentLine];
                int charsToShow = (int)Math.Clamp(MathF.Floor(_charsVisible), 0, fullText.Length);
                string visibleText = fullText.Substring(0, charsToShow);

                ImGui.SetCursorPos(new Vector2(boxX + 260f, boxY + 45f));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.95f, 0.9f, 1f, FadeAlpha));
                ImGui.SetWindowFontScale(1.35f); 
                ImGui.PushTextWrapPos(boxX + boxW - 50f);
                ImGui.TextUnformatted(visibleText);
                ImGui.PopTextWrapPos();
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                bool isTyping = charsToShow < fullText.Length;
                string btnLabel = isTyping ? "Accelerer >>" : "Suivant >";
                if (_currentLine == _dialogues.Length - 1 && !isTyping) btnLabel = "S'eveiller";

                ImGui.SetCursorPos(new Vector2(boxX + boxW - 170f, boxY + boxH - 55f));
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, FadeAlpha);
                
                bool btnClicked = Utils.UIHelper.DrawPixelButton("intro_next", btnLabel, new Vector2(140, 35), new Vector4(0.4f, 0.15f, 0.5f, 1f), false);
                
                if (btnClicked || _advanceRequested)
                {
                    _advanceRequested = false;
                    if (isTyping)
                    {
                        _charsVisible = fullText.Length;
                    }
                    else if (_currentLine < _dialogues.Length - 1)
                    {
                        _currentLine++;
                        _charsVisible = 0f;
                    }
                    else
                    {
                        _isComplete = true; // Trigger fade out
                    }
                }

                ImGui.SetCursorPos(new Vector2(W - 140f, 40f));
                if (Utils.UIHelper.DrawPixelButton("intro_skip", "Skip Intro", new Vector2(100, 30), new Vector4(0.3f, 0.1f, 0.1f, 1f), false))
                {
                    _isComplete = true;
                }
                ImGui.PopStyleVar();
            }
        }
        ImGui.End();
        ImGui.PopStyleVar();
    }
}
