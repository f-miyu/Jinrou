using System;
using System.Globalization;
using JinrouClient.Domain;
using JinrouClient.Extensions;
using Xamarin.Forms;

namespace JinrouClient.Converters
{
    public class SideStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Side)value).ToName();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
