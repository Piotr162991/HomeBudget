using System;
using System.ComponentModel;
using System.Windows;
using HouseBug.ViewModels;

namespace HouseBug.Views
{
    public partial class CategoryWindow : Window
    {
        private CategoryViewModel ViewModel => (CategoryViewModel)DataContext;

        public CategoryWindow()
        {
            InitializeComponent();
            Loaded += CategoryWindow_Loaded;
        }

        private void CategoryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.ValidationMessage))
            {
                if (ViewModel.ValidationMessage?.Contains("została dodana") == true ||
                    ViewModel.ValidationMessage?.Contains("została zaktualizowana") == true)
                {
                    DialogResult = true;
                    Close();
                }
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            base.OnClosed(e);
        }
    }
}