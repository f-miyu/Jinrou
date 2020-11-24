using System;
using System.Globalization;
using JinrouClient.Models;
using Xamarin.Forms;

namespace JinrouClient.Converters
{
    public class ActionTypeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (ActionType)value switch
            {
                ActionType.None => "",
                ActionType.Confirm => "確認",
                ActionType.Abstention => "棄権する",
                ActionType.Vote => "投票する",
                ActionType.Kill => "殺害する",
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
