using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AudioAnalyzer.Infrastructure;

public class TonicMarkerVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentTonicIndex && parameter is string indexStr && int.TryParse(indexStr, out int index))
        {
            return currentTonicIndex == index ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
