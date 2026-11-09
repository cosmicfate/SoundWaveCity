using MediocreSoundDesigner.Properties;
using System.Text.Json;

namespace MediocreSoundDesigner;

public partial class FrmMain : Form {
  // fields
  private TrackManager trackManager;

  // ctor
  public FrmMain() {
    InitializeComponent();
    Setup();
    SizeChanged += FrmMain_SizeChanged;
  }

  // setup
  private void Setup() {
    DoubleBuffered = true;
    trackManager = new(panTracks);
    btnPlay.BackgroundImage = Resources.play;
    trackManager.DrawTimescale = trkDrawTimescale.Value;
    UpdateSizes();
    ApplySettings();
    SetupScrubber(); // Initialize the scrubber
    }
  private void ApplySettings() {
    BackColor = Settings.Color.WinBg;
    panTracks.BackColor = Settings.Color.TrackBg_Unselected;
    lblPlayhead.BackColor = Settings.Color.Playline;
    trackManager.ApplySettings();
  }

    // GUI controls
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
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
                if (selectedTrack.SelectedClip is not null & selectedTrack.Clips.Count != 1)
                {
                    if (MessageBox.Show("This will delete the selected clip. This is NOT undoable!", "Are You Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                    {
                        selectedTrack.RemoveSelectedClip();
                        selectedTrack.PanDrawSamples.Invalidate();
                    }
                }
                else
                {
                    if (MessageBox.Show("This will delete the selected track. This is NOT undoable!", "Are You Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                    {
                        trackManager.RemTrack(selectedTrack);
                        panTracks.Controls.Remove(selectedTrack.PanDrawSamples);
                        selectedTrack.Dispose();
                        UpdateSizes();
                    }
                }
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

  private void FrmMain_SizeChanged(object sender, EventArgs e) {
    UpdateSizes();
  }

  //private void btnPlay_Click(object sender, EventArgs e) {
  //  if (trackManager.State is NAudio.Wave.PlaybackState.Stopped or NAudio.Wave.PlaybackState.Paused) {
  //    trackManager.PlayAll();
  //    btnPlay.BackgroundImage = Resources.pause;
  //  }
  //  else {
  //    trackManager.PauseAll();
  //    btnPlay.BackgroundImage = Resources.play;
  //  }
  //}

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
  private void menuFileSave_Click(object sender, EventArgs e) {
    sfdSaveFile.DefaultExt = "wav";
    if (sfdSaveFile.ShowDialog() == DialogResult.OK) {
      trackManager.SaveToFile(sfdSaveFile.FileName);
    }
  }
  private void menuFileSettings_Click(object sender, EventArgs e) {
    FrmEditSettings frm = new();
    frm.SetSound(trackManager.SelectedTrack);
    if (frm.ShowDialog() == DialogResult.OK) {
      ApplySettings();
    }
  }

  private void menuEditFlip_Click(object sender, EventArgs e) {
    trackManager.SelectedTrack?.Flip();
  }
  private void menuEditReverse_Click(object sender, EventArgs e) {
    trackManager.SelectedTrack?.Reverse();
  }
  private void menuEditVolume_Click(object sender, EventArgs e) {
    if (trackManager.SelectedTrack is not null) {
      FrmEditVolume frm = new();
      frm.SetSound(trackManager.SelectedTrack);
      frm.ShowDialog();
    }
  }
  private void menuEditEliminateSilence_Click(object sender, EventArgs e) {
    if (trackManager.SelectedTrack?.SelectedClip is not null) {
      FrmEditEliminateSilence frm = new();
      frm.SetSound(trackManager.SelectedTrack);
      frm.ShowDialog();
    }
  }

  private void menuEditSplit_Click(object sender, EventArgs e) {
    // Split works on the selected track, like every other Edit menu command.
    Track track = trackManager.SelectedTrack;
    if (track is null) return;

     // Split the clip under the edit cursor (a pixel X position). Both halves
     // stay on this track, so there are no new panels or rows and UpdateSizes
     // isn't needed. SplitAt returns false if the cursor isn't inside a clip.
     if (track.SplitAt(scrubbedXPosition))
     {
         // Repaint so the new clip edge and the selection outline appear.
         track.PanDrawSamples.Invalidate();
     }
  }

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

    //private void tmrUpdatePlaybackState_Tick(object sender, EventArgs e) {
    //  if (trackManager.Tracks.Count > 0) {
    //    btnPlay.BackgroundImage = trackManager.State == NAudio.Wave.PlaybackState.Playing ? Resources.pause : Resources.play;
    //    btnPlay.Visible = true;
    //    lblPlayhead.Left = trackManager.GetPlayheadPos();
    //    lblPlayhead.Visible = true;
    //  }
    //  else {
    //    btnPlay.BackgroundImage = null;
    //    btnPlay.Visible = false;
    //    lblPlayhead.Visible = false;
    //  }
    //}

    private void tmrUpdatePlaybackState_Tick(object sender, EventArgs e)
    {
        if (trackManager.Tracks.Count > 0)
        {
            bool isPlaying = trackManager.State == NAudio.Wave.PlaybackState.Playing;
            btnPlay.BackgroundImage = isPlaying ? Resources.pause : Resources.play;
            btnPlay.Visible = true;

            if (!isScrubbing)
            {
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

    private void trkDrawTimescale_Scroll(object sender, EventArgs e) {
    trackManager.DrawTimescale = trkDrawTimescale.Maximum - trkDrawTimescale.Value;
    trackManager.RedrawAll();
  }

  // Helper methods
  private Panel CreateTrackPanel(Track track) {
    Panel pan = new() {
      BackColor = Settings.Color.TrackBg_Unselected,
      Width = panTracks.Width,
      Left = 0,
      Top = 0,
    };
    pan.Click += (sender, e) => {
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
    };
    pan.DoubleClick += (sender, e) => {
      track.ToggleDisabled();
      track.PanDrawSamples.Invalidate();
    };
    pan.Paint += (sender, e) => {
      track.DrawSamples(e.Graphics, trackManager.DrawTimescale);
    };
    panTracks.Controls.Add(pan);
    track.LinkPanels(pan, panTracks);
    trackManager.SelectTrack(track);

    return pan;
  }

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

    private TrackBar trkScrubber;
    private bool isScrubbing = false;
    private int scrubbedXPosition = 0;

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

    private void trkScrubber_MouseDown(object sender, MouseEventArgs e)
    {
        isScrubbing = true;
    }

    private void trkScrubber_Scroll(object sender, EventArgs e)
    {
        scrubbedXPosition = trkScrubber.Value;
        lblPlayhead.Left = scrubbedXPosition;
    }

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
