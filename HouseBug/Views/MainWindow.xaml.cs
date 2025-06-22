using System.Windows;
using HouseBug.Views.Dialogs;

namespace HouseBug.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CategoriesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var categoryWindow = new CategoryWindow
            {
                Owner = this
            };
            categoryWindow.ShowDialog();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var aboutDialog = new AboutDialog
            {
                Owner = this
            };
            aboutDialog.ShowDialog();
        }
    }
}