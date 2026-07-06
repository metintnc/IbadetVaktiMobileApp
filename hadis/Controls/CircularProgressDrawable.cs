using System;
using Microsoft.Maui.Graphics;

namespace hadis.Controls
{
    public class CircularProgressDrawable : IDrawable
    {
        // Progress should be 0..1
        public float Progress { get; set; } = 0f;
        public Color BackgroundColor { get; set; } = Colors.LightGray.WithAlpha(0.3f);
        public Color ProgressColor { get; set; } = Colors.White;
        public float StrokeWidth { get; set; } = 6f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            try
            {
                var cx = dirtyRect.Center.X;
                var cy = dirtyRect.Center.Y;
                var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - StrokeWidth / 2;

                // Background circle
                canvas.StrokeColor = BackgroundColor;
                canvas.StrokeSize = StrokeWidth;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawCircle(cx, cy, radius);

                // Progress arc (start at top -90 degrees)
                var sweep = Math.Clamp(Progress, 0f, 1f) * 360f;
                if (sweep > 0.01f)
                {
                    canvas.StrokeColor = ProgressColor;
                    canvas.StrokeSize = StrokeWidth;
                    // DrawArc overload: x, y, width, height, startAngle, sweepAngle, clockwise, includeCenter
                    canvas.DrawArc(cx - radius, cy - radius, radius * 2, radius * 2, -90, sweep, false, false);
                }
            }
            finally
            {
                canvas.RestoreState();
            }
        }
    }
}
