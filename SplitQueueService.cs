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
        private bool _isLoopRunning = false;

        private SplitQueueService()
        {
            StartProcessingLoop();
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
                    task.LogMessage = "已在队列中取消";
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

            Task.Run(async () =>
            {
                while (true)
                {
                    await _signal.WaitAsync();

                    SplitTask? currentTask = null;
                    lock (TaskQueue)
                    {
                        currentTask = TaskQueue.FirstOrDefault(t => t.Status == TaskStatus.Waiting);
                    }

                    if (currentTask == null) continue;
                    await ProcessTaskAsync(currentTask);
                }
            });
        }

        private async Task ProcessTaskAsync(SplitTask task)
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
            string tempOutputPath = task.OutputPath + "_tmp" + System.IO.Path.GetExtension(task.FileName);

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

                IConversionResult result = await conversion.Start(task.Cts.Token);

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
    }
}
