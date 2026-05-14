using System;
using System.Globalization;
using System.Windows.Data;

namespace AudioAnalyzer.Infrastructure
{
    public class ProgressWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return 0.0;
            
            // values[0] = AnalysisProgress (double)
            // values[1] = parent width (double)
            
            double progress = 0;
            double totalWidth = 0;

            if (values[0] is double d1) progress = d1;
            else if (values[0] is float f1) progress = f1;
            else if (values[0] is int i1) progress = i1;

            if (values[1] is double d2) totalWidth = d2;
            else if (values[1] is float f2) totalWidth = f2;
            
            if (totalWidth <= 0) return 0.0;
            
            double width = (progress / 100.0) * totalWidth;
            return Math.Max(0, width);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
