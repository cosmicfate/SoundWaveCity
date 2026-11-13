using System.Text.Json;

namespace MediocreSoundDesigner;

/// <summary>first-run choices stored in local application data.</summary>
public sealed class OnboardingPreferences
{
    // remember whether the user has finished the initial setup wizard.
    public bool Completed { get; set; }
    // keep the selected theme even when the system theme option is enabled.
    public bool DarkTheme { get; set; } = true;
    // when enabled, the windows app theme takes priority over the saved light or dark choice.
    public bool FollowSystemTheme { get; set; }
    // use stronger foreground and background separation for better readability.
    public bool HighContrast { get; set; }
    // store the accent as a hex string so json can serialize it without a custom converter.
    public string AccentHex { get; set; } = "#9469EF";
    // save the waveform display preference for future launches.
    public bool ColorfulClips { get; set; } = true;

    // save preferences separately for each windows user under local app data.
    private static string PreferencePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MediocreSoundDesigner", "onboarding.json");

    // restore the last saved choices, or fall back to defaults on missing or invalid data.
    public static OnboardingPreferences Load()
    {
        try
        {
            // only attempt to read the json file if it already exists.
            if (File.Exists(PreferencePath))
                return JsonSerializer.Deserialize<OnboardingPreferences>(File.ReadAllText(PreferencePath))
                    ?? new OnboardingPreferences();
        }
        catch (Exception) { /* corrupt or inaccessible file: show setup again. */ }
        return new OnboardingPreferences();
    }

    // write the current settings to disk when the user finishes the wizard.
    public void Save()
    {
        string folder = Path.GetDirectoryName(PreferencePath)!;
        // create the application folder automatically on the first save.
        Directory.CreateDirectory(folder);
        File.WriteAllText(PreferencePath, JsonSerializer.Serialize(this,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    // read the current windows application theme from the per-user registry.
    public static bool WindowsUsesLightTheme()
    {
        try
        {
            // a value of one indicates light mode; zero indicates dark mode.
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 0)) != 0;
        }
        catch { return false; }
    }

    // decide which theme to apply without overwriting the user's original selection.
    public bool EffectiveDarkTheme => !FollowSystemTheme ? DarkTheme : !WindowsUsesLightTheme();

    // convert the saved hex value back into a drawing color.
    public Color AccentColor
    {
        get
        {
            // use a safe default if a manually edited or old json file contains a bad color.
            try { return ColorTranslator.FromHtml(AccentHex); }
            catch { return Color.MediumPurple; }
        }
    }

    /// <summary>
    /// integrates with the existing palette. per-track clip colors also require
    /// integration in the track rendering code.
    /// </summary>
    public void ApplyToAppColors()
    {
        // calculate the active palette once before assigning application colors.
        bool dark = EffectiveDarkTheme;
        // set the main window background and text contrast.
        Settings.Color.WinBg = HighContrast ? (dark ? Color.Black : Color.White)
            : (dark ? Color.FromArgb(44, 49, 55) : Color.FromArgb(246, 247, 250));
        Settings.Color.WinFg = dark ? Color.White : Color.FromArgb(25, 28, 33);
        // selected and unselected tracks use different surfaces to indicate focus.
        Settings.Color.TrackBg_Unselected = HighContrast ? (dark ? Color.Black : Color.White)
            : (dark ? Color.FromArgb(39, 41, 54) : Color.FromArgb(231, 234, 241));
        Settings.Color.TrackBg_Selected = HighContrast ? (dark ? Color.FromArgb(35, 35, 35) : Color.FromArgb(225, 225, 225))
            : (dark ? Color.FromArgb(62, 53, 88) : Color.FromArgb(222, 213, 247));
        // the accent highlights the playhead and enabled waveform samples.
        Settings.Color.Playline = AccentColor;
        Settings.Color.TrackSamples_Enabled = HighContrast ? (dark ? Color.White : Color.Black) : AccentColor;
        // muted or disabled tracks use a neutral waveform color.
        Settings.Color.TrackSamples_Disabled = dark ? Color.Gray : Color.DarkGray;
    }
}
