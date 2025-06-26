using System;
using System.Globalization;
using System.Windows.Data;
using HouseBug.ViewModels;

namespace HouseBug.Resources.Converters
{
    public class MoneyFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                return amount.ToString("N2");
            }
            return "0,00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                strValue = strValue.Replace(" ", "")
                                 .Replace("zł", "")
                                 .Replace("$", "")
                                 .Replace("£", "")
                                 .Trim();

                if (decimal.TryParse(strValue, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal result))
                {
                    return result;
                }
            }
            return 0m;
        }
    }
}
