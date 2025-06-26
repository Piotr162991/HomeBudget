using System.Windows;
using HouseBug.Models;

namespace HouseBug.Views.Dialogs
{
    public partial class AppSettingsDialog : Window
    {
        public AppSettings AppSettings { get; private set; }
        public bool IsSaved { get; private set; }

        public AppSettingsDialog(AppSettings settings)
        {
            InitializeComponent();
            AppSettings = settings;
            DataContext = AppSettings;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = true;
            DialogResult = true;
            Close();
        }
    }
}

