using System;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace MediocreSoundDesigner;

/// <summary>First-run choices, stored in the user's local application data.</summary>
public sealed class OnboardingPreferences
{
    public bool Completed { get; set; }
    public bool DarkTheme { get; set; } = true;
    public bool FollowSystemTheme { get; set; }
    public bool HighContrast { get; set; }
    public string AccentHex { get; set; } = "#9469EF";
    public bool ColorfulClips { get; set; } = true;

    private static string PreferencePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MediocreSoundDesigner", "onboarding.json");

    public static OnboardingPreferences Load()
    {
        try
        {
            if (File.Exists(PreferencePath))
                return JsonSerializer.Deserialize<OnboardingPreferences>(File.ReadAllText(PreferencePath))
                    ?? new OnboardingPreferences();
        }
        catch (Exception) { /* Corrupt or inaccessible file: show setup again. */ }
        return new OnboardingPreferences();
    }

    public void Save()
    {
        string folder = Path.GetDirectoryName(PreferencePath)!;
        Directory.CreateDirectory(folder);
        File.WriteAllText(PreferencePath, JsonSerializer.Serialize(this,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    public static bool WindowsUsesLightTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 0)) != 0;
        }
        catch { return false; }
    }

    public bool EffectiveDarkTheme => !FollowSystemTheme ? DarkTheme : !WindowsUsesLightTheme();

    public Color AccentColor
    {
        get
        {
            try { return ColorTranslator.FromHtml(AccentHex); }
            catch { return Color.MediumPurple; }
        }
    }

    /// <summary>
    /// Integrates with the app's existing palette. True per-track colors must additionally
    /// be used by Track.DrawSamples, which isn't included in the current uploads.
    /// </summary>
    public void ApplyToAppColors()
    {
        bool dark = EffectiveDarkTheme;
        Settings.Color.WinBg = HighContrast ? (dark ? Color.Black : Color.White)
            : (dark ? Color.FromArgb(44, 49, 55) : Color.FromArgb(246, 247, 250));
        Settings.Color.WinFg = dark ? Color.White : Color.FromArgb(25, 28, 33);
        Settings.Color.TrackBg_Unselected = HighContrast ? (dark ? Color.Black : Color.White)
            : (dark ? Color.FromArgb(39, 41, 54) : Color.FromArgb(231, 234, 241));
        Settings.Color.TrackBg_Selected = HighContrast ? (dark ? Color.FromArgb(35, 35, 35) : Color.FromArgb(225, 225, 225))
            : (dark ? Color.FromArgb(62, 53, 88) : Color.FromArgb(222, 213, 247));
        Settings.Color.Playline = AccentColor;
        Settings.Color.TrackSamples_Enabled = HighContrast ? (dark ? Color.White : Color.Black) : AccentColor;
        Settings.Color.TrackSamples_Disabled = dark ? Color.Gray : Color.DarkGray;
    }
}
