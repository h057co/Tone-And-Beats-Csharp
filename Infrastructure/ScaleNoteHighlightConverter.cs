using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AudioAnalyzer.Infrastructure;

public class ScaleNoteHighlightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool[] scaleNotes && parameter is string indexStr && int.TryParse(indexStr, out int index))
        {
            if (index >= 0 && index < scaleNotes.Length && scaleNotes[index])
            {
                // Try to get KeyForegroundBrush from resources, fallback to Magenta
                if (System.Windows.Application.Current?.Resources["KeyForegroundBrush"] is Brush brush)
                {
                    return brush;
                }
                return new SolidColorBrush(Color.FromRgb(255, 0, 255)); // Magenta fallback
            }
        }

        // Default color (Transparent or muted)
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
