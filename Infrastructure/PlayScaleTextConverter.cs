using System;
using System.Globalization;
using System.Windows.Data;

namespace AudioAnalyzer.Infrastructure;

public class PlayScaleTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isPlaying && isPlaying)
        {
            return "|| PAUSE";
        }
        return ">_ PLAY";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
