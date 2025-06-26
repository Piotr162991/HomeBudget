using System;
using System.Globalization;
using System.Windows.Data;
using HouseBug.ViewModels;

namespace HouseBug.Resources.Converters
{
    public class CurrencyValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                string symbol = "zł";
                if (parameter is string currency && !string.IsNullOrEmpty(currency))
                    symbol = GetSymbolForCurrency(currency);
                return $"{amount:N2} {symbol}";
            }
            return "0,00";
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                if (parameter is string currency && !string.IsNullOrEmpty(currency))
                    strValue = strValue.Replace(GetSymbolForCurrency(currency), "").Trim();
                else
                    strValue = strValue.Replace("zł", "").Trim();
                if (decimal.TryParse(strValue, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal result))
                {
                    return result;
                }
            }
            return 0m;
        }
    }
}
