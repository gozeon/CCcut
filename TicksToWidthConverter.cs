using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace CCcut
{
    internal class TicksToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if(values.Length < 3 ||
                 values[0] == DependencyProperty.UnsetValue ||
                 values[1] == DependencyProperty.UnsetValue ||
                 values[2] == DependencyProperty.UnsetValue)
            {
                return 0.0;
            }

            try
            {
                long durationTicks = System.Convert.ToInt64(values[0]);
                long allTicks = System.Convert.ToInt64(values[1]);
                double actualWidth = System.Convert.ToDouble(values[2]);

                if(allTicks == 0)
                {
                    return 0.0;
                }

                double ratio = (double)durationTicks / allTicks;

               

                return ratio * actualWidth;
            }
            catch
            {
                return 0.0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
