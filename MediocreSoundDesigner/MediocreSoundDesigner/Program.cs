namespace MediocreSoundDesigner
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            OnboardingPreferences preferences = OnboardingPreferences.Load();
            if (!preferences.Completed)
            {
                using var wizard = new FrmOnboarding(preferences);
                if (wizard.ShowDialog() != DialogResult.OK)
                    return; // Closing setup before Finish does not start the editor.
            }

            preferences.ApplyToAppColors();
            Application.Run(new FrmMain());
        }
    }
}
