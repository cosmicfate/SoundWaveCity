using MediocreSoundDesigner.Code;
using NAudio.Wave;
using System.Drawing.Drawing2D;

namespace MediocreSoundDesigner;

public enum SampleRegion {
  BEGINNING_ONLY,
  END_ONLY,
  BEGINNING_AND_END,
  ANYWHERE,
}

public class Track : IDisposable
{
    // Fields
    private string file;
    private TimeSpan duration;
    private float offset;
    private float timeScale;
    private int sampleRate;

    // All clips on the track, kept in order by StartSample, list object created only once.
    private readonly List<Clip> clips = new();

    // Read-only view of the clips. Callers can look at clips but can't add or remove without going through Track.cs methods.
    public IReadOnlyList<Clip> Clips => clips;

    // The clip that is "selected" and allows for edits to act on.
    public Clip SelectedClip { get; private set; }

    // Compatability change to support the addition of clips. Samples used to be the track's only audio array
    // this now redirects to the selected clip's array so now every edit is only on the selected clip.
    private float[] samples
    {
        get => SelectedClip.Samples;
        set => SelectedClip.Samples = value;
    }
    private float pitch;
    private int bits;
    private int channels;
    private AudioFileReader fileReader;
    private WaveOut outputDevice;
    private Panel panContainer;
    private int drawTimescale;

    private int startXPosition = 0;

    // properties

    // DrawOffset() removed because each Clip now stores its own position (Clip.StartSample). -G
    //public int DrawOffset { get; set; } 
    public bool IsDisabled { get; private set; }
    public Panel PanDrawSamples { get; private set; }
    public float Volume { get; private set; }
    private string tempFilePath => Path.Combine(Path.GetTempPath(), "MediocreSoundDesigner", "temp.wav");
    public PlaybackState State => outputDevice?.PlaybackState ?? PlaybackState.Stopped;

    // ctor
    public Track(string file)
    {
        fileReader = new AudioFileReader(file);
        int sampleCount = (int)(fileReader.Length / 4);
        // samples is read into local array now because now 'samples' needs a SelectedClip that doesn't exist yet
        float[] fileSamples = new float[sampleCount];
        int samplesRead = ((ISampleProvider)fileReader).Read(fileSamples);
        if (samplesRead < sampleCount)
        {
            Array.Resize(ref fileSamples, samplesRead);
        }
        // whole file becomes one clip that starts at pos 0 and is selected
        clips.Add(new Clip(fileSamples, 0));
        SelectedClip = clips[0];
        this.file = file;
        this.duration = fileReader.TotalTime;
        this.offset = 0;
        this.timeScale = 1.0f;
        this.Volume = 1.0f;
        this.pitch = 1.0f;
        this.bits = fileReader.WaveFormat.BitsPerSample;
        this.channels = fileReader.WaveFormat.Channels;
        this.sampleRate = fileReader.WaveFormat.SampleRate;
        IsDisabled = false;
    }
    public Track(float[] samples)
    {
        clips.Add(new Clip(samples, 0));
        SelectedClip = clips[0];

        this.file = "";
        this.duration = TimeSpan.Zero;
        this.offset = 0;
        this.timeScale = 1.0f;
        this.Volume = 1.0f;
        this.pitch = 1.0f;
        this.bits = 32;
        this.channels = 1;
        this.sampleRate = 0;
        IsDisabled = false;
    }
    public void LinkPanels(Panel panDrawSamples, Panel panContainer)
    {
        this.PanDrawSamples = panDrawSamples;
        this.panContainer = panContainer;
    }

    // edits
    public float[] CloneSamples()
    {
        return (float[])samples.Clone();
    }
    public void ReplaceSamples(float[] samples)
    {
        this.samples = samples;
    }
    public void Reverse()
    {
        for (int i = 0; i < samples.Length / 2; i++)
        {
            var temp = samples[i];
            samples[i] = samples[samples.Length - 1 - i];
            samples[samples.Length - 1 - i] = temp;
        }
    }
    public void Flip()
    {
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= -1;
        }
    }
    public void Trim()
    {
    }
    public void AlterTimeScale(float scale)
    {
    }
    public void SetVolume(float newVolume)
    {
        Volume = newVolume;
    }
    public void EliminateSilence(SampleRegion region, float minDuration, float thres)
    {
        float[] newSamples = CloneSamples();
        if (region is SampleRegion.BEGINNING_ONLY or SampleRegion.BEGINNING_AND_END)
        {
            int silentDurationInNumSamples = 0;
            for (int i = 0; i < newSamples.Length; i++)
            {
                if (Math.Abs(newSamples[i]) <= thres)
                {
                    silentDurationInNumSamples++;
                }
                else
                {
                    break;
                }
            }
            if ((float)silentDurationInNumSamples / sampleRate >= minDuration)
            {
                newSamples = newSamples[silentDurationInNumSamples..];
            }
        }
        if (region is SampleRegion.END_ONLY or SampleRegion.BEGINNING_AND_END)
        {
            int silentDurationInNumSamples = 0;
            for (int i = newSamples.Length - 1; i >= 0; i--)
            {
                if (Math.Abs(newSamples[i]) <= thres)
                {
                    silentDurationInNumSamples++;
                }
                else
                {
                    break;
                }
            }
            if ((float)silentDurationInNumSamples / sampleRate >= minDuration)
            {
                newSamples = newSamples[0..^silentDurationInNumSamples];
            }
        }
        if (region is SampleRegion.ANYWHERE)
        {
            int silentDurationInNumSamples = 0;
            int silentRegionStart = 0;
            int i;
            for (i = 0; i < newSamples.Length; i++)
            {
                if (Math.Abs(newSamples[i]) <= thres)
                {
                    silentDurationInNumSamples++;
                }
                else
                {
                    if ((float)silentDurationInNumSamples / sampleRate >= minDuration)
                    {
                        newSamples = [.. newSamples[0..silentRegionStart], .. newSamples[i..]];
                        i = 0;
                    }
                    silentRegionStart = i + 1;
                    silentDurationInNumSamples = 0;
                }
            }
            if ((float)silentDurationInNumSamples / sampleRate >= minDuration)
            {
                newSamples = [.. newSamples[0..silentRegionStart], .. newSamples[i..]];
            }
        }
        samples = newSamples;
    }

    // converts an X position on the track panel into a sample index
    private int TimelineSampleAt(int panelX) =>
        panelX * Math.Max(1, drawTimescale);

    // returns clip within the given panel X position, or null if between clips, before the first, or after the last.
    public Clip ClipAt(int panelX)
    {
        int t = TimelineSampleAt(panelX);
        // List.Find returns the first clip whose Contains(t) is True or null
        return clips.Find(c => c.Contains(t));
    }

    // selects clip under the given panel X position. returns False and maintains current selection if no clip is present at that position.
    public bool SelectClipAt(int panelX)
    {
        Clip clip = ClipAt(panelX);
        if (clip is null) return false;
        SelectedClip = clip;
        return true;
    }

    // Split: splits the clip at the panel X position into two separate clips on the track.
    // returns false if nothing is split either because the point is not inside a clip or if 
    // the point is at the clip's very first sample
    public bool SplitAt(int panelX)
    {
        // find which clip the point is at
        int t = TimelineSampleAt(panelX);
        Clip clip = clips.Find(c => c.Contains(t));
        if (clip is null) return false;

        // make position relative to the start of the clip; gives index into clip.Samples
        int index = t - clip.StartSample;
        index -= index % Math.Max(1, channels);

        // prevents index from being at the very start or past the end
        if (index <= 0 || index >= clip.Samples.Length) return false;

        // the second half of a split clip starts exactly where the first half ends.
        Clip right = new(clip.Samples[index..], clip.StartSample + index);  // [index..] copies everything from index to the end.

        // the original (left half) of the clip gets shortened to the index.
        clip.Samples = clip.Samples[..index];

        // insert the right half directly after the left half
        clips.Insert(clips.IndexOf(clip) + 1, right);

        // for convenience to the user, the right half gets selected for easy movement/edit (right arrow)
        SelectedClip = right;
        return true;
    }

    // play
    public void Play()
    {
        outputDevice?.Dispose();
        fileReader?.Dispose();
        SaveToTempFile();
        fileReader = new AudioFileReader(tempFilePath);
        outputDevice = new WaveOut();
        outputDevice.Init(fileReader);
        outputDevice.Play();
    }
    public void Pause()
    {
        outputDevice?.Pause();
    }
    public void Reset()
    {
    }
    //public int GetPlayheadPos()
    //{
    //    return (int)((outputDevice?.GetPosition() ?? 0) / 4);
    //}

    public int GetPlayheadPos()
    {
        int samplesPlayed = (int)((outputDevice?.GetPosition() ?? 0) / 4);
        return (startXPosition * drawTimescale) + samplesPlayed;
    }

    public void ToggleDisabled()
    {
        IsDisabled = !IsDisabled;
    }

    // file i/o
    private void SaveToTempFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
        using var writer = new WaveFileWriter(tempFilePath, fileReader?.WaveFormat ?? new());
        // since 'samples' now only references the selectedClip, using output's length writes everything
        float[] output = GetAdjustedSamples();
        writer.WriteSamples(output, 0, output.Length);
    }
    public void SaveToFile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using var writer = new WaveFileWriter(path, fileReader?.WaveFormat ?? new());
        // since 'samples' now only references the selectedClip, using output's length writes everything
        float[] output = GetAdjustedSamples();
        writer.WriteSamples(output, 0, output.Length);
    }
    public float[] GetAdjustedSamples()
    {
        int length = clips.Max(c => c.EndSample);
        float[] adjusted = new float[length];

        foreach (Clip clip in clips)
        {
            for (int s = 0; s < clip.Samples.Length; s++)
            {
                // copies each sample to its timeline position, scaled by track's volume
                adjusted[clip.StartSample + s] += clip.Samples[s] * Volume;
            }
        }
        return adjusted;
    }

    // render
    public void ShiftSampleOver(int amount)
    {
        int delta = amount * Math.Max(1, drawTimescale);
        delta -= delta % Math.Max(1, channels);
        int i = clips.IndexOf(SelectedClip);
        int min = i > 0 ? clips[i - 1].EndSample : 0;
        int max = i < clips.Count - 1
            ? clips[i + 1].StartSample - SelectedClip.Samples.Length
            : int.MaxValue;
        SelectedClip.StartSample = Math.Clamp(SelectedClip.StartSample + delta, min, max);
    }
    public void DrawSamples(Graphics g, int drawTimescale)
    {
        if (PanDrawSamples is null)
        {
            return;
        }

        this.drawTimescale = drawTimescale;
        int ts = Math.Max(1, drawTimescale);

        int height = PanDrawSamples.Height;
        int middle = height / 2;            
        float heightAdj = height / 2;       

        using Pen samplePen = new(IsDisabled ? Settings.Color.TrackSamples_Disabled : Settings.Color.TrackSamples_Enabled);
        using Pen edgePen = new(Settings.Color.WinBg);
        using Pen selectedPen = new(Settings.Color.TrackSamples_Disabled) { DashStyle = DashStyle.Dash };

        foreach (Clip clip in clips)
        {
            int startX = clip.StartSample / ts;
            int endX = clip.EndSample / ts;

            if (clip.Samples.Length > 0)
            {
                List<Point> points = new();
                int x = startX;
                for (int s = 0; s < clip.Samples.Length; s += ts)
                {
                    var sample = (int)(clip.Samples[s] * heightAdj * Volume);
                    points.Add(new(x, middle));         
                    points.Add(new(x, middle - sample)); 
                    x++;
                }
                g.DrawLines(samplePen, points.ToArray());
            }

            if (clips.Count > 1)
            {
                g.DrawLine(edgePen, startX, 0, startX, height);
                g.DrawLine(edgePen, endX, 0, endX, height);

                if (clip == SelectedClip)
                {
                    g.DrawRectangle(selectedPen, startX, 0, endX - startX, height - 1);
                }
            }
        }
    }

    // dispose
    public void Dispose()
    {
        outputDevice?.Dispose();
        fileReader?.Dispose();
    }


    // scrubbing code
    private TimeSpan scrubPosition = TimeSpan.Zero;

    public TimeSpan ScrubPosition
    {
        get => scrubPosition;
        set
        {
            scrubPosition = value;
            if (fileReader != null)
            {
                if (value < TimeSpan.Zero) value = TimeSpan.Zero;
                if (value > fileReader.TotalTime) value = fileReader.TotalTime;
                fileReader.CurrentTime = value;
            }
        }
    }

    public TimeSpan CurrentTime => fileReader?.CurrentTime ?? scrubPosition;

    public TimeSpan TotalTime => fileReader?.TotalTime ?? duration;

    public void PlayFrom(TimeSpan startPosition)
    {
        scrubPosition = startPosition;
        outputDevice?.Dispose();
        fileReader?.Dispose();

        SaveToTempFile();
        fileReader = new AudioFileReader(tempFilePath);

        if (startPosition < fileReader.TotalTime)
        {
            fileReader.CurrentTime = startPosition;
        }

        outputDevice = new WaveOut();
        outputDevice.Init(fileReader);
        outputDevice.Play();
    }

    public void PlayFromPosition(int xPos)
    {
        startXPosition = xPos;
        outputDevice?.Dispose();
        fileReader?.Dispose();
        SaveToTempFile();
        fileReader = new AudioFileReader(tempFilePath);

        long targetByte = (long)xPos * drawTimescale * 4;
        targetByte -= targetByte % 4;
        fileReader.Position = Math.Clamp(targetByte, 0, fileReader.Length);

        outputDevice = new WaveOut();
        outputDevice.Init(fileReader);
        outputDevice.Play();
    }

    public void SetDrawTimescale(int timescale)
    {
        this.drawTimescale = timescale;
    }


}