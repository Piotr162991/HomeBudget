using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using HouseBug.Services;

namespace HouseBug.Views.Dialogs
{
    public partial class ReportDialog : Window
    {
        private readonly ReportGenerator _reportGenerator;
        private readonly BudgetManager _budgetManager;

        public ReportDialog(ReportGenerator reportGenerator, BudgetManager budgetManager)
        {
            InitializeComponent();
            _reportGenerator = reportGenerator;
            _budgetManager = budgetManager;

            CurrentDate = DateTime.Now;
            MinDate = DateTime.Now.AddYears(-5);
            MaxDate = DateTime.Now;

            DataContext = this;
            Loaded += ReportDialog_Loaded;
        }

        private void ReportDialog_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateYearComboBox();

            ReportTypeComboBox.SelectionChanged += ReportTypeComboBox_SelectionChanged;

            if (ReportTypeComboBox.Items.Count > 0)
            {
                ReportTypeComboBox.SelectedIndex = 0;
                MonthlyOptionsPanel.Visibility = Visibility.Visible;
                YearlyOptionsPanel.Visibility = Visibility.Collapsed;
            }
        }

        public DateTime CurrentDate { get; set; }
        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }

        private void PopulateYearComboBox()
        {
            YearComboBox.Items.Clear();

            int currentYear = DateTime.Now.Year;
            for (int year = currentYear; year >= currentYear - 5; year--)
            {
                YearComboBox.Items.Add(year);
            }

            YearComboBox.SelectedItem = currentYear;
        }

        private void ReportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportTypeComboBox == null || ReportTypeComboBox.SelectedItem == null || 
                MonthlyOptionsPanel == null || YearlyOptionsPanel == null)
                return;

            ComboBoxItem selectedItem = (ComboBoxItem)ReportTypeComboBox.SelectedItem;
            if (selectedItem.Tag == null)
                return;

            string reportType = selectedItem.Tag.ToString();

            if (reportType == "Monthly")
            {
                MonthlyOptionsPanel.Visibility = Visibility.Visible;
                YearlyOptionsPanel.Visibility = Visibility.Collapsed;
            }
            else if (reportType == "Yearly")
            {
                MonthlyOptionsPanel.Visibility = Visibility.Collapsed;
                YearlyOptionsPanel.Visibility = Visibility.Visible;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxItem selectedItem = (ComboBoxItem)ReportTypeComboBox.SelectedItem;
            string reportType = selectedItem.Tag.ToString();

            if (reportType == "Monthly")
            {
                DateTime selectedMonth = MonthPicker.SelectedDate ?? DateTime.Now;
                var transactions = _budgetManager.GetTransactionsByMonth(selectedMonth);
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Title = "Zapisz raport",
                    Filter = "Plik tekstowy (*.txt)|*.txt|Plik CSV (*.csv)|*.csv|Wszystkie pliki (*.*)|*.*",
                    FileName = $"Raport_Miesięczny_{selectedMonth:yyyy_MM}"
                };
                if (sfd.ShowDialog() == true)
                {
                    if (sfd.FilterIndex == 2 || sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        _reportGenerator.ExportToCsvAsync(transactions, sfd.FileName).Wait();
                    }
                    else
                    {
                        string report = _reportGenerator.GenerateMonthlyReport(selectedMonth);
                        File.WriteAllText(sfd.FileName, report);
                    }
                    MessageBox.Show($"Raport został zapisany w pliku:\n{sfd.FileName}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            else if (reportType == "Yearly")
            {
                int selectedYear = (int)YearComboBox.SelectedItem;
                var yearlySummary = _budgetManager.GetYearlySummary(selectedYear).SelectMany(s => _budgetManager.GetTransactionsByMonth(s.Period)).ToList();
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Title = "Zapisz raport",
                    Filter = "Plik tekstowy (*.txt)|*.txt|Plik CSV (*.csv)|*.csv|Wszystkie pliki (*.*)|*.*",
                    FileName = $"Raport_Roczny_{selectedYear}"
                };
                if (sfd.ShowDialog() == true)
                {
                    if (sfd.FilterIndex == 2 || sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        _reportGenerator.ExportToCsvAsync(yearlySummary, sfd.FileName).Wait();
                    }
                    else
                    {
                        string report = _reportGenerator.GenerateYearlyReport(selectedYear);
                        File.WriteAllText(sfd.FileName, report);
                    }
                    MessageBox.Show($"Raport został zapisany w pliku:\n{sfd.FileName}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
        }

        private string GenerateSelectedReport()
        {
            ComboBoxItem selectedItem = (ComboBoxItem)ReportTypeComboBox.SelectedItem;
            string reportType = selectedItem.Tag.ToString();

            if (reportType == "Monthly")
            {
                DateTime selectedMonth = MonthPicker.SelectedDate ?? DateTime.Now;
                return _reportGenerator.GenerateMonthlyReport(selectedMonth);
            }
            else if (reportType == "Yearly")
            {
                int selectedYear = (int)YearComboBox.SelectedItem;
                return _reportGenerator.GenerateYearlyReport(selectedYear);
            }

            return null;
        }

    }
}
