using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;

namespace SmartStandby.Converters;

public class DrainRateColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double rate)
        {
            if (rate > 5.0) return new SolidColorBrush(Colors.Red);
            if (rate > 2.0) return new SolidColorBrush(Colors.Orange);
            return new SolidColorBrush(Colors.Gray); // Healthy/Default
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
