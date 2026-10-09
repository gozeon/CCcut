using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CCcut
{
    internal class SettingManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        //缩进
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            // 确保即使有只读的框架属性，也不会被序列化进去
            IgnoreReadOnlyProperties = true
        };

        public static T Load<T>() where T : new()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    T defaultConfig = new T();
                    Save(defaultConfig);
                    return defaultConfig;
                }

                string jsonStr = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<T>(jsonStr, Options) ?? throw new JsonException("配置文件内容解析失败");
            }
            catch
            {
                throw;
            }
        }

        public static void Save<T>(T config) where T : new()
        {
            try
            {
                string jsonStr = JsonSerializer.Serialize<T>(config, Options);
                File.WriteAllText(ConfigPath, jsonStr);
            }
            catch
            {
                throw;
            }
        }
    }

    partial class SettingsDto : ObservableObject
    {
        [ObservableProperty]
        private string _fFmpegBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpegBin");

        [ObservableProperty]
        private string _fFmpegDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpegDll");

        [ObservableProperty]
        public string _pluginsPath = string.Empty;

        [ObservableProperty]
        public string _folderRecordings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recordings");

        [ObservableProperty]
        public string _folderSnapshots = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Snapshots");

        [ObservableProperty]
        public string _folderParagraphs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Paragraphs");

        public string SnapshotFormat = "png";
    }
}