using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace CCcut
{
    internal class TicksToPixelConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 依次接收绑定的三个底层数据
            if (values.Length >= 3
                && values[0] is long currentTicks
                && values[1] is long totalTicks
                && values[2] is double canvasWidth)
            {
                if (totalTicks <= 0 || canvasWidth <= 0) return 0.0;

                // 核心数学映射：(Current / Total) * Canvas 实际宽度
                double ratio = (double)currentTicks / totalTicks;
                return ratio * canvasWidth;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
