using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace SmartStandby.Converters;

public class InverseVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool invert = parameter?.ToString() == "invert";
        bool isEmpty = value is int count && count == 0;
        bool show = invert ? !isEmpty : isEmpty;
        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
