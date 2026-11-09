using NAudio.Wave;

namespace MediocreSoundDesigner; 

public class TrackManager {
  private Track pseudoCombinedTrack;
  private Panel panContainer;

  public List<Track> Tracks { get; private set; }
	public Track SelectedTrack { get; private set; }
  public int DrawTimescale { get; set; }
  public PlaybackState State => pseudoCombinedTrack?.State ?? PlaybackState.Stopped;

  public TrackManager(Panel panContainer) {
		Tracks = new();
    SelectedTrack = null;
    pseudoCombinedTrack = null;
    this.panContainer = panContainer;
  }

	public void AddTrack(Track track) {
		Tracks.Add(track);
	}

	public void RemTrack(Track track) {
		Tracks.Remove(track);
        // if removed track was selected, deselect to avoid a reference to it
        if (SelectedTrack == track)
        {
            SelectedTrack = null;
        }
    }

	public void SelectTrack(Track track) {
		SelectedTrack = track;
    Tracks.ForEach(track => {
      track.PanDrawSamples.BorderStyle = BorderStyle.None;
      track.PanDrawSamples.BackColor = Settings.Color.TrackBg_Unselected;
    });
    track.PanDrawSamples.BorderStyle = BorderStyle.Fixed3D;
    track.PanDrawSamples.BackColor = Settings.Color.TrackBg_Selected;
  }

  public void RedrawAll() {
    Tracks.ForEach(track => track.PanDrawSamples.Invalidate());
  }

  public void ApplySettings() {
    foreach (Track track in Tracks) {
      if (SelectedTrack == track) {
        track.PanDrawSamples.BackColor = Settings.Color.TrackBg_Selected;
      }
      else {
        track.PanDrawSamples.BackColor = Settings.Color.TrackBg_Unselected;
      }
    }
    RedrawAll();
  }

  public void SaveToFile(string filename) {
    CombineTracks();
    pseudoCombinedTrack.SaveToFile(filename);
  }

	public void PlayAll() {
    CombineTracks();
    pseudoCombinedTrack.Play();
  }
  //private void CombineTracks() {
  //  pseudoCombinedTrack?.Dispose();
  //  List<float> combinedSamples = new();
  //  foreach (Track track in Tracks) {
  //    if (track.IsDisabled) {
  //      continue;
  //    }
  //    float[] trackSamples = (float[])track.GetAdjustedSamples().Clone();
  //    for (int i = 0; i < trackSamples.Length; i++) {
  //      if (combinedSamples.Count <= i) {
  //        combinedSamples.Add(0);
  //      }
  //      combinedSamples[i] += trackSamples[i];
  //    }
  //  }
  //  pseudoCombinedTrack = new(combinedSamples.ToArray());
  //  pseudoCombinedTrack.LinkPanels(null, panContainer);
  //}

  private void CombineTracks() {
  pseudoCombinedTrack?.Dispose();
  List<float> combinedSamples = new();
  foreach (Track track in Tracks) {
    if (track.IsDisabled) {
      continue;
    }
    float[] trackSamples = (float[])track.GetAdjustedSamples().Clone();
    for (int i = 0; i < trackSamples.Length; i++) {
      if (combinedSamples.Count <= i) {
        combinedSamples.Add(0);
      }
      combinedSamples[i] += trackSamples[i];
    }
  }
  pseudoCombinedTrack = new(combinedSamples.ToArray());
  pseudoCombinedTrack.LinkPanels(null, panContainer);
  pseudoCombinedTrack.SetDrawTimescale(DrawTimescale);
}

  public void PauseAll() {
    pseudoCombinedTrack?.Pause();
  }
  public int GetPlayheadPos() {
    return (pseudoCombinedTrack?.GetPlayheadPos() ?? 0) / Math.Max(1, DrawTimescale);
   }

  public void PlayAllFrom(int xPos) {
    CombineTracks();
    pseudoCombinedTrack?.PlayFromPosition(xPos);
   }
}
