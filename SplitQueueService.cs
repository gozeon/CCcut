using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xabe.FFmpeg;

namespace CCcut
{
    internal class SplitQueueService : ObservableObject
    {
        private static readonly Lazy<SplitQueueService> _instance = new(() => new SplitQueueService());
        public static SplitQueueService Instance => _instance.Value;
        public ObservableCollection<SplitTask> TaskQueue { get; } = new();
        private readonly SemaphoreSlim _signal = new(0);
        private CancellationTokenSource? _loopCts;
        private bool _isLoopRunning = false;
        private int _waitingCount, _processingCount, _completedCount, _canceledCount, _failedCount;
        public int WaitingCount { get => _waitingCount; private set => SetProperty(ref _waitingCount, value); }
        public int ProcessingCount { get => _processingCount; private set => SetProperty(ref _processingCount, value); }
        public int CompletedCount { get => _completedCount; private set => SetProperty(ref _completedCount, value); }
        public int CanceledCount { get => _canceledCount; private set => SetProperty(ref _canceledCount, value); }
        public int FailedCount { get => _failedCount; private set => SetProperty(ref _failedCount, value); }
        public int FinishedCount => CompletedCount + CanceledCount + FailedCount;
        public int TotalCount => TaskQueue.Count;

        private SplitQueueService()
        {
            StartProcessingLoop();

            TaskQueue.CollectionChanged += TaskQueue_CollectionChanged;
        }

        private void TaskQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (SplitTask t in e.OldItems)
                {
                    t.PropertyChanged -= T_PropertyChanged;
                }
            }

            if (e.NewItems is not null)
            {
                foreach (SplitTask t in e.NewItems)
                {
                    t.PropertyChanged += T_PropertyChanged;
                }
            }

            Recalculate();
        }

        private void T_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SplitTask.Status))
            {
                Recalculate();
            }
        }

        private void Recalculate()
        {
            WaitingCount = TaskQueue.Count(t => t.Status == TaskStatus.Waiting);
            ProcessingCount = TaskQueue.Count(t => t.Status == TaskStatus.Processing);
            CompletedCount = TaskQueue.Count(t => t.Status == TaskStatus.Completed);
            CanceledCount = TaskQueue.Count(t => t.Status == TaskStatus.Canceled);
            FailedCount = TaskQueue.Count(t => t.Status == TaskStatus.Failed);

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(FinishedCount));
        }

        public bool AddTask(SplitTask task)
        {
            bool isDuplicate = false;

            App.Current.Dispatcher.Invoke(() =>
            {
                //isDuplicate = TaskQueue.Any(t => t.TaskKey == task.TaskKey && (t.Status == TaskStatus.Waiting || t.Status == TaskStatus.Processing));
                isDuplicate = TaskQueue.Any(t => t.TaskKey == task.TaskKey);

                if (!isDuplicate)
                {
                    TaskQueue.Add(task);
                }
            });

            if (isDuplicate)
            {
                return false;
            }

            _signal.Release(); // 唤醒后台消费线程
            return true;
        }

        public void CancelTask(string taskId)
        {
            var task = TaskQueue.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                if (task.Status == TaskStatus.Waiting)
                {
                    task.Status = TaskStatus.Canceled;
                    task.LogMessage = "已取消!";
                }
                else if (task.Status == TaskStatus.Processing)
                {
                    task.Cts.Cancel(); // 触发 FFmpeg 异步取消
                }
            }
        }

        private void StartProcessingLoop()
        {
            if (_isLoopRunning) return;
            _isLoopRunning = true;

            _loopCts = new CancellationTokenSource();
            var loopToken = _loopCts.Token;

            Task.Run(async () =>
            {
                while (!loopToken.IsCancellationRequested)
                {
                    try
                    {
                        await _signal.WaitAsync(loopToken);
                    }
                    catch (OperationCanceledException) { break; }

                    SplitTask? currentTask = null;
                    lock (TaskQueue)
                    {
                        currentTask = TaskQueue.FirstOrDefault(t => t.Status == TaskStatus.Waiting);
                    }

                    if (currentTask == null) continue;
                    await ProcessTaskAsync(currentTask, loopToken);
                }
            }, loopToken);
        }

        private async Task ProcessTaskAsync(SplitTask task, CancellationToken loopToken)
        {
            task.Status = TaskStatus.Processing;
            task.LogMessage = "开始拆分...";

            if (System.IO.File.Exists(task.OutputPath))
            {
                task.Status = TaskStatus.Completed;
                task.Progress = 100;
                task.LogMessage = "文件已存在，跳过";
                return;
            }

            // temp file
            string tempOutputPath = System.IO.Path.GetTempFileName() + System.IO.Path.GetExtension(task.FileName);

            // 同时监听两个取消事件
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(loopToken, task.Cts.Token);

            try
            {
                IConversion conversion = await FFmpeg.Conversions.FromSnippet.Split(
                    task.VideoPath,
                    tempOutputPath,
                    task.Start,
                    task.Duration
                );

                conversion.OnProgress += (s, args) =>
                {
                    task.Progress = args.Percent;
                    task.LogMessage = $"处理中: {args.Duration} / {args.TotalLength} ";
                };

                IConversionResult result = await conversion.Start(linked.Token);

                if (System.IO.File.Exists(tempOutputPath))
                {
                    System.IO.File.Move(tempOutputPath, task.OutputPath);
                }

                task.Status = TaskStatus.Completed;
                task.Progress = 100;
                task.LogMessage = "完成!";
            }
            catch (OperationCanceledException)
            {
                task.Status = TaskStatus.Canceled;
                task.LogMessage = "已取消!";
            }
            catch(Exception ex)
            {
                task.Status = TaskStatus.Failed;
                task.LogMessage = $"失败: {ex.Message}";
            }
            finally
            {
                TryDeleteFile(tempOutputPath);
            }
        }

        private void TryDeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"清理临时文件失败: {ex.Message}");
                }
            });
        }
        
        public void OpenTaskFile(string taskId)
        {
            var task = TaskQueue.FirstOrDefault(t => t.Id == taskId);
            if(task!=null && task.Status == TaskStatus.Completed)
            {
                try
                {
                    if(System.IO.File.Exists(task.OutputPath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(task.OutputPath)
                        {
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        task.LogMessage = "文件已被移动或删除!";
                    }
                }
                catch(Exception ex)
                {
                    System.Windows.MessageBox.Show($"无法打开文件: {ex.Message}");
                }
            }
        }

        public void DeleteTaskRecord(string taskId)
        {
            var task = TaskQueue.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                // 再次确保处理中的任务不会被意外删除
                if (task.Status == TaskStatus.Processing) return;

                App.Current.Dispatcher.Invoke(() =>
                {
                    TaskQueue.Remove(task);
                });
            }
        }
    
        public async Task StopAsync()
        {
            _loopCts?.Cancel();
            _signal.Release();

            foreach (var t in TaskQueue)
            {
                if (t.Status == TaskStatus.Waiting)
                {
                    t.Status = TaskStatus.Canceled;
                    t.LogMessage = "已取消!";
                }
            }
        }
    }
}
