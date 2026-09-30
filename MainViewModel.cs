using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Xabe.FFmpeg;

namespace CCcut
{
    public enum AlignDirection
    {
        Left,  // 对齐左边缘
        Right  // 对齐右边缘
    }

    internal partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly Random _random = new Random();
        private readonly IFileDialogService _fileDialogService;

        public ObservableCollection<VideoParagraphModel> Paragraphs { get; set; } = new ObservableCollection<VideoParagraphModel>();
        public VideoParagraphModel _activeParagraph;
        public VideoParagraphModel ActiveParagraph {
            get => _activeParagraph;
            set
            {
                if (_activeParagraph != value)
                {
                    _activeParagraph = value;
                    OnPropertyChanged();

                    UpdateParagraphZIndices(_activeParagraph);
                }
            }
        }

        private void UpdateParagraphZIndices(VideoParagraphModel activeParagraph)
        {
            if (Paragraphs == null) return;
            foreach (var p in Paragraphs)
            {
                p.ZIndex = 1;
            }

            if (activeParagraph != null)
            {
                activeParagraph.ZIndex = 99;
            }
        }

        [ObservableProperty]
        private string _playButtonText = "播放";

        [ObservableProperty]
        private string _logText = string.Empty;

        [ObservableProperty]
        private Player _videoPlayer;

        [ObservableProperty]
        private string _videoPath = string.Empty;

        [ObservableProperty]
        private long _curTime;

        [ObservableProperty]
        private double _hoverX;          // 鼠标悬停时的实时 X 像素坐标

        [ObservableProperty]
        private string _hoverTimeText = "00:00";  // 悬停气泡显示的时间文本

        public void SeekByRatio(double ratio)
        {
            if (VideoPlayer == null || VideoPlayer.Duration <= 0) return;
            //var msDuration = VideoPlayer.Duration / 10_000;
            //VideoPlayer.SeekAccurate((int)(ratio * msDuration));

            // need playerConfig.Player.SeekAccurate = true;
            VideoPlayer.CurTime = (long)(ratio * VideoPlayer.Duration);
        }

        public MainViewModel(IFileDialogService fileDialogService)
        {
            _fileDialogService = fileDialogService;

            Config playerConfig = new Config();
            //playerConfig.Video.BackColor = System.Windows.Media.Colors.White;
            playerConfig.Player.SeekAccurate = true;
            playerConfig.Player.Stats = true;
            playerConfig.Player.AutoPlay = false;
            playerConfig.Player.SeekOffset = TimeSpan.FromSeconds(1).Ticks;

            VideoPlayer = new Player(playerConfig);

            VideoPlayer.PropertyChanged += OnPlayerPropertyChanged;
        }

        private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            AppendLog($"OnPlayerPropertyChanged PropertyName: {e.PropertyName}");
            switch (e.PropertyName)
            {
                case nameof(Player.BufferedDuration):
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurTime = VideoPlayer.CurTime;
                    });
                    break;
                case nameof(Player.Duration):
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                    });
                    break;
                case nameof(Player.CurTime):
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurTime = VideoPlayer.CurTime;
                    });
                    break;
                case nameof(Player.Status):
                    PlayButtonText = VideoPlayer.IsPlaying ? "暂停" : "播放";
                    break;
            }
        }

        private void AppendLog(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogText += $"[{DateTime.Now:HH:mm:ss.fff}] {text.TrimEnd('\r', '\n') + Environment.NewLine}";
            });
        }
        
        [RelayCommand]
        private void OpenVideo()
        {
            string? filePath = _fileDialogService.OpenVideoFile("视频文件|*.mp4;*.mkv;*.avi;*.mov");
            if(!string.IsNullOrEmpty(filePath))
            {
                VideoPath = filePath;
                VideoPlayer.Open(VideoPath);
                AppendLog($"打开文件 {VideoPath}");
            }
        }

        [RelayCommand]
        private void AddParagraph()
        {
            var item = new VideoParagraphModel
            {
                Title = "段落 " + (Paragraphs.Count() + 1),
                StartTicks = VideoPlayer.CurTime,
                EndTicks = VideoPlayer.CurTime + TimeSpan.FromSeconds(10).Ticks,
                ColorHex = GetRandomLightColor(_random)
            };

            Paragraphs.Add(item);
            ActiveParagraph = item;
        }

        [RelayCommand]
        private void RemoveParagraph()
        {
            var selectItems = Paragraphs.Where(i => i.IsSelect).ToList();
            foreach(var item in selectItems)
            {
                Paragraphs.Remove(item);
            }
        }

        [RelayCommand]
        private async Task SplitParagraph()
        {
            var selectItems = Paragraphs.Where(i => i.IsSelect).ToList();
            foreach (var item in selectItems)
            {
                // 将时间字符串中的 ":" 和 "." 替换为 "-"，使其符合文件名规范
                string safeStart = TimeSpan.FromTicks(item.StartTicks).ToString(@"dd\.hhmmss\_fff");
                string safeEnd = TimeSpan.FromTicks(item.EndTicks).ToString(@"dd\.hhmmss\_fff");

                // 确保 Title 不为空且安全截取（防止不足3个字符时报错），并去掉非法文件名字符
                string safeTitle = string.IsNullOrEmpty(item.Title) ? "Untitled" : item.Title;
                safeTitle = string.Concat(safeTitle.Split(Path.GetInvalidFileNameChars())).Trim();
                if (safeTitle.Length > 3) safeTitle = safeTitle.Substring(0, 3).Trim();

                string fileName = $"{safeStart}-{safeEnd}_{safeTitle}{Path.GetExtension(VideoPath)}";
                string folderName = Path.GetFileNameWithoutExtension(VideoPath);
                string output = Path.Combine(VideoPlayer.Config.Player.FolderRecordings, folderName, fileName);
                if (Path.Exists(output))
                {
                    continue;
                }
                IConversion conversion = await FFmpeg.Conversions.FromSnippet.Split(VideoPath, output, TimeSpan.FromTicks(item.StartTicks), TimeSpan.FromTicks(item.DurationTicks));
                conversion.OnProgress += async (sender, args) =>
                {
                    //Show all output from FFmpeg to console
                    AppendLog($"{fileName} {args.Percent}%");
                };
                IConversionResult result = await conversion.Start();
                AppendLog($"{fileName} 完成!");
            }
        }

        public static Color GetRandomLightColor(Random random)
        {
            // 将 RGB 的随机下限严格锁死在 180 到 255 之间
            // 这样生成的颜色亮度极高，属于清爽的淡色系，搭配黑色字体非常高级
            byte r = (byte)random.Next(180, 256);
            byte g = (byte)random.Next(180, 256);
            byte b = (byte)random.Next(180, 256);

            return Color.FromArgb(255, r, g, b);
        }

        [RelayCommand]
        private void ExecuteAlignToCursor(AlignDirection direction)
        {
            if (ActiveParagraph == null || VideoPlayer == null) return;

            switch (direction)
            {
                case AlignDirection.Left:
                    if(ActiveParagraph.EndTicks - VideoPlayer.CurTime > TimeSpan.FromSeconds(5).Ticks)
                    {
                        ActiveParagraph.StartTicks = VideoPlayer.CurTime;
                    } 
                    else
                    {
                        var durationTicks = ActiveParagraph.DurationTicks;
                        ActiveParagraph.StartTicks = VideoPlayer.CurTime;
                        ActiveParagraph.EndTicks = Math.Min(VideoPlayer.CurTime + durationTicks, VideoPlayer.Duration);
                    }
                    break;
                case AlignDirection.Right:
                    if(VideoPlayer.CurTime - ActiveParagraph.StartTicks > TimeSpan.FromSeconds(5).Ticks)
                    {
                        ActiveParagraph.EndTicks = VideoPlayer.CurTime;
                    }
                    else
                    {
                        var durationTicks = ActiveParagraph.DurationTicks;
                        ActiveParagraph.EndTicks = VideoPlayer.CurTime;
                        ActiveParagraph.StartTicks = Math.Max(0, ActiveParagraph.EndTicks - durationTicks);
                    }
                    break;
            }
        }


        public void Dispose()
        {
            VideoPlayer?.Dispose();
        }
    }
}
