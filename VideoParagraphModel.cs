using System.ComponentModel;
using System.Windows.Media;

namespace CCcut
{
    public class VideoParagraphModel : INotifyPropertyChanged
    {
        public Color ColorHex { get; set; }


        private string _title = string.Empty;
        public string Title {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            } 
        }

        private long _startTicks;

        public long StartTicks
        {
            get => _startTicks;
            set { _startTicks = value; OnPropertyChanged(nameof(StartTicks)); OnPropertyChanged(nameof(DurationTicks)); }
        }

        private long _endTicks;

        public long EndTicks
        {
            get => _endTicks;
            set { _endTicks = value; OnPropertyChanged(nameof(EndTicks)); OnPropertyChanged(nameof(DurationTicks)); }
        }

        private int _zIndex = 1; // 默认层级都是 1
        public int ZIndex
        {
            get => _zIndex;
            set { _zIndex = value; OnPropertyChanged(nameof(ZIndex)); }
        }

        private bool _isSelect = true;
        public bool IsSelect
        {
            get => _isSelect;
            set { _isSelect = value; OnPropertyChanged(nameof(IsSelect)); }
        }

        // 辅助属性：让 Width 绑定更简单
        public long DurationTicks => EndTicks - StartTicks;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}