namespace MediocreSoundDesigner.Code


{
    public class Clip
    {
        // Samples: the raw audio data for the clip, represented as an array of floats.
        public float[] Samples { get; set; }

        // StartSample: the starting sample index of the clip within a larger audio track.
        // Stored in Samples rather than pixels so the clip stays in place on the timeline.
        public int StartSample { get; set; }

        // EndSample: The first time sample after the clip ends, calculated as StartSample + Samples.Length.
        public int EndSample => StartSample + Samples.Length;

        public Clip(float[] samples, int startSample)
        {
            Samples = samples;
            StartSample = startSample;
        }

        // Contains: is True if the given timeline sample falls inside this clip.
        public bool Contains(int timelineSample) =>
            timelineSample >= StartSample && timelineSample < EndSample;
    }
}
