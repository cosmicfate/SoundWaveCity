using System;
using System.Drawing;
using System.Windows.Forms;

namespace MediocreSoundDesigner;

public partial class FrmMain
{
    // add quick shortcuts without changing the current editing methods.
    private void AddQuickAccessToolbar()
    {
        ToolStrip toolbar = new()
        {
            Name = "quickActions",
            Dock = DockStyle.None,
            AutoSize = false,
            GripStyle = ToolStripGripStyle.Hidden,
            Location = new Point(12, menu.Bottom + 2),
            Size = new Size(Math.Max(0, ClientSize.Width - 24), 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Settings.Color.WinBg,
            ForeColor = Settings.Color.WinFg,
            ShowItemToolTips = true,
            RenderMode = ToolStripRenderMode.System,
            TabStop = false
        };

        // reuse the existing menu actions instead of writing new audio logic.
        toolbar.Items.Add(MakeToolbarButton("Open", "import a wav file", menuFileOpen_Click));
        toolbar.Items.Add(MakeToolbarButton("Save", "export the current mix", menuFileSave_Click));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(MakeToolbarButton("Play / Pause", "toggle playback", (_, _) =>
        {
            if (trackManager.Tracks.Count > 0)
                btnPlay_Click(this, EventArgs.Empty);
        }));
        toolbar.Items.Add(MakeToolbarButton("Record", "record a new track", menuGenRecord_Click));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(MakeToolbarButton("Noise", "generate white noise", menuGenNoise_Click));
        toolbar.Items.Add(MakeToolbarButton("Split", "split at the edit cursor", (_, _) => SplitSelectedTrack()));

        // match the toolbar colors to the existing appearance preferences.
        BackColorChanged += (_, _) => UpdateToolbarColors(toolbar);
        ForeColorChanged += (_, _) => UpdateToolbarColors(toolbar);

        Controls.Add(toolbar);
        toolbar.BringToFront();
    }

    // create consistent shortcut buttons with a small amount of shared code.
    private static ToolStripButton MakeToolbarButton(string title, string tip, EventHandler click)
    {
        ToolStripButton button = new(title)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ToolTipText = tip,
            Margin = new Padding(3, 0, 3, 0)
        };
        button.Click += click;
        return button;
    }

    // refresh colors after the user switches between light and dark mode.
    private static void UpdateToolbarColors(ToolStrip toolbar)
    {
        toolbar.BackColor = Settings.Color.WinBg;
        toolbar.ForeColor = Settings.Color.WinFg;
        foreach (ToolStripItem item in toolbar.Items)
            item.ForeColor = Settings.Color.WinFg;
        toolbar.Invalidate();
    }
}
