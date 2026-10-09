using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Forms;

namespace CCcut
{
    partial class SettingsViewModel : ObservableObject, IDisposable
    {
        private readonly IFileDialogService _fileDialogService;

        [ObservableProperty]
        private SettingsDto _settings;

        public SettingsViewModel(IFileDialogService fileDialogService)
        {
            _fileDialogService = fileDialogService;
            try
            {
                Settings = SettingManager.Load<SettingsDto>();
            }
            catch
            {
                Settings = new SettingsDto();
            }
        }

        [RelayCommand]
        private void SelectFolder(string type)
        {

            string? folderPath = _fileDialogService.OpenFolder("请选择文件夹目录");
            if (!string.IsNullOrEmpty(folderPath))
            {
                switch (type)
                {
                    case "FFmpegBin":
                        Settings.FFmpegBinPath = folderPath;
                        break;
                    case "FFmpegDll":
                        Settings.FFmpegDllPath = folderPath;
                        break;
                    case "Plugins":
                        Settings.PluginsPath = folderPath;
                        break;
                    case "Recordings":
                        Settings.FolderRecordings = folderPath;
                        break;
                    case "Snapshots":
                        Settings.FolderSnapshots = folderPath;
                        break;
                    case "Paragraphs":
                        Settings.FolderParagraphs = folderPath;
                        break;
                }
            }
        }

        [RelayCommand]
        private void SaveSettings(Window window)
        {
            try
            {
                SettingManager.Save<SettingsDto>(Settings);

                if (window != null)
                {
                    // 设置为 true 会自动关闭窗口，并让外部的 ShowDialog() 接收到 true 返回值
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
                System.Windows.MessageBox.Show($"保存失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
        }
    }
}
