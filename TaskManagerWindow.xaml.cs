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

        // 取消全部任务
        private void CancelAll_Click(object sender, RoutedEventArgs e)
        {
            SplitQueueService.Instance.CancelTasks();
        }

        // 一键清空任务记录
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "确定要清空所有任务记录吗？（处理中的任务不会被移除，请先取消）",
                "确认清空",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.OK) return;

            SplitQueueService.Instance.ClearTasks();
        }

        // 打开切片输出目录
        private void OpenOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            SplitQueueService.Instance.OpenOutputFolder();
        }

        // 表头全选 / 取消全选
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) return;

            bool selectAll = checkBox.IsChecked == true;
            foreach (var task in SplitQueueService.Instance.TaskQueue)
            {
                task.IsSelect = selectAll;
            }
        }
    }
}
