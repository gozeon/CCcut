using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;

namespace CCcut
{
    internal partial class MainViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty]
        private Player _videoPlayer;

        [ObservableProperty]
        private string _videoPath;

        [ObservableProperty]
        private long _startTimeTicks;
        

        [ObservableProperty]
        private long _endTimeTicks;

        [ObservableProperty]
        private long _durationTicks;

        private bool _isSeekingLock = false;

        public MainViewModel()
        {
            Config playerConfig = new Config();
            //playerConfig.Video.BackColor = System.Windows.Media.Colors.White;
            playerConfig.Player.SeekAccurate = true;

            VideoPlayer = new Player(playerConfig);

            VideoPlayer.PropertyChanged += OnPlayerPropertyChanged;
        }

        private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isSeekingLock) return;

            switch (e.PropertyName)
            {
                case nameof(Player.Duration):
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DurationTicks = VideoPlayer.Duration;
                        if(EndTimeTicks == 0)
                        {
                            EndTimeTicks = VideoPlayer.Duration;
                        }
                    });
                    break;
                case nameof(Player.CurTime):
                    if(VideoPlayer.CurTime >= EndTimeTicks)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                _isSeekingLock = true;
                                VideoPlayer.Pause();
                                VideoPlayer.Seek((int)StartTimeTicks/10000);
                            }
                            finally
                            {
                                _isSeekingLock = false;
                            }
                        });
                    }
                    break;
            }
        }

        [RelayCommand]
        private void PlayFromStart()
        {
            if (VideoPlayer == null) return;

            VideoPlayer.Seek((int)StartTimeTicks/10000);
            VideoPlayer.Play();
        }

        [RelayCommand]
        private void RangeChanged()
        {
            if (VideoPlayer == null || _isSeekingLock) return;

            try
            {
                _isSeekingLock = true;

                if(VideoPlayer.CurTime < StartTimeTicks)
                {
                    VideoPlayer.Seek((int)StartTimeTicks/10000);
                }
                else if(VideoPlayer.CurTime>EndTimeTicks)
                {
                    VideoPlayer.Seek((int)EndTimeTicks/10000);
                }
            }
            finally
            {
                _isSeekingLock = false;
            }
        }

        [RelayCommand]
        private void OpenVideo()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "视频文件|*.mp4;*.mkv;*.avi;*.mov",
                Title = "选择视频文件",
                CheckFileExists = true,
            };

            if (openFileDialog.ShowDialog() == true)
            {
                VideoPath = openFileDialog.FileName;
                VideoPlayer.Open(VideoPath);
            }
        }


        public void Dispose()
        {
            VideoPlayer?.Dispose();
        }
    }
}
