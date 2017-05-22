using System;
using System.Globalization;
using System.Windows.Data;

namespace FirstFloor.ModernUI.Windows.Converters {
    [ValueConversion(typeof(double), typeof(double))]
    public class SliderRoundConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            var precision = parameter.AsDouble();
            if (Equals(precision, 0d)) precision = 1d;
            return Math.Round(value.AsDouble() / precision) * precision;
        }
    }
}