using System;
using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est;

namespace Alea_Jacta_Est.Utils;

public static class UIHelper
{
    public static void DrawMuteButton(Vector2 mutePos)
    {
        ImGui.SetCursorPos(mutePos);
        
        bool isMuted = Game1.BgmInstance != null && Game1.BgmInstance.Volume == 0f;

        var screenPos = ImGui.GetCursorScreenPos();
        var muteSize = new Vector2(32, 24);
        
        if (ImGui.InvisibleButton("mute_btn_global", muteSize))
        {
            if (Game1.BgmInstance != null)
            {
                Game1.BgmInstance.Volume = isMuted ? 0.5f : 0f;
            }
        }
        
        bool hovered = ImGui.IsItemHovered();
        bool active = ImGui.IsItemActive();
        
        uint speakerCol = ImGui.ColorConvertFloat4ToU32(hovered ? new Vector4(1f, 0.8f, 0.2f, 1f) : new Vector4(0.85f, 0.65f, 0.15f, 1f));
        uint darkOutline = ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1f));

        float oy = active ? 1f : 0f;
        var dl = ImGui.GetWindowDrawList();
        
        float bx = screenPos.X + 4;
        float by = screenPos.Y + 8 + oy;
        float bw = 6;
        float bh = 8;
        dl.AddRectFilled(new Vector2(bx, by), new Vector2(bx + bw, by + bh), speakerCol);
        dl.AddRect(new Vector2(bx, by), new Vector2(bx + bw, by + bh), darkOutline, 0f, 0, 2f);

        var p1 = new Vector2(bx + bw, by + bh / 2f);
        var p2 = new Vector2(bx + bw + 10, by - 4);
        var p3 = new Vector2(bx + bw + 10, by + bh + 4);
        dl.AddTriangleFilled(p1, p2, p3, speakerCol);
        dl.AddTriangle(p1, p2, p3, darkOutline, 2f);
        
        dl.AddLine(new Vector2(bx + bw, by), new Vector2(bx + bw, by + bh), speakerCol, 2f);

        if (isMuted)
        {
            float xx = bx + bw + 14;
            float xy = by + 1;
            dl.AddLine(new Vector2(xx, xy), new Vector2(xx + 6, xy + 6), darkOutline, 2f);
            dl.AddLine(new Vector2(xx + 6, xy), new Vector2(xx, xy + 6), darkOutline, 2f);
        }
        else
        {
            float cx = bx + bw + 6;
            float cy = by + bh / 2f;
            dl.PathArcTo(new Vector2(cx, cy), 6f, -MathF.PI / 4f, MathF.PI / 4f);
            dl.PathStroke(darkOutline, ImDrawFlags.None, 2f);
            
            dl.PathArcTo(new Vector2(cx, cy), 10f, -MathF.PI / 3f, MathF.PI / 3f);
            dl.PathStroke(darkOutline, ImDrawFlags.None, 2f);
        }
    }

    public static bool DrawPixelButton(string id, string text, Vector2 size, Vector4 baseColor, bool disabled = false)
    {
        var pos = ImGui.GetCursorScreenPos();
        bool pressed = false;
        
        if (disabled) ImGui.BeginDisabled();
        if (ImGui.InvisibleButton(id, size))
        {
            pressed = true;
        }
        if (disabled) ImGui.EndDisabled();

        bool hovered = ImGui.IsItemHovered() && !disabled;
        bool active = ImGui.IsItemActive() && !disabled;

        var dl = ImGui.GetWindowDrawList();
        
        Vector4 fillCol = baseColor;
        if (disabled)
        {
            float gray = (fillCol.X + fillCol.Y + fillCol.Z) / 3f;
            fillCol = new Vector4(gray * 0.7f, gray * 0.7f, gray * 0.7f, 1f);
        }
        else if (active)
        {
            fillCol = new Vector4(fillCol.X * 0.8f, fillCol.Y * 0.8f, fillCol.Z * 0.8f, 1f);
        }
        else if (hovered)
        {
            fillCol = new Vector4(Math.Min(1f, fillCol.X * 1.2f), Math.Min(1f, fillCol.Y * 1.2f), Math.Min(1f, fillCol.Z * 1.2f), 1f);
        }

        uint fill = ImGui.ColorConvertFloat4ToU32(fillCol);
        
        Vector4 hl = disabled ? fillCol : new Vector4(Math.Min(1f, fillCol.X + 0.3f), Math.Min(1f, fillCol.Y + 0.3f), Math.Min(1f, fillCol.Z + 0.3f), 1f);
        uint highlight = ImGui.ColorConvertFloat4ToU32(hl);
        
        Vector4 sh = disabled ? new Vector4(fillCol.X * 0.5f, fillCol.Y * 0.5f, fillCol.Z * 0.5f, 1f) : new Vector4(fillCol.X * 0.4f, fillCol.Y * 0.4f, fillCol.Z * 0.4f, 1f);
        uint shadow = ImGui.ColorConvertFloat4ToU32(sh);
        
        uint border = ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1f));

        float dx = active ? 2f : 0f;
        float dy = active ? 2f : 0f;

        Vector2 tl = pos + new Vector2(dx, dy);
        Vector2 br = pos + size + new Vector2(dx, dy);

        dl.AddRect(tl - new Vector2(1, 1), br + new Vector2(1, 1), border, 4f, ImDrawFlags.RoundCornersAll, 2f);
        dl.AddRectFilled(tl, br, fill, 2f);

        if (active)
        {
            dl.AddLine(tl, new Vector2(br.X, tl.Y), shadow, 2f);
            dl.AddLine(tl, new Vector2(tl.X, br.Y), shadow, 2f);
        }
        else
        {
            dl.AddLine(tl + new Vector2(2, 2), new Vector2(br.X - 2, tl.Y + 2), highlight, 2f);
            dl.AddLine(tl + new Vector2(2, 2), new Vector2(tl.X + 2, br.Y - 2), highlight, 2f);
            
            dl.AddLine(new Vector2(tl.X + 2, br.Y - 2), br - new Vector2(2, 2), shadow, 2f);
            dl.AddLine(new Vector2(br.X - 2, tl.Y + 2), br - new Vector2(2, 2), shadow, 2f);
        }
        
        if (disabled)
        {
            dl.AddLine(tl + new Vector2(10, 2), tl + new Vector2(14, 8), shadow, 1f);
            dl.AddLine(tl + new Vector2(14, 8), tl + new Vector2(12, 12), shadow, 1f);
            dl.AddLine(br - new Vector2(15, 2), br - new Vector2(10, 6), shadow, 1f);
        }

        Vector2 textSize = ImGui.CalcTextSize(text);
        Vector2 textPos = tl + (size - textSize) / 2f;
        dl.AddText(textPos, ImGui.ColorConvertFloat4ToU32(disabled ? new Vector4(0.5f, 0.5f, 0.5f, 1f) : new Vector4(1f, 1f, 1f, 1f)), text);

        ImGui.Dummy(size);
        return pressed;
    }
}
