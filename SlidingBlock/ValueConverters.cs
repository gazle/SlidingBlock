using System;
using System.Windows.Data;
using System.Collections.Generic;
using System.Windows.Media;

namespace SlidingPuzzle
{
    public class CellToCanvasConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int cellPos = (int)values[0];
            //double multiplier = (double)values[1];
            double multiplier = 40.0;
            return cellPos * multiplier;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToBrushConverter : IValueConverter
    {
        static List<SolidColorBrush> brushes;

        static IntToBrushConverter()
        {
            brushes = new List<SolidColorBrush>() { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow, Brushes.Purple, Brushes.Olive,
                Brushes.CadetBlue, Brushes.DarkOrange, Brushes.Maroon, Brushes.Navy, Brushes.Orange, Brushes.DarkRed};
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int n = (int)value;
            return brushes[n % brushes.Count];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
