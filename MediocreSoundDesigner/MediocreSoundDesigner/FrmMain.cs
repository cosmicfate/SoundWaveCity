using MediocreSoundDesigner.Properties;

namespace MediocreSoundDesigner;

public partial class FrmMain : Form
{
    // fields
    // manage imported audio tracks, selection, drawing, and playback.
    private TrackManager trackManager;

    // ctor
    // initialize the editor, then attach the appearance menu and resize handler.
    public FrmMain()
    {
        InitializeComponent();
        Setup();
        InitalizeTrackInfo();
        InitializeTimelineZoom();
        InitializeClipContextMenu();
        AddAppearanceMenu();
        SizeChanged += FrmMain_SizeChanged;
    }

    // setup
    // configure the track display, playback button, and timeline controls.
    private void Setup()
    {
        DoubleBuffered = true;
        trackManager = new(panTracks);
        btnPlay.BackgroundImage = Resources.play;
        trackManager.DrawTimescale = trkDrawTimescale.Value;
        UpdateSizes();
        ApplySettings();
        SetupScrubber();
        RefreshTrackHeaderColor();
        trackManager.TracksChanged += () => {
            UpdateSizes();
        };
    }
    // refresh visible editor colors from the shared application palette.
    private void ApplySettings()
    {
        BackColor = Settings.Color.WinBg;
        ForeColor = Settings.Color.WinFg;
        panTracks.BackColor = Settings.Color.TrackBg_Unselected;
        lblPlayhead.BackColor = Settings.Color.Playline;
        trackManager.ApplySettings();
        RefreshTrackHeaderColor();
    }

    // let users revisit the same choices without resetting their setup
    // allow users to reopen the wizard without resetting the first-run flag.
    private void AddAppearanceMenu()
    {
        ToolStripMenuItem appearance = new("Appearance...");
        appearance.Click += (sender, e) =>
        {
            // load a fresh copy so the wizard begins with the last saved choices.
            var preferences = OnboardingPreferences.Load();
            using var wizard = new FrmOnboarding(preferences, editingAppearance: true);
            // only apply and redraw when the user saves changes with finish.
            if (wizard.ShowDialog(this) == DialogResult.OK)
            {
                preferences.ApplyToAppColors();
                ApplySettings();
                // force existing tracks to repaint with the updated shared colors.
                foreach (Track track in trackManager.Tracks)
                    track.PanDrawSamples.Invalidate();
                Invalidate(true);
            }
        };
        menuFile.DropDownItems.Add(appearance);
    }

    // gui controls
    // provide keyboard shortcuts for nudging or deleting the selected track.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // undo keybind code that when Ctrl+Z is pressed, it calls the UndoLastAction method of the trackManager and updates the sizes of the tracks.
        if (keyData == (Keys.Control | Keys.Z))
        {
            trackManager.UndoLastAction();
            UpdateSizes();
            return true;
        }

        Track selectedTrack = trackManager.SelectedTrack;
        if (selectedTrack is not null)
        {
            if (keyData == Keys.Left)
            {
                selectedTrack.ShiftSampleOver(-5);
                selectedTrack.PanDrawSamples.Invalidate();
                return true;
            }
            else if (keyData == Keys.Right)
            {
                selectedTrack.ShiftSampleOver(+5);
                selectedTrack.PanDrawSamples.Invalidate();
                return true;
            }
            else if (keyData == Keys.Delete)
            {
                // a selected clip is deleted on its own. its track is only removed if that leaves it empty and other tracks exist.
                // with no clip selected, Delete removes the track.
                if (selectedTrack.SelectedClip is not null)
                {
                    DeleteSelectedClip(selectedTrack);
                }
                else
                {
                    DeleteTrack(selectedTrack);
                }
                return true;
            }
            else if (keyData == (Keys.Control | Keys.I))
            {
                SplitSelectedTrack();
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // deletes the track's selected clip. the track is removed too if that leaves it empty and other tracks exist.
    // shared by the Delete key and the right-click clip menu.
    private void DeleteSelectedClip(Track track)
    {
        if (track?.SelectedClip is null) return;
        track.RemoveSelectedClip();
        track.PanDrawSamples.Invalidate();
        // the track goes too if that was its last clip and other tracks exist.
        // (a track emptied by dragging its clip away is left alone, so this only checks this track)
        if (trackManager.RemoveTrackIfEmpty(track))
        {
            UpdateSizes();
        }
    }

    // asks for confirmation, then deletes the whole track along with every clip on it.
    // shared by the Delete key and the right-click track menu.
    private void DeleteTrack(Track track)
    {
        if (track is null) return;
        if (MessageBox.Show("This will delete the selected TRACK. This is NOT undoable!", "Are You Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
        {
            panTracks.Controls.Remove(track.PanDrawSamples);
            track.PanDrawSamples.Dispose();
            track.Dispose();
            trackManager.RemTrack(track);
            UpdateSizes();
        }
    }

    // resize the timeline and track panels when the editor window changes size.
    private void FrmMain_SizeChanged(object sender, EventArgs e)
    {
        UpdateSizes();
    }

    // start playback at the chosen scrubber position or pause current playback.
    private void btnPlay_Click(object sender, EventArgs e)
    {
        if (trackManager.State is NAudio.Wave.PlaybackState.Stopped or NAudio.Wave.PlaybackState.Paused)
        {
            trackManager.PlayAllFrom(trkScrubber?.Value ?? 0);
            btnPlay.BackgroundImage = Resources.pause;
        }
        else
        {
            trackManager.PauseAll();
            btnPlay.BackgroundImage = Resources.play;
        }
    }

    // import a wave file and add its waveform panel to the editor.
    private void menuFileOpen_Click(object sender, EventArgs e)
    {
        ofdLoadFile.DefaultExt = "wav";
        if (ofdLoadFile.ShowDialog() == DialogResult.OK)
        {
            // the file becomes a clip. it goes on the first track that has no clips,
            // and a new track is only made when every existing track already holds audio.
            var clip = Track.LoadClip(ofdLoadFile.FileName);
            Track target = trackManager.Tracks.Find(t => t.Clips.Count == 0);
            if (target is null)
            {
                target = new();
                CreateTrackPanel(target);
                trackManager.AddTrack(target);
                UpdateSizes();
            }
            else
            {
                trackManager.RecordTrackSnapshot(target); // so undo takes the clip back off the track
                trackManager.SelectTrack(target);
            }
            target.AttachClip(clip, 0); // the clip always starts at the beginning of its track
            SetTrackHeaderName(target, Path.GetFileNameWithoutExtension(ofdLoadFile.FileName));
            target.PanDrawSamples.Invalidate();
        }
    }
    // export the current audio mix through the track manager.
    private void menuFileSave_Click(object sender, EventArgs e)
    {
        sfdSaveFile.DefaultExt = "wav";
        if (sfdSaveFile.ShowDialog() == DialogResult.OK)
        {
            trackManager.SaveToFile(sfdSaveFile.FileName);
        }
    }
    // open the existing color settings dialog and refresh its changes.
    private void menuFileSettings_Click(object sender, EventArgs e)
    {
        FrmEditSettings frm = new();
        frm.SetSound(trackManager.SelectedTrack);
        if (frm.ShowDialog() == DialogResult.OK)
        {
            ApplySettings();
        }
    }

    // invert the selected waveform vertically.
    private void menuEditFlip_Click(object sender, EventArgs e)
    {
        trackManager.RecordTrackSnapshot(trackManager.SelectedTrack);
        trackManager.SelectedTrack?.Flip();
    }
    // reverse the order of samples in the selected track.
    private void menuEditReverse_Click(object sender, EventArgs e)
    {
        trackManager.RecordTrackSnapshot(trackManager.SelectedTrack);
        trackManager.SelectedTrack?.Reverse();
    }
    // open the volume adjustment dialog for the selected track.
    private void menuEditVolume_Click(object sender, EventArgs e)
    {
        if (trackManager.SelectedTrack is not null)
        {
            trackManager.RecordTrackSnapshot(trackManager.SelectedTrack);
            FrmEditVolume frm = new();
            frm.SetSound(trackManager.SelectedTrack);
            frm.ShowDialog();
            RefreshTrackHeaderColor();
        }
    }
    // open silence removal controls for the selected track.
    private void menuEditEliminateSilence_Click(object sender, EventArgs e)
    {
        if (trackManager.SelectedTrack is not null)
        {
            trackManager.RecordTrackSnapshot(trackManager.SelectedTrack);
            FrmEditEliminateSilence frm = new();
            frm.SetSound(trackManager.SelectedTrack);
            frm.ShowDialog();
        }
    }

    private void SplitSelectedTrack()
    {
        // Split works on the selected track, like every other Edit menu command.
        Track track = trackManager.SelectedTrack;
        if (track is null) return;

        trackManager.RecordTrackSnapshot(trackManager.SelectedTrack);
        // Split the clip under the edit cursor (a pixel X position). Both halves
        // stay on this track, so there are no new panels or rows and UpdateSizes
        // isn't needed. SplitAt returns false if the cursor isn't inside a clip.
        if (track.SplitAt(scrubbedXPosition))
        {
            // Repaint so the new clip edge and the selection outline appear.
            track.PanDrawSamples.Invalidate();
        }
    }

    // create a new track and let the user record microphone audio into it.
    private void menuGenRecord_Click(object sender, EventArgs e)
    {
        Track newTrack = new(new float[0]);
        CreateTrackPanel(newTrack);
        trackManager.AddTrack(newTrack);
        UpdateSizes();
        newTrack.PanDrawSamples.Invalidate();

        FrmGenRecord frm = new();
        frm.SetSound(trackManager.SelectedTrack);
        if (frm.ShowDialog() == DialogResult.Cancel)
        {
            trackManager.RemTrack(newTrack);
            UpdateSizes();
        }
    }
    // generate a track containing randomized white-noise samples.
    private void menuGenNoise_Click(object sender, EventArgs e)
    {
        const int NUM_SAMPLES = 100_000;
        float[] samples = new float[NUM_SAMPLES];
        Random rand = new();
        for (int i = 0; i < NUM_SAMPLES; i++)
        {
            samples[i] = rand.NextSingle() * 2.0f - 1.0f;
        }
        Track newTrack = new(samples);
        CreateTrackPanel(newTrack);
        trackManager.AddTrack(newTrack);
        UpdateSizes();
        newTrack.PanDrawSamples.Invalidate();
    }
    // generate slowly varying random values for a smoother noise waveform.
    private void menuGenGradientNoise_Click(object sender, EventArgs e)
    {
        const int NUM_SAMPLES = 250_000;
        float[] samples = new float[NUM_SAMPLES];
        Random rand = new();
        float cur = 0;
        float inc = 0.005f;
        int dir = +1;
        for (int i = 0; i < NUM_SAMPLES; i++)
        {
            cur += (inc * dir);
            if (rand.NextSingle() < 0.5f)
            {
                dir *= -1;
            }
            if (cur > 0.9f)
            {
                dir = -1;
            }
            if (cur < -0.9f)
            {
                dir = +1;
            }
            samples[i] = cur;
        }
        Track newTrack = new(samples);
        CreateTrackPanel(newTrack);
        trackManager.AddTrack(newTrack);
        UpdateSizes();
        newTrack.PanDrawSamples.Invalidate();
    }

    // synchronize the play button, playhead, and slider with playback state.
    private void tmrUpdatePlaybackState_Tick(object sender, EventArgs e)
    {
        if (trackManager.Tracks.Count > 0)
        {
            bool isPlaying = trackManager.State == NAudio.Wave.PlaybackState.Playing;
            btnPlay.BackgroundImage = isPlaying ? Resources.pause : Resources.play;
            btnPlay.Visible = true;

            // avoid moving the slider programmatically while the user drags it.
            if (!isScrubbing)
            {
                // show the live playback location only while audio is running.
                if (isPlaying)
                {
                    int pos = trackManager.GetPlayheadPos();
                    lblPlayhead.Left = TrackHeaderWidth + pos;
                    if (trkScrubber != null && pos >= trkScrubber.Minimum && pos <= trkScrubber.Maximum)
                    {
                        trkScrubber.Value = pos;
                    }
                }
                else
                {
                    lblPlayhead.Left = TrackHeaderWidth + scrubbedXPosition;
                    if (trkScrubber != null && scrubbedXPosition >= trkScrubber.Minimum && scrubbedXPosition <= trkScrubber.Maximum)
                    {
                        trkScrubber.Value = scrubbedXPosition;
                    }
                }
            }

            lblPlayhead.Visible = true;
        }
        else
        {
            btnPlay.BackgroundImage = null;
            btnPlay.Visible = false;
            lblPlayhead.Visible = false;
        }
    }

    // update waveform zoom when the timescale slider moves.
    private void trkDrawTimescale_Scroll(object sender, EventArgs e)
    {
        trackManager.DrawTimescale = trkDrawTimescale.Maximum - trkDrawTimescale.Value;
        trackManager.RedrawAll();
    }

    // helper methods
    // create a clickable and paintable panel for a newly added audio track.
    private Panel CreateTrackPanel(Track track)
    {
        Panel pan = new()
        {
            BackColor = Settings.Color.TrackBg_Unselected,
            Width = panTracks.Width,
            Left = 0,
            Top = 0,
        };
        // a single click selects the track for keyboard and editing commands.
        pan.Click += (sender, e) =>
        {
            trackManager.RecordTrackSnapshot(trackManager.SelectedTrack);
            trackManager.SelectTrack(track);
        };
        pan.MouseClick += (sender, e) =>
        {
            // MouseClick reports the mouse position
            // clicking a track does two more things:
            //      1. moves the timeline head to the clicked position
            //      2. selects the clip under the mouse, if there is one

            // track panels sit at Left = 0 inside panTracks. the scrubber's range is 0..panTracks.Width
            // Math.Clamp allegedly keeps the value inside the range that TrackBar accepts
            scrubbedXPosition = Math.Clamp(e.X, trkScrubber.Minimum, trkScrubber.Maximum);

            // moves scrubber thumb and playhead line to the clicked point
            // as a plus this also sets where playback starts (W rizz)
            trkScrubber.Value = scrubbedXPosition;
            lblPlayhead.Left = scrubbedXPosition;

            // selects the clicked clip and then repaints the outline
            track.SelectClipAt(e.X);
            track.PanDrawSamples.Invalidate();

            // right-clicking a clip also opens the clip edit menu (ClipContextMenu)
            if (e.Button == MouseButtons.Right)
            {
                ShowTrackContextMenu(track, pan, e.Location);
            }
        };
        // double-click to enable or disable the track without removing it.
        pan.DoubleClick += (sender, e) =>
        {
            track.ToggleDisabled();
            track.PanDrawSamples.Invalidate();
            RefreshTrackHeaderColor();
        };
        // delegate waveform rendering to the track object.
        pan.Paint += (sender, e) =>
        {
            track.DrawSamples(e.Graphics, trackManager.DrawTimescale);
        };
        EnableClipDragDrop(track, pan);
        panTracks.Controls.Add(pan);
        track.LinkPanels(pan, panTracks);
        RegisterTrackHeader(track, pan);
        trackManager.SelectTrack(track);

        return pan;
    }

    // arrange track panels and adjust the scrubber width after layout changes.
    private void UpdateSizes()
    {
        const int PADDING = 15;

        panTracks.Left = PADDING;
        panTracks.Width = Math.Max(1, MathClientSize.Width - (PADDING * 2));

        int count = trackManager.Tracks.Count;
        int rowHeight = Math.Clamp(panTracks.clientSize.Height / Math.Max(1, count) - 1 , 118, 220);
        panTracks.AutoScrollMinSize = new Size(0, count * rowHeight);
        int waveformWidth = Math.Max(1, panTracks.ClientSize.Width - TrackHeaderWidth - 4);

        if (trkScrubber != null)
        {
            trkScrubber.Left = panTracks.Left + TrackHeaderWidth;
            trkScrubber.Width = waveformWidth;
            trkScrubber.Maximum = waveformWidth;
            if(trkScrubber.Value > waveformWidth) {trkScrubber.Value = waveformWidth;}
        }

        int top = 0;
        foreach (Track track in trackManager.Tracks)
        {
            track.PanDrawSamples.Left = TrackHeaderWidth;
            track.PanDrawSamples.Width = waveformWidth;
            track.PanDrawSamples.Height = rowHeight; //(panTracks.Height / trackManager.Tracks.Count) - 1;
            track.PanDrawSamples.Top = top;
            track.PanDrawSamples.Invalidate();
            //top += track.PanDrawSamples.Height;
        }
        //trkDrawTimescale.Width = panTracks.Width;
        //trkDrawTimescale.Left = PADDING;
        //lblPlayhead.Left = 0;
        //lblPlayhead.Height = panTracks.Height;
        //lblPlayhead.BringToFront();
        top += rowHeight;
    }

    //keeping controls aligned with the waveform row that they are in
    LayoutTrackHeader();
    trkDrawTimescale.Width = waveFormWidth;
    trkDrawTimescale.Left = panTracks.Left + TrackHeaderWidth;
    lblPlayhead.Left = TrackHeaderWidth + scrubbedXPosition;
    lblPlayhead.Height = Math.Max(panTracks.Height, rowHeight * count);
    lblPlayhead.BringToFront();

    // the scrubber lets users select a horizontal playback starting position.
    private TrackBar trkScrubber;
    // prevent the playback timer from overriding active dragging.
    private bool isScrubbing = false;
    // remember the last manually chosen position when playback stops.
    private int scrubbedXPosition = 0;

    // create the timeline slider dynamically above the track area.
    private void SetupScrubber()
    {
        trkScrubber = new TrackBar
        {
            Left = panTracks.Left + TrackHeaderWidth,
            Top = Math.Max(0, panTracks.Top - 28),
            Width = Math.Max(1, panTracks.Width - TrackHeaderWidth),
            Minimum = 0,
            Maximum = Math.Max(1, panTracks.Width - TrackHeaderWidth),
            TickStyle = TickStyle.None,
            Height = 25
        };

        trkScrubber.MouseDown += trkScrubber_MouseDown;
        trkScrubber.Scroll += trkScrubber_Scroll;
        trkScrubber.MouseUp += trkScrubber_MouseUp;

        Controls.Add(trkScrubber);
        trkScrubber.BringToFront();
    }

    // mark the start of a manual seek operation.
    private void trkScrubber_MouseDown(object sender, MouseEventArgs e)
    {
        isScrubbing = true;
    }

    // update the playhead to reflect the slider position immediately.
    private void trkScrubber_Scroll(object sender, EventArgs e)
    {
        scrubbedXPosition = trkScrubber.Value;
        lblPlayhead.Left = TrackHeaderWidth + scrubbedXPosition;
    }

    // finish the seek and restart playback from the new position if needed.
    private void trkScrubber_MouseUp(object sender, MouseEventArgs e)
    {
        isScrubbing = false;
        scrubbedXPosition = trkScrubber.Value;

        if (trackManager.State == NAudio.Wave.PlaybackState.Playing)
        {
            trackManager.PlayAllFrom(scrubbedXPosition);
        }
    }
    private void FrmMain_Load(object sender, EventArgs e)
    {

    }
}