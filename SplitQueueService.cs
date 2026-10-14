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

        // 批量操作的目标：优先勾选的任务，未勾选任何任务时作用于全部
        private List<SplitTask> GetOperationTargets()
        {
            var checkedTasks = TaskQueue.Where(t => t.IsSelect).ToList();
            return checkedTasks.Count > 0 ? checkedTasks : TaskQueue.ToList();
        }

        // 批量取消：等待中的标记为已取消，处理中的触发取消
        public void CancelTasks()
        {
            foreach (var task in GetOperationTargets())
            {
                switch (task.Status)
                {
                    case TaskStatus.Waiting:
                        task.Status = TaskStatus.Canceled;
                        task.LogMessage = "已取消!";
                        break;
                    case TaskStatus.Processing:
                        task.Cts.Cancel(); // 触发 FFmpeg 异步取消
                        break;
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

            // 临时文件：用唯一名拼接扩展名，避免 GetTempFileName() 额外创建的空文件残留
            string tempOutputPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"{Guid.NewGuid():N}{System.IO.Path.GetExtension(task.FileName)}");

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
                    // 输出目录可能尚不存在，File.Move 会抛 DirectoryNotFoundException，这里先确保目录存在
                    string? outputDir = System.IO.Path.GetDirectoryName(task.OutputPath);
                    if (!string.IsNullOrEmpty(outputDir) && !System.IO.Directory.Exists(outputDir))
                    {
                        System.IO.Directory.CreateDirectory(outputDir);
                    }

                    // overwrite: true 兜底处理极端情况下目标已存在（如并发/重复文件）导致的 IOException
                    System.IO.File.Move(tempOutputPath, task.OutputPath, overwrite: true);
                }

                // 以最终产物是否存在为准：临时文件被清理或未生成时，不能标记为完成
                if (System.IO.File.Exists(task.OutputPath))
                {
                    task.Status = TaskStatus.Completed;
                    task.Progress = 100;
                    task.LogMessage = "完成!";
                }
                else
                {
                    task.Status = TaskStatus.Failed;
                    task.LogMessage = "失败: 未生成输出文件（临时文件可能已被清理）";
                }
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

        // 打开拆分结果所在的目录，并尽可能选中该文件
        public void OpenTaskFolder(string taskId)
        {
            var task = TaskQueue.FirstOrDefault(t => t.Id == taskId);
            if (task == null || task.Status != TaskStatus.Completed) return;

            try
            {
                string? folder = System.IO.Path.GetDirectoryName(task.OutputPath);
                if (string.IsNullOrEmpty(folder) || !System.IO.Directory.Exists(folder))
                {
                    task.LogMessage = "目录不存在!";
                    return;
                }

                if (System.IO.File.Exists(task.OutputPath))
                {
                    // 打开目录并选中输出文件
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{task.OutputPath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    // 文件已被移动或删除时，退化为直接打开所在目录
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder)
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"无法打开目录: {ex.Message}");
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

        // 打开切片输出根目录
        public void OpenOutputFolder()
        {
            try
            {
                var settings = SettingManager.Load<SettingsDto>();
                string folder = settings.FolderParagraphs;

                if (!System.IO.Directory.Exists(folder))
                {
                    System.IO.Directory.CreateDirectory(folder);
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"无法打开输出目录: {ex.Message}");
            }
        }

        // 一键清空：移除目标中非处理中的任务记录（处理中的任务需先取消）
        public void ClearTasks()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var task in GetOperationTargets().Where(t => t.Status != TaskStatus.Processing))
                {
                    TaskQueue.Remove(task);
                }
            });
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
