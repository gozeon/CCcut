using FlyleafLib;
using System.IO;
using System.Windows;

namespace CCcut
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                SettingsDto settings = SettingManager.Load<SettingsDto>();
                if (string.IsNullOrEmpty(settings.FFmpegBinPath) ||
                    !Directory.Exists(settings.FFmpegBinPath) ||
                    Directory.GetFiles(settings.FFmpegBinPath, "ffmpeg.exe").Length == 0)
                {
                    throw new DirectoryNotFoundException("ffmpeg.exe | ffprobe.exe 不存在");
                }
                if (string.IsNullOrEmpty(settings.FFmpegDllPath) || 
                    !Directory.Exists(settings.FFmpegDllPath) || 
                    Directory.GetFiles(settings.FFmpegDllPath, "*.dll").Length == 0)
                {
                    throw new DirectoryNotFoundException("ffmpeg dll 不存在");
                }
                var config = new EngineConfig()
                {
                    FFmpegPath = settings.FFmpegDllPath,
#if RELEASE
                FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Quiet,
                LogLevel = LogLevel.Quiet,

#else
                    FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Warn,
                    LogLevel = LogLevel.Debug,
                    LogOutput = ":debug",
                    //LogOutput         = ":console",
                    //LogOutput         = @"C:\Flyleaf\Logs\flyleaf.log",
#endif

                    UIRefresh = true,
                    // 16 = 60 fps
                    UIRefreshInterval = 1,

                };
                
                if(!string.IsNullOrEmpty(settings.PluginsPath))
                {
                    config.PluginsPath = settings.PluginsPath;
                }

                Engine.Start(config);
            }
            catch(Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"播放引擎初始化失败，请重新配置 FFmpeg 路径！\n提示: {ex.Message}",
                        "初始化向导",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning
                    );

                    var setupWindow = new SettingsWindow();
                    setupWindow.ShowDialog();

                    //Application.Current.Shutdown();
                    //Environment.Exit(0);
                    // 重启
                    MessageBoxResult result = System.Windows.MessageBox.Show(
                        "配置已保存！是否立即重启软件以应用新配置？\n\n[提示]：如果配置仍不正确，建议选择“否”以检查文件。",
                        "重启确认",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question
                    );
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // 用户选择“是”：拉起新进程并退出旧进程
                            string currentExePath = Environment.ProcessPath;
                            System.Diagnostics.Process.Start(currentExePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"自动重启失败: {ex.Message}");
                        }
                        finally
                        {
                            Environment.Exit(0); // 彻底杀掉当前旧进程
                        }
                    }
                    else
                    {
                        // 用户选择“否”：直接安全退出，打破无限循环，允许用户去检查本地文件
                        System.Diagnostics.Debug.WriteLine("用户拒绝了重启，程序安全退出。");
                        Environment.Exit(0);
                    }

                });

                return;
            }
            
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await SplitQueueService.Instance.StopAsync();
            base.OnExit(e);
        }
    }

}
