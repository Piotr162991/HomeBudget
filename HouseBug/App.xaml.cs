using System;
using System.Windows;
using HouseBug.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HouseBug
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<IDialogService, DialogService>();
            services.AddTransient<BudgetManager>();
            services.AddTransient<ReportGenerator>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                await DatabaseInitializer.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas inicjalizacji bazy danych: {ex.Message}", 
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        public static T GetService<T>()
        {
            return ((App)Current)._serviceProvider.GetRequiredService<T>();
        }
    }
}