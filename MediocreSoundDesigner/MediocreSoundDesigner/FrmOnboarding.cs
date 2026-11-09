using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MediocreSoundDesigner;

/// <summary>First-launch onboarding; built in code so no Designer/resx is required.</summary>
public sealed class FrmOnboarding : Form
{
    private readonly OnboardingPreferences prefs;
    private readonly Color bg = Color.FromArgb(45, 51, 57);
    private readonly Color surface = Color.FromArgb(49, 56, 64);
    private readonly Color border = Color.FromArgb(70, 77, 85);
    private readonly Color muted = Color.FromArgb(170, 180, 194);
    private readonly Color[] accents = {
        Color.FromArgb(255, 87, 91), Color.FromArgb(247, 136, 22),
        Color.FromArgb(189, 152, 25), Color.FromArgb(36, 173, 80),
        Color.FromArgb(37, 151, 251), Color.FromArgb(148, 105, 239),
        Color.FromArgb(223, 74, 181)
    };
    private int step;
    private Panel content = null!;
    private Label progress = null!;
    private Button previous = null!;
    private Button next = null!;

    public FrmOnboarding(OnboardingPreferences preferences)
    {
        prefs = preferences;
        Text = "Getting started";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(700, 550);
        MinimumSize = new Size(700, 550);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = bg;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        BuildShell();
        ShowStep();
    }

    private void BuildShell()
    {
        var heading = new Label { Text = "Getting started", Dock = DockStyle.Top,
            Height = 39, Padding = new Padding(12, 9, 0, 0), ForeColor = Color.White };
        Controls.Add(heading);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 53,
            BackColor = Color.FromArgb(46, 53, 61) };
        footer.Paint += (s, e) => e.Graphics.DrawLine(new Pen(border), 0, 0, footer.Width, 0);
        progress = new Label { AutoSize = true, Location = new Point(14, 18), ForeColor = Color.White };
        previous = MakeButton("Back", Color.FromArgb(88, 96, 107), 165);
        next = MakeButton("Next", prefs.AccentColor, 165);
        previous.Location = new Point(345, 9);
        next.Location = new Point(519, 9);
        previous.Click += (_, _) => { if (step > 0) { step--; ShowStep(); } };
        next.Click += (_, _) =>
        {
            if (step == 0) { step = 1; ShowStep(); }
            else
            {
                prefs.Completed = true;
                try { prefs.Save(); }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not save your setup preferences: " + ex.Message,
                        "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    prefs.Completed = false;
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        footer.Controls.Add(progress);
        footer.Controls.Add(previous);
        footer.Controls.Add(next);
        Controls.Add(footer);

        content = new Panel { Dock = DockStyle.Fill, BackColor = bg };
        Controls.Add(content);
        content.BringToFront();
        heading.BringToFront();
    }

    private Button MakeButton(string title, Color fill, int width) => new()
    {
        Text = title, Width = width, Height = 36, BackColor = fill,
        ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10)
    };

    private void ShowStep()
    {
        content.Controls.Clear();
        progress.Text = $"{step + 1} of 2";
        previous.Visible = step != 0;
        next.Text = step == 0 ? "Next" : "Finish";
        next.BackColor = prefs.AccentColor;
        if (step == 0) BuildThemePage();
        else BuildClipsPage();
    }

    private void BuildThemePage()
    {
        AddLabel(content, "Select a theme", new Rectangle(0, 48, 700, 34), 13, true, ContentAlignment.MiddleCenter);

        var light = new RadioButton { Text = "Light", Location = new Point(227, 205),
            Width = 112, Checked = !prefs.DarkTheme, ForeColor = Color.White };
        var dark = new RadioButton { Text = "Dark", Location = new Point(369, 205),
            Width = 112, Checked = prefs.DarkTheme, ForeColor = Color.White };
        light.CheckedChanged += (_, _) => { if (light.Checked) prefs.DarkTheme = false; };
        dark.CheckedChanged += (_, _) => { if (dark.Checked) prefs.DarkTheme = true; };
        content.Controls.Add(light);
        content.Controls.Add(dark);
        var lp = new ThemeThumbnail(false) { Location = new Point(228, 105), Size = new Size(110, 80) };
        var dp = new ThemeThumbnail(true) { Location = new Point(368, 105), Size = new Size(110, 80) };
        lp.Click += (_, _) => light.Checked = true;
        dp.Click += (_, _) => dark.Checked = true;
        content.Controls.Add(lp);
        content.Controls.Add(dp);

        var follow = new CheckBox { Text = "Follow system theme", Checked = prefs.FollowSystemTheme,
            AutoSize = true, Location = new Point(265, 260), ForeColor = Color.White };
        var contrast = new CheckBox { Text = "Enable high-contrast", Checked = prefs.HighContrast,
            AutoSize = true, Location = new Point(265, 307), ForeColor = Color.White };
        follow.CheckedChanged += (_, _) =>
        {
            prefs.FollowSystemTheme = follow.Checked;
            light.Enabled = dark.Enabled = !follow.Checked;
        };
        light.Enabled = dark.Enabled = !follow.Checked;
        contrast.CheckedChanged += (_, _) => prefs.HighContrast = contrast.Checked;
        content.Controls.Add(follow);
        content.Controls.Add(contrast);
        AddLabel(content, "Accent color", new Rectangle(0, 357, 700, 30), 10, false, ContentAlignment.MiddleCenter);

        for (int i = 0; i < accents.Length; i++)
        {
            Color swatch = accents[i];
            var circle = new ColorSwatch(swatch, prefs.AccentColor.ToArgb() == swatch.ToArgb())
            {
                Location = new Point(226 + i * 37, 392), Size = new Size(30, 30), Cursor = Cursors.Hand
            };
            circle.Click += (_, _) =>
            {
                prefs.AccentHex = ColorTranslator.ToHtml(swatch);
                foreach (Control ctl in content.Controls)
                    if (ctl is ColorSwatch sw) { sw.Selected = sw.Shade.ToArgb() == swatch.ToArgb(); sw.Invalidate(); }
                next.BackColor = swatch;
            };
            content.Controls.Add(circle);
        }
    }

    private void BuildClipsPage()
    {
        var left = new Panel { Dock = DockStyle.Left, Width = 349, BackColor = bg };
        var preview = new WavePreview { Dock = DockStyle.Fill, Colorful = prefs.ColorfulClips };
        content.Controls.Add(preview);
        content.Controls.Add(left);
        AddLabel(left, "Clip visualization", new Rectangle(30, 28, 295, 36), 13, true);

        var colorful = CreateChoice(left, "Colorful", "Each track gets a new color", 76, prefs.ColorfulClips);
        var classic = CreateChoice(left, "Classic", "The clips you know and love", 161, !prefs.ColorfulClips);
        Action<bool> select = (useColors) =>
        {
            prefs.ColorfulClips = useColors;
            UpdateChoice(colorful, useColors);
            UpdateChoice(classic, !useColors);
            preview.Colorful = useColors;
            preview.Invalidate();
        };
        colorful.Click += (_, _) => select(true);
        classic.Click += (_, _) => select(false);
        foreach (Control child in colorful.Controls) child.Click += (_, _) => select(true);
        foreach (Control child in classic.Controls) child.Click += (_, _) => select(false);
    }

    private Panel CreateChoice(Panel parent, string name, string detail, int top, bool selected)
    {
        var box = new Panel { Location = new Point(30, top), Size = new Size(290, 75),
            BackColor = bg, Cursor = Cursors.Hand };
        box.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, box.ClientRectangle,
            (box.Tag is bool b && b) ? prefs.AccentColor : border, ButtonBorderStyle.Solid);
        var radio = new Label { Name = "radio", Text = selected ? "◉" : "◯", Location = new Point(14, 22),
            AutoSize = true, Font = new Font("Segoe UI", 19), ForeColor = selected ? prefs.AccentColor : muted };
        var title = new Label { Text = name, Location = new Point(55, 13), AutoSize = true,
            ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        var sub = new Label { Text = detail, Location = new Point(55, 41), AutoSize = true, ForeColor = Color.White };
        box.Controls.Add(radio); box.Controls.Add(title); box.Controls.Add(sub);
        parent.Controls.Add(box);
        UpdateChoice(box, selected);
        return box;
    }

    private void UpdateChoice(Panel box, bool selected)
    {
        box.Tag = selected;
        box.BackColor = selected ? Color.FromArgb(53, 56, 70) : bg;
        var radio = box.Controls["radio"] as Label;
        if (radio != null) { radio.Text = selected ? "◉" : "◯"; radio.ForeColor = selected ? prefs.AccentColor : muted; }
        box.Invalidate();
    }

    private static Label AddLabel(Control target, string text, Rectangle position, float fontSize,
        bool bold = false, ContentAlignment alignment = ContentAlignment.MiddleLeft)
    {
        var label = new Label { Text = text, Bounds = position, ForeColor = Color.White,
            TextAlign = alignment, Font = new Font("Segoe UI", fontSize,
            bold ? FontStyle.Bold : FontStyle.Regular) };
        target.Controls.Add(label);
        return label;
    }

    private sealed class ThemeThumbnail : Control
    {
        private readonly bool dark;
        public ThemeThumbnail(bool useDark) { dark = useDark; Cursor = Cursors.Hand; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using var borderPen = new Pen(Color.FromArgb(86, 91, 100));
            using var frame = new SolidBrush(dark ? Color.FromArgb(41, 46, 52) : Color.White);
            g.FillRectangle(frame, ClientRectangle);
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            using var screen = new SolidBrush(dark ? Color.FromArgb(57, 65, 72) : Color.FromArgb(238, 238, 246));
            g.FillRectangle(screen, 12, 12, Width - 24, Height - 32);
            using var ink = new Pen(dark ? Color.White : Color.Black, 3);
            g.DrawLine(ink, 26, 26, Width - 25, 26);
            g.DrawLine(ink, 26, 36, Width - 25, 36);
            g.DrawLine(ink, 26, 46, Width - 47, 46);
            using var button = new SolidBrush(dark ? Color.MediumSlateBlue : Color.FromArgb(178, 182, 250));
            g.FillRectangle(button, Width - 46, Height - 18, 35, 12);
        }
    }

    private sealed class ColorSwatch : Control
    {
        public Color Shade { get; }
        public bool Selected { get; set; }
        public ColorSwatch(Color shade, bool selected) { Shade = shade; Selected = selected; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (Selected) using (var p = new Pen(Color.White, 1.5f))
                e.Graphics.DrawEllipse(p, 0, 0, Width - 2, Height - 2);
            using var b = new SolidBrush(Shade);
            e.Graphics.FillEllipse(b, 3, 3, Width - 7, Height - 7);
        }
    }

    private sealed class WavePreview : Control
    {
        public bool Colorful { get; set; }
        public WavePreview() { BackColor = Color.FromArgb(39, 41, 55); DoubleBuffered = true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var gridPen = new Pen(Color.FromArgb(50, 55, 69));
            for (int x = 0; x < Width; x += 30) g.DrawLine(gridPen, x, 0, x, Height);
            for (int y = 0; y < Height; y += 100) g.DrawLine(gridPen, 0, y, Width, y);
            Color[] colors = { Color.FromArgb(155, 198, 252), Color.FromArgb(177, 170, 251),
                Color.FromArgb(238, 172, 217), Color.FromArgb(251, 157, 161), Color.FromArgb(255, 182, 136) };
            int rowHeight = 98;
            for (int row = 0; row < 5; row++)
            {
                int offset = row % 2 == 0 ? 0 : 165;
                int clipWidth = row == 2 ? 100 : 185;
                for (int part = 0; part < (row == 2 ? 3 : 1); part++)
                {
                    int x = offset + part * (clipWidth + 2);
                    if (x > Width) break;
                    int y = row * rowHeight + 2;
                    Color baseColor = Colorful ? colors[row] : Color.FromArgb(147, 154, 174);
                    using var fill = new SolidBrush(baseColor);
                    var rect = new Rectangle(x, y, Math.Min(clipWidth, Width - x), 95);
                    g.FillRectangle(fill, rect);
                    using var line = new Pen(Color.FromArgb(83, 78, 91), 1);
                    int center = rect.Top + rect.Height / 2;
                    g.DrawLine(line, rect.Left, center, rect.Right, center);
                    var points = new System.Collections.Generic.List<PointF>();
                    int count = Math.Max(1, rect.Width / 2);
                    for (int i = 0; i < count; i++)
                    {
                        double t = (double)i / count;
                        double envelope = Math.Exp(-Math.Pow((t - .27) * 7, 2)) * 31
                            + Math.Exp(-Math.Pow((t - .69) * 18, 2)) * 13;
                        float amp = (float)(envelope * (0.25 + .75 * Math.Abs(Math.Sin(i * .75))));
                        points.Add(new PointF(rect.Left + i * 2, center - amp));
                    }
                    for (int i = count - 1; i >= 0; i--)
                        points.Add(new PointF(points[i].X, 2 * center - points[i].Y));
                    if (points.Count > 2)
                    {
                        using var ink = new SolidBrush(Color.FromArgb(84, 72, 89));
                        g.FillPolygon(ink, points.ToArray());
                    }
                }
            }
        }
    }
}
