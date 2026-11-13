
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MediocreSoundDesigner;

public static class AudioImport
{
    public const int SampleRate = 44100;

    public static float[] Load(string path)
    {
        using var reader = new AudioFileReader(path);
        ISampleProvider source = reader;

        // convert multiple audio channels into one mono channel
        if (source.WaveFormat.Channels > 1)
        {
            source = new MonoDownmixer(source);
        }

        // resample audio only when the sample rate is different
        if (source.WaveFormat.SampleRate != SampleRate)
        {
            source = new WdlResamplingSampleProvider(
                source, SampleRate);
        }

        // collect the converted samples into an array
        List<float> samples = new();
        float[] buffer = new float[8192];

        int count;

        while ((count = source.Read(buffer)) > 0)
        {
            for (int i = 0; i < count; i++)
            {
                samples.Add(buffer[i]);
            }
        }

        return samples.ToArray();
    }

    private sealed class MonoDownmixer : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly int channels;
        private float[] buffer = Array.Empty<float>();

        public WaveFormat WaveFormat { get; }

        public MonoDownmixer(ISampleProvider source)
        {
            this.source = source;
            channels = source.WaveFormat.Channels;

            if (channels < 1)
                throw new ArgumentException(
                    "invalid channel count");

            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(
                source.WaveFormat.SampleRate, 1);
        }

        public int Read(Span<float> output)
        {
            // request enough samples for all source channels
            int needed = checked(output.Length * channels);

            if (buffer.Length < needed)
                buffer = new float[needed];

            int read = source.Read(buffer.AsSpan(0, needed));
            int frames = read / channels;

            // average the channel samples into a mono sample
            for (int i = 0; i < frames; i++)
            {
                float sum = 0;

                for (int c = 0; c < channels; c++)
                    sum += buffer[i * channels + c];

                output[i] = sum / channels;
            }

            return frames;
        }
    }
}
