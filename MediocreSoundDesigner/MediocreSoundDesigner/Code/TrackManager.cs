using NAudio.Wave;
using MediocreSoundDesigner.code;

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

    // make sure a clip can be moved from one track to another, without actually moving it.
    public bool CanIMoveClip(Track source, Track target, Clip clip, int startSample, out string errorMessage)
    { 
        errorMessage = string.Empty;
        if(source == null || target == null || clip == null || !Tracks.Contains(source) || !Tracks.Contains(target))
        {
            errorMessage = "Invalid track or clip specified.";
            return false;
        }

        if (State == PlaybackState.playing)
        {
            errorMessage = "Cannot move clips while playback is active. Please pause playback first.";
            return false;
        }

        if(!source.Clips.Contains(clip))
        {
            errorMessage = "The specified clip does not belong to the source track.";
            return false;
        }

        if (!target.CanIPlaceClip(clip, startSample))
        {
            errorMessage = "Cannot place the clip at the specified position on the target track.";
            return false;
        }

        return true; // :)
    }

    // now that we know a clip can be moved, actually move it.
    public bool MoveClip(Track source, Track target, Clip clip, int startSample, out string errorMessage)
    {
        if (!CanIMoveClip(source, target, clip, startSample, out errorMessage))
        {
            return false; // can't move it
        }
        // remove from source track
        source.Clips.Remove(clip);
        // add to target track
        if (!target.AttachClip(clip, startSample))
        {
            errorMessage = "Failed to attach the clip to the target track.";
            return false;
        }

        SelectTrack(target); // select the target track after moving the clip
        source.PanDrawSamples?.Invalidate(); // redraw the source track
        target.PanDrawSamples?.Invalidate(); // redraw the target track
        return true; // :)
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
