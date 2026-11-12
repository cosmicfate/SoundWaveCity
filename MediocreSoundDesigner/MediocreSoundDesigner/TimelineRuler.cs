using System;
using System.Drawing;
using System.Windows.Forms;

namespace MediocreSoundDesigner;

// draws timeline ticks and labels without modifying any audio data.
public sealed class TimelineRuler : Control
{
    public double VisibleSeconds { get; set; }
    public int PlayheadX { get; set; }
    public Color MarkerColor { get; set; } = Color.IndianRed;

    public TimelineRuler()
    {
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 8);
        Height = 28;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);
        using var ticks = new Pen(Color.FromArgb(140, ForeColor));
        using var marker = new Pen(MarkerColor, 2);
        using var brush = new SolidBrush(ForeColor);

        if (VisibleSeconds <= 0 || Width < 2)
            g.DrawString("timeline · duration unavailable", Font, brush, 4, 2);
        else
        {
            // select readable intervals as the available time range changes.
            double target = VisibleSeconds * 90 / (Width - 1);
            double power = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(0.001, target))));
            double interval = power;
            foreach (int factor in new[] { 1, 2, 5, 10 })
                if (factor * power >= target) { interval = factor * power; break; }

            for (int i = 0; i < 10000; i++)
            {
                double time = i * interval / 4;
                if (time > VisibleSeconds) break;
                int x = (int)(time / VisibleSeconds * (Width - 1));
                bool major = i % 4 == 0;
                g.DrawLine(ticks, x, major ? 15 : 21, x, Height - 1);
                if (major && x < Width - 36)
                {
                    TimeSpan t = TimeSpan.FromSeconds(time);
                    string label = interval < 1 ? t.ToString(@"m\:ss\.f")
                        : t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
                    g.DrawString(label, Font, brush, x + 2, 0);
                }
            }
        }
        int playhead = Math.Clamp(PlayheadX, 0, Math.Max(0, Width - 1));
        g.DrawLine(marker, playhead, 13, playhead, Height - 1);
    }
}
