using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CCcut
{
    /// <summary>
    /// TaskManagerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TaskManagerWindow : Window
    {
        public TaskManagerWindow()
        {
            InitializeComponent();
            
            this.DataContext = SplitQueueService.Instance;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string taskId)
            {
                SplitQueueService.Instance.CancelTask(taskId);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string taskId)
            {
                SplitQueueService.Instance.OpenTaskFile(taskId);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string taskId)
            {
                // 强制清除当前界面的焦点，使单元格安全释放
                Keyboard.ClearFocus();

                SplitQueueService.Instance.DeleteTaskRecord(taskId);
            }
        }

        // 打开拆分结果所在的目录
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string taskId)
            {
                SplitQueueService.Instance.OpenTaskFolder(taskId);
            }
        }
    }
}
