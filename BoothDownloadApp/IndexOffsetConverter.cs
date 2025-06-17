using System;
using System.Globalization;
using System.Windows.Data;

namespace BoothDownloadApp
{
    /// <summary>
    /// Converter that adds an offset to an index when converting and subtracts
    /// it when converting back. Used for mapping ComboBox SelectedIndex
    /// values when an additional "none" option is inserted.
    /// </summary>
    public class IndexOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int offset = System.Convert.ToInt32(parameter);
            int index = System.Convert.ToInt32(value);
            return index + offset;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int offset = System.Convert.ToInt32(parameter);
            int index = System.Convert.ToInt32(value);
            return index - offset;
        }
    }
}
