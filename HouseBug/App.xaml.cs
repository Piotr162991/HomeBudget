using System;
using System.Windows;
using HouseBug.Services;

namespace HouseBug
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                DatabaseInitializer.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas inicjalizacji aplikacji: {ex.Message}",
                    "Błąd krytyczny",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}