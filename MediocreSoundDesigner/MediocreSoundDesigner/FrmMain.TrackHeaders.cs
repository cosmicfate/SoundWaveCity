using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MediocreSoundDesigner;

// keeps track controls separate from the audio editing code.
public partial class FrmMain
{
    private const int TrackHeaderWidth = 180;
    private readonly Dictionary<Track, TrackHeader> trackHeaders = new();
    private Panel trackHeaderBand;
    private int nextTrackNumber = 1;

    private sealed class TrackHeader
    {
        public Panel Panel { get; init; } = null!;
        public Label Name { get; init; } = null!;
        public Label GainText { get; init; } = null!;
        public TrackBar Gain { get; init; } = null!;
        public Button Mute { get; init; } = null!;
    }

    // create one small area for adding empty tracks, beside the ruler.
    private void InitializeTrackInfoUI()
    {
        trackHeaderBand = new Panel { Width = TrackHeaderWidth, Height = 80 };
        var title = new Label
        {
            Text = "TRACKS",
            Left = 12,
            Top = 8,
            Width = 100,
            Height = 20,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        var add = new Button
        {
            Text = "+  New track",
            Left = 12,
            Top = 37,
            Width = TrackHeaderWidth - 24,
            Height = 29,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        add.FlatAppearance.BorderSize = 1;
        add.Click += (_, _) => CreateEmptyTrack();

        trackHeaderBand.Controls.Add(title);
        trackHeaderBand.Controls.Add(add);
        Controls.Add(trackHeaderBand);
        BackColorChanged += (_, _) => RefreshTrackHeaderColors();
        ForeColorChanged += (_, _) => RefreshTrackHeaderColors();
        PositionTrackHeaderBand(menu.Bottom + 6);
        RefreshTrackHeaderColors();
    }

    // create the same kind of track row used for recorded or generated audio.
    private void CreateEmptyTrack()
    {
        Track track = new();
        CreateTrackPanel(track);
        trackManager.AddTrack(track);
        UpdateSizes();
        ShowTrackRow(track);
        track.PanDrawSamples.Invalidate();
    }

    // scroll the workspace until the new row is visible.
    private void ShowTrackRow(Track track)
    {
        if (!trackManager.Tracks.Contains(track) || track.PanDrawSamples is null)
            return;

        // use the built-in scroll behavior instead of adding manual offsets.
        panTracks.ScrollControlIntoView(track.PanDrawSamples);
        trackManager.SelectTrack(track);
        RefreshTrackHeaderColors();
    }

    // add a control panel next to the existing waveform panel.
    private void RegisterTrackHeader(Track track, Panel waveform)
    {
        var header = new Panel { Left = 0, Width = TrackHeaderWidth };
        header.Paint += (_, e) =>
        {
            // a thin divider separates the controls from the waveform area.
            using var edge = new Pen(Settings.Color.WinBg);
            e.Graphics.DrawLine(edge, header.Width - 1, 0,
                header.Width - 1, header.Height);
        };
        var name = new Label
        {
            Text = $"Audio {nextTrackNumber++}",
            Left = 12,
            Top = 9,
            Width = TrackHeaderWidth - 24,
            Height = 23,
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        var gainText = new Label
        {
            Text = "volume",
            Left = 12,
            Top = 35,
            Width = TrackHeaderWidth - 24,
            Height = 17,
            Font = new Font("Segoe UI", 8)
        };
        var gain = new TrackBar
        {
            Left = 6,
            Top = 52,
            Width = TrackHeaderWidth - 12,
            Height = 30,
            Minimum = 0,
            Maximum = 200,
            TickStyle = TickStyle.None,
            SmallChange = 5,
            LargeChange = 10,
            Value = Math.Clamp((int)Math.Round(track.Volume * 100), 0, 200)
        };
        var mute = new Button
        {
            Text = "Mute",
            Left = 12,
            Top = 100,
            Width = 72,
            Height = 24,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        mute.FlatAppearance.BorderSize = 1;

        // choosing a header selects its track, just like clicking the waveform.
        header.Click += (_, _) => trackManager.SelectTrack(track);
        name.Click += (_, _) => trackManager.SelectTrack(track);
        name.DoubleClick += (_, _) => RenameTrack(name);

        // use the track's existing volume property for both playback and export.
        gain.ValueChanged += (_, _) =>
        {
            track.SetVolume(gain.Value / 100f);
            gainText.Text = $"volume   {gain.Value}%";
            waveform.Invalidate();
        };
        gainText.Text = $"volume   {gain.Value}%";

        // the current audio engine already supports disabling a track.
        mute.Click += (_, _) =>
        {
            trackManager.SelectTrack(track);
            track.ToggleDisabled();
            waveform.Invalidate();
            RefreshTrackHeaderColors();
        };

        header.Controls.Add(name);
        header.Controls.Add(gainText);
        header.Controls.Add(gain);
        header.Controls.Add(mute);
        panTracks.Controls.Add(header);
        trackHeaders.Add(track, new TrackHeader
        {
            Panel = header,
            Name = name,
            GainText = gainText,
            Gain = gain,
            Mute = mute
        });

        // the track manager uses the waveform background to signal selection.
        waveform.BackColorChanged += (_, _) => RefreshTrackHeaderColors();
        RefreshTrackHeaderColors();
        header.BringToFront();
    }

    // use the imported filename as its initial track name.
    private void SetTrackHeaderName(Track track, string name)
    {
        if (trackHeaders.TryGetValue(track, out TrackHeader? header))
            header.Name.Text = string.IsNullOrWhiteSpace(name) ? header.Name.Text : name;
    }

    // double-click a track name to rename it without editing audio data.
    private void RenameTrack(Label label)
    {
        var editor = new TextBox
        {
            Bounds = label.Bounds,
            Text = label.Text,
            MaxLength = 80,
            Font = label.Font
        };
        Panel parent = (Panel)label.Parent!;
        bool finished = false;
        void Finish(bool save)
        {
            if (finished) return;
            finished = true;
            if (save && !string.IsNullOrWhiteSpace(editor.Text))
                label.Text = editor.Text.Trim();
            parent.Controls.Remove(editor);
            editor.Dispose();
        }
        editor.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Finish(true); }
            if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; Finish(false); }
        };
        editor.LostFocus += (_, _) => Finish(true);
        parent.Controls.Add(editor);
        editor.BringToFront();
        editor.Focus();
        editor.SelectAll();
    }

    // keep track headers and waveforms at exactly the same vertical position.
    private void LayoutTrackHeaders()
    {
        foreach (Track track in trackHeaders.Keys.ToArray())
        {
            if (trackManager.Tracks.Contains(track)) continue;
            Panel unused = trackHeaders[track].Panel;
            trackHeaders.Remove(track);
            panTracks.Controls.Remove(unused);
            unused.Dispose();
        }

        foreach (Track track in trackManager.Tracks)
        {
            if (!trackHeaders.TryGetValue(track, out TrackHeader? header)) continue;
            Panel waveform = track.PanDrawSamples;
            header.Panel.Bounds = new Rectangle(0, waveform.Top,
                TrackHeaderWidth, waveform.Height);
            header.Panel.BringToFront();
        }
        lblPlayhead.BringToFront();
        RefreshTrackHeaderColors();
    }

    // line up the add-track area with the top of the compact ruler.
    private void PositionTrackHeaderBand(int top)
    {
        if (trackHeaderBand is null) return;
        trackHeaderBand.Bounds = new Rectangle(15, top, TrackHeaderWidth, 80);
        trackHeaderBand.BringToFront();
    }

    // reuse the theme's selected, muted, text, and accent colors.
    private void RefreshTrackHeaderColors()
    {
        bool dark = Settings.Color.WinBg.GetBrightness() < 0.5f;
        Color normal = Settings.Color.TrackBg_Unselected;
        Color selected = Settings.Color.TrackBg_Selected;
        Color button = dark ? Color.FromArgb(67, 76, 90) : Color.FromArgb(215, 222, 232);

        if (trackHeaderBand is not null)
        {
            trackHeaderBand.BackColor = normal;
            foreach (Control control in trackHeaderBand.Controls)
            {
                control.ForeColor = Settings.Color.WinFg;
                if (control is Button) control.BackColor = button;
            }
        }

        foreach ((Track track, TrackHeader info) in trackHeaders)
        {
            // keep the slider in sync with changes made through the volume dialog.
            int volume = Math.Clamp((int)Math.Round(track.Volume * 100), 0, 200);
            if (info.Gain.Value != volume) info.Gain.Value = volume;
            info.GainText.Text = $"volume   {volume}%";
            info.Panel.BackColor = ReferenceEquals(trackManager.SelectedTrack, track)
                ? selected : normal;
            info.Name.ForeColor = Settings.Color.WinFg;
            info.GainText.ForeColor = Settings.Color.WinFg;
            info.Gain.BackColor = info.Panel.BackColor;
            info.Mute.BackColor = track.IsDisabled ? Settings.Color.Playline : button;
            info.Mute.ForeColor = track.IsDisabled ? Settings.Color.WinBg : Settings.Color.WinFg;
            info.Mute.Text = track.IsDisabled ? "Unmute" : "Mute";
        }
    }
}
