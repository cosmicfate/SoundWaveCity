namespace MediocreSoundDesigner;

internal static class Program
{
    // windows forms requires a single-threaded apartment for its ui components.
    [STAThread]
    static void Main()
    {
        // configure standard windows forms behavior before creating any windows.
        ApplicationConfiguration.Initialize();

        // load the previous choices before opening the editor
        // read the previous setup choices from the current user's local app data.
        var preferences = OnboardingPreferences.Load();

        // show the setup only when it has not been completed
        if (!preferences.Completed)
        {
            // do not open the editor until the initial setup is finished.
            using var onboarding = new FrmOnboarding(preferences);
            // closing or canceling setup exits instead of opening an unconfigured editor.
            if (onboarding.ShowDialog() != DialogResult.OK)
                return;
        }

        // apply the saved palette on every launch
        // apply the chosen palette before showing the main editor window.
        preferences.ApplyToAppColors();
        Application.Run(new FrmMain());
    }
}
