using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace CCcut
{
    public class TickItem
    {
        public double Ratio { get; set; }
        public string Lablel { get; set; }
        public double TickHeight { get; set; }
    }
    internal partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly IFileDialogService _fileDialogService;

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

        public ObservableCollection<TickItem> Ticks { get; } = new ObservableCollection<TickItem>();


        public MainViewModel(IFileDialogService fileDialogService)
        {
            _fileDialogService = fileDialogService;

            Config playerConfig = new Config();
            //playerConfig.Video.BackColor = System.Windows.Media.Colors.White;
            //playerConfig.Player.SeekAccurate = true;
            playerConfig.Player.Stats = true;
            //playerConfig.Player.AutoPlay = false;

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
                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    DurationTicks = VideoPlayer.Duration;
                    //    if(EndTimeTicks == 0)
                    //    {
                    //        EndTimeTicks = VideoPlayer.Duration;
                    //    }
                    //});
                    break;
                case nameof(Player.CurTime):
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurTime = VideoPlayer.CurTime;
                    });
                    //if(VideoPlayer.CurTime >= EndTimeTicks)
                    //{
                    //    Application.Current.Dispatcher.Invoke(() =>
                    //    {
                    //        try
                    //        {
                    //            _isSeekingLock = true;
                    //            VideoPlayer.Pause();
                    //            VideoPlayer.Seek((int)StartTimeTicks/10000);
                    //        }
                    //        finally
                    //        {
                    //            _isSeekingLock = false;
                    //        }
                    //    });
                    //}
                    break;
                case nameof(Player.Status):
                    PlayButtonText = VideoPlayer.Status == Status.Playing ? "暂停" : "播放";
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


        public void Dispose()
        {
            VideoPlayer?.Dispose();
        }
    }
}
