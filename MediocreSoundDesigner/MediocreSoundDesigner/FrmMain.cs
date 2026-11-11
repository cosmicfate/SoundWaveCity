using MediocreSoundDesigner.Properties;
using System.Text.Json;

namespace MediocreSoundDesigner;

public partial class FrmMain : Form {
  // fields
  // manage imported audio tracks, selection, drawing, and playback.
  private TrackManager trackManager;

  // ctor
  // initialize the editor, then attach the appearance menu and resize handler.
  public FrmMain() {
    InitializeComponent();
    Setup();
    AddAppearanceMenu();
    SizeChanged += FrmMain_SizeChanged;
  }

  // setup
  // configure the track display, playback button, and timeline controls.
  private void Setup() {
    DoubleBuffered = true;
    trackManager = new(panTracks);
    btnPlay.BackgroundImage = Resources.play;
    trackManager.DrawTimescale = trkDrawTimescale.Value;
    UpdateSizes();
    ApplySettings();
    SetupScrubber(); // initialize the scrubber
    }
  // refresh visible editor colors from the shared application palette.
  private void ApplySettings() {
    BackColor = Settings.Color.WinBg;
    ForeColor = Settings.Color.WinFg;
    panTracks.BackColor = Settings.Color.TrackBg_Unselected;
    lblPlayhead.BackColor = Settings.Color.Playline;
    trackManager.ApplySettings();
  }

  // let users revisit the same choices without resetting their setup
  // allow users to reopen the wizard without resetting the first-run flag.
  private void AddAppearanceMenu() {
    ToolStripMenuItem appearance = new("Appearance...");
    appearance.Click += (sender, e) => {
      // load a fresh copy so the wizard begins with the last saved choices.
      var preferences = OnboardingPreferences.Load();
      using var wizard = new FrmOnboarding(preferences, editingAppearance: true);
      // only apply and redraw when the user saves changes with finish.
      if (wizard.ShowDialog(this) == DialogResult.OK) {
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
  protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
    if (trackManager.SelectedTrack is not null) {
      if (keyData == Keys.Left) {
        trackManager.SelectedTrack.ShiftSampleOver(-5);
        trackManager.SelectedTrack.PanDrawSamples.Invalidate();
        return true;
      }
      else if (keyData == Keys.Right) {
        trackManager.SelectedTrack.ShiftSampleOver(+5);
        trackManager.SelectedTrack.PanDrawSamples.Invalidate();
        return true;
      }
      else if (keyData == Keys.Delete) {
        if (trackManager.SelectedTrack is not null) {
          if (MessageBox.Show("This will delete the selected track. This is NOT undoable!", "Are You Sure?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation) == DialogResult.Yes) {
            panTracks.Controls.Remove(trackManager.SelectedTrack.PanDrawSamples);
            trackManager.SelectedTrack.PanDrawSamples.Dispose();
            trackManager.SelectedTrack.Dispose();
            trackManager.RemTrack(trackManager.SelectedTrack);
            UpdateSizes();
            return true;
          }
        }
      }
    }
    return base.ProcessCmdKey(ref msg, keyData);
  }

  // resize the timeline and track panels when the editor window changes size.
  private void FrmMain_SizeChanged(object sender, EventArgs e) {
    UpdateSizes();
  }

    // start playback at the chosen scrubber position or pause current playback.
    private void btnPlay_Click(object sender, EventArgs e) {
      if (trackManager.State is NAudio.Wave.PlaybackState.Stopped or NAudio.Wave.PlaybackState.Paused) {
        trackManager.PlayAllFrom(trkScrubber?.Value ?? 0);
        btnPlay.BackgroundImage = Resources.pause;
      }
      else {
        trackManager.PauseAll();
        btnPlay.BackgroundImage = Resources.play;
      }
    }

  // import a wave file and add its waveform panel to the editor.
  private void menuFileOpen_Click(object sender, EventArgs e) {
    ofdLoadFile.DefaultExt = "wav";
    if (ofdLoadFile.ShowDialog() == DialogResult.OK) {
      Track newTrack = new(ofdLoadFile.FileName);
      CreateTrackPanel(newTrack);
      trackManager.AddTrack(newTrack);
      UpdateSizes();
      newTrack.PanDrawSamples.Invalidate();
    }
  }
  // export the current audio mix through the track manager.
  private void menuFileSave_Click(object sender, EventArgs e) {
    sfdSaveFile.DefaultExt = "wav";
    if (sfdSaveFile.ShowDialog() == DialogResult.OK) {
      trackManager.SaveToFile(sfdSaveFile.FileName);
    }
  }
  // open the existing color settings dialog and refresh its changes.
  private void menuFileSettings_Click(object sender, EventArgs e) {
    FrmEditSettings frm = new();
    frm.SetSound(trackManager.SelectedTrack);
    if (frm.ShowDialog() == DialogResult.OK) {
      ApplySettings();
    }
  }

  // invert the selected waveform vertically.
  private void menuEditFlip_Click(object sender, EventArgs e) {
    trackManager.SelectedTrack?.Flip();
  }
  // reverse the order of samples in the selected track.
  private void menuEditReverse_Click(object sender, EventArgs e) {
    trackManager.SelectedTrack?.Reverse();
  }
  // open the volume adjustment dialog for the selected track.
  private void menuEditVolume_Click(object sender, EventArgs e) {
    if (trackManager.SelectedTrack is not null) {
      FrmEditVolume frm = new();
      frm.SetSound(trackManager.SelectedTrack);
      frm.ShowDialog();
    }
  }
  // open silence removal controls for the selected track.
  private void menuEditEliminateSilence_Click(object sender, EventArgs e) {
    if (trackManager.SelectedTrack is not null) {
      FrmEditEliminateSilence frm = new();
      frm.SetSound(trackManager.SelectedTrack);
      frm.ShowDialog();
    }
  }

  // create a new track and let the user record microphone audio into it.
  private void menuGenRecord_Click(object sender, EventArgs e) {
    Track newTrack = new(new float[0]);
    CreateTrackPanel(newTrack);
    trackManager.AddTrack(newTrack);
    UpdateSizes();
    newTrack.PanDrawSamples.Invalidate();

    FrmGenRecord frm = new();
    frm.SetSound(trackManager.SelectedTrack);
    if (frm.ShowDialog() == DialogResult.Cancel) {
      trackManager.RemTrack(newTrack);
    }
  }
  // generate a track containing randomized white-noise samples.
  private void menuGenNoise_Click(object sender, EventArgs e) {
    const int NUM_SAMPLES = 100_000;
    float[] samples = new float[NUM_SAMPLES];
    Random rand = new();
    for (int i = 0; i < NUM_SAMPLES; i++) {
      samples[i] = rand.NextSingle() * 2.0f - 1.0f;
    }
    Track newTrack = new(samples);
    CreateTrackPanel(newTrack);
    trackManager.AddTrack(newTrack);
    UpdateSizes();
    newTrack.PanDrawSamples.Invalidate();
  }
  // generate slowly varying random values for a smoother noise waveform.
  private void menuGenGradientNoise_Click(object sender, EventArgs e) {
    const int NUM_SAMPLES = 250_000;
    float[] samples = new float[NUM_SAMPLES];
    Random rand = new();
    float cur = 0;
    float inc = 0.005f;
    int dir = +1;
    for (int i = 0; i < NUM_SAMPLES; i++) {
      cur += (inc * dir);
      if (rand.NextSingle() < 0.5f) {
        dir *= -1;
      }
      if (cur > 0.9f) {
        dir = -1;
      }
      if (cur < -0.9f) {
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
                    lblPlayhead.Left = pos;
                    if (trkScrubber != null && pos >= trkScrubber.Minimum && pos <= trkScrubber.Maximum)
                    {
                        trkScrubber.Value = pos;
                    }
                }
                else
                {
                    lblPlayhead.Left = scrubbedXPosition;
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
    private void trkDrawTimescale_Scroll(object sender, EventArgs e) {
    trackManager.DrawTimescale = trkDrawTimescale.Maximum - trkDrawTimescale.Value;
    trackManager.RedrawAll();
  }

  // helper methods
  // create a clickable and paintable panel for a newly added audio track.
  private Panel CreateTrackPanel(Track track) {
    Panel pan = new() {
      BackColor = Settings.Color.TrackBg_Unselected,
      Width = panTracks.Width,
      Left = 0,
      Top = 0,
    };
    // a single click selects the track for keyboard and editing commands.
    pan.Click += (sender, e) => {
      trackManager.SelectTrack(track);
    };
    // double-click to enable or disable the track without removing it.
    pan.DoubleClick += (sender, e) => {
      track.ToggleDisabled();
      track.PanDrawSamples.Invalidate();
    };
    // delegate waveform rendering to the track object.
    pan.Paint += (sender, e) => {
      track.DrawSamples(e.Graphics, trackManager.DrawTimescale);
    };
    panTracks.Controls.Add(pan);
    track.LinkPanels(pan, panTracks);
    trackManager.SelectTrack(track);

    return pan;
  }

  // arrange track panels and adjust the scrubber width after layout changes.
  private void UpdateSizes() {
    const int PADDING = 15;

    panTracks.Left = PADDING;
    panTracks.Width = ClientSize.Width - (PADDING * 2);

    if (trkScrubber != null) {
        trkScrubber.Left = panTracks.Left;
        trkScrubber.Width = panTracks.Width;
        trkScrubber.Maximum = panTracks.Width;
    }

    int top = 0;
    foreach (Track track in trackManager.Tracks) {
      track.PanDrawSamples.Left = 0;
      track.PanDrawSamples.Width = panTracks.Width;
      track.PanDrawSamples.Height = (panTracks.Height / trackManager.Tracks.Count) - 1;
      track.PanDrawSamples.Top = top;
      track.PanDrawSamples.Invalidate();
      top += track.PanDrawSamples.Height;
    }
    trkDrawTimescale.Width = panTracks.Width;
    trkDrawTimescale.Left = PADDING;
    lblPlayhead.Left = 0;
    lblPlayhead.Height = panTracks.Height;
    lblPlayhead.BringToFront();
  }

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
            Left = panTracks.Left,
            Top = Math.Max(0, panTracks.Top - 28),
            Width = panTracks.Width,
            Minimum = 0,
            Maximum = panTracks.Width,
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
        lblPlayhead.Left = scrubbedXPosition;
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
}