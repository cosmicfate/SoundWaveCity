using NAudio.Wave;

namespace MediocreSoundDesigner;

// adds a compact timeline without changing the existing audio editor methods.
public partial class FrmMain
{
    private TimelineRuler ruler = null!;
    private Button zoomOut = null!;
    private Button zoomReset = null!;
    private Button zoomIn = null!;
    private int defaultZoom;
    private int originalBottomGap;
    private int previousTrackCount;
    private double importedDuration;
    private bool positioningTimeline;

    // call this once after setup() and the optional quick-access toolbar.
    private void InitializeTimelineZoom()
    {
        defaultZoom = trkDrawTimescale.Value;
        originalBottomGap = ClientSize.Height - panTracks.Bottom;
        previousTrackCount = trackManager.Tracks.Count;

        // keep the existing controls available to the audio logic, but hide
        // their visual tracks so there is only one timeline on the screen.
        trkDrawTimescale.Visible = false;
        trkScrubber.Visible = false;

        ruler = new TimelineRuler();
        Controls.Add(ruler);
        ruler.BringToFront();

        // place the three zoom controls inside the header of the ruler.
        zoomOut = CreateZoomButton("−", -50, 30);
        zoomReset = CreateZoomButton("100%", 0, 54);
        zoomIn = CreateZoomButton("+", 50, 30);

        // dragging the ruler reuses the same pixel-based seek position as before.
        ruler.MouseDown += (sender, e) =>
        {
            if (e.Button != MouseButtons.Left || e.Y < 34) return;
            isScrubbing = true;
            ruler.Capture = true;
            SeekFromRuler(e.X);
        };
        ruler.MouseMove += (sender, e) =>
        {
            if (isScrubbing && (e.Button & MouseButtons.Left) != 0)
                SeekFromRuler(e.X);
        };
        ruler.MouseUp += (sender, e) =>
        {
            if (!isScrubbing || e.Button != MouseButtons.Left) return;
            SeekFromRuler(e.X);
            isScrubbing = false;
            ruler.Capture = false;

            // restart from the chosen location only after a drag finishes.
            if (trackManager.State == PlaybackState.Playing)
                trackManager.PlayAllFrom(scrubbedXPosition);
        };

        // the wav header supplies an estimated time range for imported clips.
        // the full track manager is needed for exact mixed-project duration.
        ofdLoadFile.FileOk += (sender, e) =>
        {
            try
            {
                using var wav = new WaveFileReader(ofdLoadFile.FileName);
                importedDuration = Math.Max(importedDuration, wav.TotalTime.TotalSeconds);
                RefreshRuler();
            }
            catch
            {
                // keep the ruler usable even if the chosen file cannot be read.
            }
        };

        // a hidden slider still drives the existing waveform zoom calculation.
        trkDrawTimescale.Scroll += (sender, e) => RefreshRuler();
        ClientSizeChanged += (sender, e) => PositionCompactTimeline();

        // synchronize the ruler with the original playhead without redrawing
        // when the audio has not moved to another screen pixel.
        tmrUpdate.Tick += (sender, e) =>
        {
            int trackCount = trackManager.Tracks.Count;
            if (trackCount == 0 && previousTrackCount > 0)
            {
                importedDuration = 0;
                RefreshRuler();
            }
            previousTrackCount = trackCount;

            if (ruler.PlayheadX != trkScrubber.Value)
            {
                ruler.PlayheadX = trkScrubber.Value;
                ruler.Invalidate();
            }
            if (ruler.MarkerColor != Settings.Color.Playline)
            {
                ruler.MarkerColor = Settings.Color.Playline;
                ruler.Invalidate();
            }
        };

        BackColorChanged += (sender, e) => ApplyTimelineColors();
        ForeColorChanged += (sender, e) => ApplyTimelineColors();
        PositionCompactTimeline();
        ApplyTimelineColors();
        RefreshRuler();
    }

    // make one small flat button that calls the zoom already in frm main.
    private Button CreateZoomButton(string label, int delta, int width)
    {
        Button button = new()
        {
            Text = label,
            Width = width,
            Height = 27,
            Font = new Font("Segoe UI", 9f),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TabStop = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (sender, e) =>
        {
            // zero represents the reset button, not a change in zoom.
            int desired = delta == 0 ? defaultZoom : trkDrawTimescale.Value + delta;

            // avoid letting the scale reach zero at the far end of the slider.
            int minimumDistance = Math.Max(1,
                (int)Math.Ceiling((trkDrawTimescale.Maximum - defaultZoom) / 8.0));
            int upperLimit = trkDrawTimescale.Maximum - minimumDistance;
            trkDrawTimescale.Value = Math.Clamp(desired,
                trkDrawTimescale.Minimum, upperLimit);

            // the original handler redraws all waveforms using the new scale.
            trkDrawTimescale_Scroll(trkDrawTimescale, EventArgs.Empty);
            RefreshRuler();
        };
        ruler.Controls.Add(button);
        return button;
    }

    // move the ruler into the space directly below the optional quick toolbar.
    private void PositionCompactTimeline()
    {
        if (ruler is null || positioningTimeline) return;
        positioningTimeline = true;
        try
        {
            Control[] toolbar = Controls.Find("quickActions", false);

            // position the ruler below the toolbar 
            int top = toolbar.Length > 0 ? toolbar[0].Bottom + 8 : menu.Bottom + 36; // location of zoom buttons
            int width = Math.Max(150, ClientSize.Width - 30);
            ruler.Bounds = new Rectangle(15, top, width, 80);

            // a single compact ruler replaces the two large visible sliders.
            int trackTop = ruler.Bottom + 8;
            int trackHeight = Math.Max(120,
                ClientSize.Height - originalBottomGap - trackTop);
            bool trackLayoutChanged = panTracks.Top != trackTop ||
                panTracks.Height != trackHeight || panTracks.Width != width;

            panTracks.Top = trackTop;
            panTracks.Height = trackHeight;
            panTracks.Left = 15;
            panTracks.Width = width;

            // keep the invisible scrubber aligned with the same pixel range.
            trkScrubber.Minimum = 0;
            trkScrubber.Maximum = width;
            if (trkScrubber.Value > width) trkScrubber.Value = width;

            zoomIn.Location = new Point(ruler.Width - 41, 7);
            zoomReset.Location = new Point(zoomIn.Left - 58, 7);
            zoomOut.Location = new Point(zoomReset.Left - 34, 7);

            // only resize the track rows when their available area changes.
            if (trackLayoutChanged)
                UpdateSizes();

            ruler.BringToFront();
        }
        finally
        {
            positioningTimeline = false;
        }
    }

    // update the same editing cursor that frm main already uses for splitting.
    private void SeekFromRuler(int x)
    {
        int position = Math.Clamp(x, trkScrubber.Minimum, trkScrubber.Maximum);
        scrubbedXPosition = position;
        trkScrubber.Value = position;
        lblPlayhead.Left = position;
        ruler.PlayheadX = position;
        ruler.Invalidate();
    }

    // convert the current zoom into the number of visible timeline seconds.
    private void RefreshRuler()
    {
        if (ruler is null) return;
        double baseScale = Math.Max(1, trkDrawTimescale.Maximum - defaultZoom);
        double currentScale = Math.Max(1,
            trkDrawTimescale.Maximum - trkDrawTimescale.Value);
        ruler.VisibleSeconds = importedDuration * currentScale / baseScale;

        // the center button displays the active percentage and resets it.
        int percent = (int)Math.Round(baseScale / currentScale * 100);
        zoomReset.Text = $"{percent}%";
        ruler.Invalidate();
    }

    // apply light or dark colors to the ruler and its three zoom buttons.
    private void ApplyTimelineColors()
    {
        if (ruler is null) return;
        bool dark = Settings.Color.WinBg.GetBrightness() < 0.5f;
        ruler.BackColor = dark ? Color.FromArgb(34, 42, 52) : Color.FromArgb(236, 239, 244);
        ruler.ForeColor = Settings.Color.WinFg;
        ruler.MarkerColor = Settings.Color.Playline;

        Color buttonBg = dark ? Color.FromArgb(61, 73, 91) : Color.FromArgb(210, 217, 228);
        foreach (Button button in new[] { zoomOut, zoomReset, zoomIn })
        {
            button.BackColor = buttonBg;
            button.ForeColor = Settings.Color.WinFg;
        }
        ruler.Invalidate();
    }

    // keep the split menu connected when it exists in the form designer.
    private void menuEditSplit_Click(object sender, EventArgs e) => SplitSelectedTrack();
}
