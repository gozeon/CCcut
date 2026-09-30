using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel VM => this.DataContext as MainViewModel;
        private bool _isMouseDown = false;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel(new WindowsFileDialogService());

            LogTextBox.TextChanged += (s, e) =>
            {
                LogTextBox.ScrollToEnd();
            };
        }

        private void ProgressBarCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return;

            _isMouseDown = true;
            ProgressBarCanvas.CaptureMouse(); // 锁定鼠标

            double mouseX = e.GetPosition(ProgressBarCanvas).X;
            double ratio = ProgressBarCanvas.ActualWidth > 0 ? mouseX / ProgressBarCanvas.ActualWidth : 0;

            VM.SeekByRatio(ratio); // 点击的瞬间视频画面和红线立刻跳过去

        }

        private void ProgressBarCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = false;
            ProgressBarCanvas.ReleaseMouseCapture();
        }

        private void ProgressBarCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (VM == null) return;

            // 1. 获取物理坐标
            double mouseX = e.GetPosition(ProgressBarCanvas).X;
            if (mouseX < 0) mouseX = 0;
            if (mouseX > ProgressBarCanvas.ActualWidth) mouseX = ProgressBarCanvas.ActualWidth;

            VM.HoverX = mouseX;

            if(_isMouseDown)
            {
                double ratio = ProgressBarCanvas.ActualWidth > 0 ? mouseX / ProgressBarCanvas.ActualWidth : 0;
                VM.SeekByRatio(ratio);
            }
        }

        private void ProgressBarCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseDown = false;
        }

        // 整体拖拽
        private void BodyThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            var item = thumb.DataContext as VideoParagraphModel;
            if (item == null) return;

            double canvasWidth = ProgressBarCanvas.ActualWidth;
            if (canvasWidth <= 0) return;

            //// 1. 先计算出如果没有边界限制，鼠标这一帧想把左边缘移到哪个像素点（预测左坐标）
            //long predictedLeft = item.Left + (long)e.HorizontalChange;

            //// 2. 根据当前的宽度，推算出预测的右边缘像素点
            //long predictedRight = predictedLeft + item.Width;

            //// 🛑 3. 全局边界双向拦截裁剪（统一裁剪 predictedLeft）

            //// 限制一：如果左边缘撞到了最左侧墙壁（< 0）
            //if (predictedLeft < 0)
            //{
            //    predictedLeft = 0;
            //}
            //// 限制二：如果右边缘撞到了最右侧墙壁（> 画布总宽度）
            //else if (predictedRight > canvasWidth)
            //{
            //    // 强行把左边缘卡在：总宽度 - 自身宽度 的绝对死位置上
            //    predictedLeft = (long)(canvasWidth - item.Width);
            //}

            //// 4. 最终一次性原子赋值，由于中间没有任何逻辑断层，UI 刷新绝对平滑
            //item.Left = predictedLeft;

            double ticksPerPixel = (double)VM.VideoPlayer.Duration / canvasWidth;
            long deltaTicks = (long)(e.HorizontalChange * ticksPerPixel);
            long predictedStart = item.StartTicks + deltaTicks;
            long predictedEnd = predictedStart + item.DurationTicks;

            if(predictedStart <0)
            {
                predictedStart = 0;
                predictedEnd = item.DurationTicks;
            }
            else if(predictedEnd>VM.VideoPlayer.Duration)
            {
                predictedStart = VM.VideoPlayer.Duration - item.DurationTicks;
                predictedEnd = VM.VideoPlayer.Duration;
            }
            item.StartTicks = predictedStart;
            item.EndTicks = predictedEnd;

            _isMouseDown = false;
        }

        // 左边缘扩展
        private void LeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            var item = thumb.DataContext as VideoParagraphModel;
            if (item == null) return;

            //var newLeft = item.Left + (long)e.HorizontalChange;

            //// 1. 【核心精髓】：先死死钉住右边缘当前的绝对像素位置
            //long originalRight = item.Left + item.Width;

            //// 2. 计算出如果没有任何限制，鼠标这一帧想把左边缘移到哪个像素点（预测左坐标）
            //long predictedLeft = item.Left + (long)e.HorizontalChange;

            //// 🛑 3. 边界裁剪限制（统一单向限制，绝不用 else 分流）

            //// 限制一：左边缘绝对不能往左滑出画布开头（< 0）
            //if (predictedLeft < 0)
            //{
            //    predictedLeft = 0;
            //}

            //// 限制二：防止向右拉过头把分段缩成了负数！左边缘绝对不能碰触或超过右边缘。
            //// 我们必须强行保留一个最小像素宽度（例如最低保留 10 像素）
            //long minWidthPixels = 10;
            //if (predictedLeft > originalRight - minWidthPixels)
            //{
            //    predictedLeft = originalRight - minWidthPixels;
            //}

            //// 4. 【乾坤定调】：根据裁剪后绝对安全的新 Left，反向推算出 Width
            //// 这样计算出来的 Width，可以百分之百保证：新 Left + 新 Width 永远完美等于 originalRight（右边死死钉住不动）
            //item.Left = predictedLeft;
            //item.Width = originalRight - predictedLeft;

            double canvasWidth = ProgressBarCanvas.ActualWidth;
            if (canvasWidth <= 0) return;

            double ticksPerPixel = (double)VM.VideoPlayer.Duration / canvasWidth;
            long deltaTicks = (long)(e.HorizontalChange * ticksPerPixel);

            long predictedStart = item.StartTicks + deltaTicks;
            if (predictedStart < 0)
            {
                predictedStart = 0;
            }
            long minTicks = TimeSpan.FromSeconds(5).Ticks;
            if(predictedStart > item.EndTicks-minTicks)
            {
                predictedStart = item.EndTicks - minTicks;
            }

            item.StartTicks = predictedStart;


            _isMouseDown = false;
        }

        // 右边缘扩展
        private void RightThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            var item = thumb.DataContext as VideoParagraphModel;
            if (item == null) return;

            //double canvasWidth = ProgressBarCanvas.ActualWidth;

            //// 1. 先计算出如果没有边界限制，鼠标这一帧想把它变多宽（预测宽度）
            //long predictedWidth = item.Width + (long)e.HorizontalChange;

            //// 2. 计算在这种预测宽度下，右边缘会达到多少像素
            //long predictedRight = item.Left + predictedWidth;

            //// 3. 【核心修正】：如果预测的右边缘超过了画布宽度，直接裁剪预测宽度
            //if (predictedRight > canvasWidth)
            //{
            //    predictedWidth = (long)(canvasWidth - item.Left);
            //}

            //// 4. 防止向左拉过头，限制一个最小像素宽度（例如最低保留 10 像素）
            //long minWidthPixels = 10;
            //if (predictedWidth < minWidthPixels)
            //{
            //    predictedWidth = minWidthPixels;
            //}

            //// 5. 统一单向赋值，没有 else 分流，彻底消除正负反馈死循环
            //item.Width = predictedWidth;

            double canvasWidth = ProgressBarCanvas.ActualWidth;
            if (canvasWidth <= 0) return;

            double ticksPerPixel = (double)VM.VideoPlayer.Duration / canvasWidth;
            long deltaTicks = (long)(e.HorizontalChange * ticksPerPixel);

            long predictedEnd = item.EndTicks + deltaTicks;
            if (predictedEnd > VM.VideoPlayer.Duration)
            {
                predictedEnd = VM.VideoPlayer.Duration;
            }

            long minTicks = TimeSpan.FromSeconds(5).Ticks;
            if (predictedEnd < item.StartTicks+minTicks)
            {
                predictedEnd = item.StartTicks + minTicks;
            }

            item.EndTicks = predictedEnd;

            _isMouseDown = false;
        }

        private void Thumb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 💡 1. 强行把外层 Canvas 的拖拽开关彻底关掉！
            // 这样即便事件等下冒泡传到了 Canvas，Canvas 的 MouseMove 也绝对不敢执行 Seek 逻辑！
            _isMouseDown = false;

            // 💡 2. 强行逼迫外层 Canvas 释放它通过 CaptureMouse() 抢占的操作系统鼠标锁
            ProgressBarCanvas.ReleaseMouseCapture();

            // 💡 3. 让当前的 Thumb 手柄强行接管最高优先级的鼠标控制权
            var thumb = sender as Thumb;
            if (thumb != null)
            {
                thumb.CaptureMouse();

                var currentItem = thumb.DataContext as VideoParagraphModel;
                if (currentItem != null && VM != null)
                {
                    VM.ActiveParagraph = currentItem;
                }
            }

            // ⚠️ 【核心关键】：绝对不要写 e.Handled = true; 
            // 这样才能保证 Thumb 内部能够顺利接收到按下信号，从而正常激活你的 DragDelta 拖拽算法！
        }
    }
}