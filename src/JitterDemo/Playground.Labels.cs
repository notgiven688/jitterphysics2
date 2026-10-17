using System;
using System.Collections.Generic;
using Jitter2.LinearMath;
using JitterDemo.Renderer.OpenGL;
using Vellum;
using Color = Vellum.Rendering.Color;

namespace JitterDemo;

public partial class Playground
{
    private readonly record struct WorldLabel(JVector Position, string Title, string Description);
    private readonly List<WorldLabel> worldLabels = [];

    /// <summary>Queues a world-anchored, noninteractive label for this frame's Vellum overlay.</summary>
    public void DrawWorldLabel(JVector position, string title, string description) =>
        worldLabels.Add(new WorldLabel(position, title, description));

    private void DrawWorldLabels(int logicalWidth, int logicalHeight)
    {
        if (worldLabels.Count == 0) return;
        Matrix4 viewProjection = Camera.ProjectionMatrix * Camera.ViewMatrix;
        float margin = Gui.RootPadding;
        float cardWidth = MathF.Min(Math.Clamp(logicalWidth * 0.18f, 140, 230), logicalWidth - 2 * margin);
        float titleFontSize = logicalWidth < 1000 ? 14 : 16;
        if (cardWidth <= 24 || logicalHeight <= 2 * margin)
        {
            worldLabels.Clear();
            return;
        }

        Gui.Canvas(logicalWidth - 2 * margin, logicalHeight - 2 * margin, canvas =>
        {
            foreach (WorldLabel label in worldLabels)
            {
                Vector4 clip = viewProjection * new Vector4(Conversion.FromJitter(label.Position), 1);
                // The camera uses a zero-to-one depth projection. Cull instead of clamping
                // offscreen anchors, which would stack unrelated labels along the window edge.
                if (!(clip.W > 0) || !float.IsFinite(clip.W) || clip.Z < 0 || clip.Z > clip.W ||
                    MathF.Abs(clip.X) > clip.W || MathF.Abs(clip.Y) > clip.W) continue;

                float screenX = (clip.X / clip.W * 0.5f + 0.5f) * logicalWidth;
                float screenY = (0.5f - clip.Y / clip.W * 0.5f) * logicalHeight;
                float textWidth = cardWidth - 20;
                var titleSize = canvas.MeasureText(label.Title, size: titleFontSize, maxWidth: textWidth,
                    wrap: TextWrapMode.WordWrap);
                var descriptionSize = canvas.MeasureText(label.Description, size: 13, maxWidth: textWidth,
                    wrap: TextWrapMode.WordWrap);
                float cardHeight = titleSize.Y + descriptionSize.Y + 25;
                float left = screenX - cardWidth / 2;
                if (left < margin || left + cardWidth > logicalWidth - margin) continue;
                float x = left - canvas.X;
                float y = screenY - cardHeight - 10 - canvas.Y;
                // Keep the attachment visible: a label above the viewport is simply hidden.
                if (y < 0 || y + cardHeight > canvas.Height) continue;
                // Keep labels out of the translucent statistics window, where the two
                // text layers would otherwise be visible on top of each other.
                float statsWidth = statsWindowState.Size.X > 0 ? statsWindowState.Size.X : 260;
                float statsHeight = statsWindowState.Collapsed ? 30 : statsWindowState.Size.Y;
                if (statsWindowState.Open && left < statsWindowState.Position.X + statsWidth &&
                    left + cardWidth > statsWindowState.Position.X &&
                    y + canvas.Y < statsWindowState.Position.Y + statsHeight &&
                    y + canvas.Y + cardHeight > statsWindowState.Position.Y) continue;

                canvas.DrawRect(x, y, cardWidth, cardHeight, Gui.Theme.PanelBg,
                    Gui.Theme.PanelBorder, strokeWidth: 1, radius: 6);
                canvas.DrawText(label.Title, x + 10, y + 9, size: titleFontSize, color: new Color(255, 200, 90),
                    maxWidth: textWidth, wrap: TextWrapMode.WordWrap);
                canvas.DrawText(label.Description, x + 10, y + titleSize.Y + 15, size: 13,
                    color: new Color(232, 236, 243), maxWidth: textWidth, wrap: TextWrapMode.WordWrap);
            }
        });
        worldLabels.Clear();
    }
}
