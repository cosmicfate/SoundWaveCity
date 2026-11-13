using System.Text.Json;
using System.Text.Json.Serialization;
using Color = System.Drawing.Color;

namespace MediocreSoundDesigner;

public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => ColorTranslator.FromHtml(reader.GetString());
    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) => writer.WriteStringValue("#" + value.R.ToString("X2") + value.G.ToString("X2") + value.B.ToString("X2").ToLower());
}

public static class Settings
{
    public class Colors
    {
        public Color WinBg { get; set; }
        public Color WinFg { get; set; }
        public Color TrackSamples_Enabled { get; set; }
        public Color TrackSamples_Disabled { get; set; }
        public Color TrackBg_Unselected { get; set; }
        public Color TrackBg_Selected { get; set; }
        public Color Playline { get; set; }
        public Color ClipSelectionOutline => ControlPaint.Dark(TrackSamples_Enabled, .15f);

        public Colors()
        {
            WinBg = System.Drawing.Color.Black;
            WinFg = System.Drawing.Color.White;
            TrackSamples_Enabled = System.Drawing.Color.Cyan;
            TrackSamples_Disabled = System.Drawing.Color.LightGray;
            TrackBg_Unselected = System.Drawing.Color.DimGray;
            TrackBg_Selected = System.Drawing.Color.Gray;
            Playline = System.Drawing.Color.White;
        }
    }

    static Settings()
    {
        LoadFromFile();
    }

    public static Colors Color { get; private set; }

    private const string FILENAME = "settings.json";

    public static void SaveToFile()
    {
        JsonSerializerOptions options = new() { Converters = { new ColorJsonConverter() } };
        File.WriteAllText(FILENAME, JsonSerializer.Serialize(Color, options));
    }

    public static void LoadFromFile()
    {
        if (File.Exists(FILENAME))
        {
            JsonSerializerOptions options = new() { Converters = { new ColorJsonConverter() } };
            Color = JsonSerializer.Deserialize<Colors>(File.ReadAllText(FILENAME), options);
        }
        else
        {
            Color = new();
        }
    }

    public static void RevertToDefault()
    {
        Color = new();
    }
}
