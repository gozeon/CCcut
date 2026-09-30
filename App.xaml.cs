using FlyleafLib;
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

            Engine.Start(new EngineConfig()
            {
                FFmpegPath = @"C:\Users\admin\Downloads\Flyleaf_v3.11.9\FFmpeg",
                PluginsPath = @"C:\Users\admin\Downloads\Flyleaf_v3.11.9\Plugins",

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

            });
        }
    }

}
