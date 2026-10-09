using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace CCcut
{
    public enum TaskStatus
    {
        Waiting,
        Processing,
        Completed,
        Canceled,
        Failed
    }
    internal partial class SplitTask : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string VideoPath { get; set; } = string.Empty;
        public long SourceFileSize { get; set; }
        public DateTime SourceFileLastWriteTime { get; set; }
        public string OutputPath { get; set; } = string.Empty;
        public TimeSpan Start { get; set; }
        public TimeSpan Duration { get; set; }

        [ObservableProperty]
        private TaskStatus _status = TaskStatus.Waiting;

        [ObservableProperty]
        private int _progress;

        [ObservableProperty]
        private string _logMessage = "等待中...";

        // 用于取消单个任务
        public CancellationTokenSource Cts { get; set; } = new();

        public string TaskKey => $"{VideoPath}_{SourceFileSize}_{SourceFileLastWriteTime.Ticks}_{Start.Ticks}_{Duration.Ticks}";
    }
}
