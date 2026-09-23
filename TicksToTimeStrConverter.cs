using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace CCcut
{
    public class TicksToTimeStrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long ticks)
            {
                // 将 Ticks 转换为 TimeSpan
                TimeSpan time = TimeSpan.FromTicks(ticks);

                // 如果视频总时长超过 1 小时，显示 时:分:秒；否则只显示 分:秒
                //if (time.TotalHours >= 1)
                //{
                //    return time.ToString(@"hh\:mm\:ss");
                //}
                //return time.ToString(@"mm\:ss");

                return time.ToString(@"hh\:mm\:ss");
            }
            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
