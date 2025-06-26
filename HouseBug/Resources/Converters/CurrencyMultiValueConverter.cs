using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using HouseBug.Services;

namespace HouseBug.Resources.Converters
{
    public class CurrencyMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 1 && values[0] is decimal amount)
            {
                // Pobierz walutę bezpośrednio z bazy
                string currency = GetCurrencyFromDb();
                string symbol = GetSymbolForCurrency(currency);
                return $"{amount:N2} {symbol}";
            }
            return "0,00";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string GetCurrencyFromDb()
        {
            try
            {
                using (var context = new BudgetContext())
                {
                    var settings = context.AppSettings.FirstOrDefault();
                    return settings?.DefaultCurrency ?? "PLN";
                }
            }
            catch
            {
                return "PLN";
            }
        }

        private string GetSymbolForCurrency(string currency)
        {
            switch (currency)
            {
                case "PLN": return "zł";
                case "USD": return "$";
                case "GBP": return "£";
                case "EUR": return "€";
                default: return currency;
            }
        }
    }
}
