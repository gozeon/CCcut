using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows.Media;

namespace CCcut
{
    public partial class VideoParagraphModel : ObservableObject
    {
        [ObservableProperty]
        public Color _colorHex;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private long _startTicks;

        [ObservableProperty]
        private long _endTicks;

        [ObservableProperty]
        private int _zIndex = 1; // 默认层级都是 1

        [ObservableProperty]
        private bool _isSelect = true;

        // 辅助属性：让 Width 绑定更简单
        public long DurationTicks => EndTicks - StartTicks;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if(e.PropertyName==nameof(StartTicks) || e.PropertyName == nameof(EndTicks))
            {
                OnPropertyChanged(nameof(DurationTicks));
            }
        }
    }
}