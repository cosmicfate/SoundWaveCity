using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MediocreSoundDesigner;

// draws one compact ruler instead of adding another visible slider.
public sealed class TimelineRuler : Control
{
    // seconds currently represented by the full width of the ruler.
    public double VisibleSeconds { get; set; }

    // the playhead uses the same horizontal pixel position as the track panels.
    public int PlayheadX { get; set; }

    // the playhead color follows the appearance settings.
    public Color MarkerColor { get; set; } = Color.IndianRed;

    private readonly Font headingFont = new("Segoe UI", 8f, FontStyle.Bold);
    private readonly Font tickFont = new("Segoe UI", 9f);

    public TimelineRuler()
    {
        // double buffering prevents the tick marks from flickering during playback.
        DoubleBuffered = true;
        Height = 80;
        Cursor = Cursors.Cross;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            headingFont.Dispose();
            tickFont.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        bool dark = BackColor.GetBrightness() < 0.5f;
        Color muted = dark ? Color.FromArgb(161, 175, 190) : Color.FromArgb(95, 105, 116);
        Color grid = dark ? Color.FromArgb(119, 132, 149) : Color.FromArgb(133, 144, 158);
        Color edge = dark ? Color.FromArgb(70, 82, 97) : Color.FromArgb(205, 211, 218);

        // keep the header small so the waveform workspace has more room.
        TextRenderer.DrawText(g, "TIMELINE", headingFont, new Point(12, 9), ForeColor);

        if (VisibleSeconds > 0 && double.IsFinite(VisibleSeconds))
        {
            DrawTimeTicks(g, grid, muted);
        }
        else
        {
            // the editor has no timing information until a wav file is imported.
            TextRenderer.DrawText(g, "0:00", tickFont, new Point(14, 36), ForeColor);
            if (Width > 470)
                TextRenderer.DrawText(g, "import a wav to show timestamps", tickFont,
                    new Point(94, 9), muted);

            // show small unlabeled ticks instead of a large error-like message.
            using var idlePen = new Pen(grid, 1);
            for (int x = 15; x < Width - 12; x += 16)
                g.DrawLine(idlePen, x, x % 64 == 15 ? 56 : 64, x, Height - 9);
        }

        // the red triangle and vertical line mark the current playback position.
        int markerX = Math.Clamp(PlayheadX, 0, Math.Max(0, Width - 1));
        using var markerPen = new Pen(MarkerColor, 2);
        using var markerBrush = new SolidBrush(MarkerColor);
        g.DrawLine(markerPen, markerX, 56, markerX, Height - 5);
        g.FillPolygon(markerBrush, new[]
        {
            new Point(markerX - 6, 51), new Point(markerX + 6, 51),
            new Point(markerX, 59)
        });
        using var edgePen = new Pen(edge);
        g.DrawLine(edgePen, 0, Height - 1, Width, Height - 1);
    }

    // choose easy-to-read major intervals such as 1, 2, 5, 10 or 20 seconds.
    private void DrawTimeTicks(Graphics g, Color grid, Color muted)
    {
        double targetInterval = VisibleSeconds * 90 / Math.Max(1, Width);
        double power = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(0.001, targetInterval))));
        double majorStep = 10 * power;
        foreach (int multiplier in new[] { 1, 2, 5, 10 })
        {
            if (multiplier * power >= targetInterval)
            {
                majorStep = multiplier * power;
                break;
            }
        }

        // four small subdivisions appear between major timestamp labels.
        double minorStep = majorStep / 4;
        int count = (int)Math.Min(10000, Math.Ceiling(VisibleSeconds / minorStep));
        int previousLabelRight = -100;
        using var tickPen = new Pen(grid);

        for (int i = 0; i <= count; i++)
        {
            double seconds = i * minorStep;
            if (seconds > VisibleSeconds + 0.000001) break;

            int x = (int)Math.Round(seconds / VisibleSeconds * (Width - 1));
            bool major = i % 4 == 0;
            g.DrawLine(tickPen, x, major ? 54 : 63, x, Height - 9);

            if (!major) continue;

            string label = FormatTime(seconds, majorStep);
            Size size = TextRenderer.MeasureText(g, label, tickFont);
            if (x < previousLabelRight + 6 || x + size.Width >= Width - 6) continue;

            TextRenderer.DrawText(g, label, tickFont, new Point(x + 2, 34), ForeColor);
            previousLabelRight = x + size.Width;
        }
    }

    private static string FormatTime(double seconds, double step)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
        if (step < 1)
            return $"{(int)time.TotalMinutes}:{time.Seconds:00}.{time.Milliseconds / 100:0}";
        return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
    }
}
