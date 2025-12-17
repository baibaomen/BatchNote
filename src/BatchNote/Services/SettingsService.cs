using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace BatchNote.Services
{
    /// <summary>
    /// 应用程序设置服务
    /// </summary>
    public class SettingsService
    {
        private const string SettingsFileName = "settings.json";
        private const string AutoStartRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "BatchNote";

        private readonly string _settingsFilePath;
        private AppSettings _settings;

        /// <summary>
        /// 应用程序设置
        /// </summary>
        public class AppSettings
        {
            /// <summary>
            /// 热键主键（如 Keys.B）
            /// </summary>
            public Keys HotkeyKey { get; set; } = Keys.B;

            /// <summary>
            /// 热键修饰符（如 Control | Shift）
            /// </summary>
            public Keys HotkeyModifiers { get; set; } = Keys.Control | Keys.Shift;

            /// <summary>
            /// 是否开机自动启动
            /// </summary>
            public bool AutoStart { get; set; } = false;

            /// <summary>
            /// 是否在任务栏显示
            /// </summary>
            public bool ShowInTaskbar { get; set; } = false;

            /// <summary>
            /// 是否首次运行（用于判断启动时是否显示主窗口）
            /// </summary>
            public bool IsFirstRun { get; set; } = true;
        }

        public SettingsService()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataPath = Path.Combine(localAppData, "BatchNote");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _settingsFilePath = Path.Combine(appDataPath, SettingsFileName);
            Load();
        }

        /// <summary>
        /// 获取当前设置
        /// </summary>
        public AppSettings Settings => _settings;

        /// <summary>
        /// 加载设置
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _settings = new AppSettings();
                }
            }
            catch
            {
                _settings = new AppSettings();
            }

            // 同步注册表中的开机启动状态
            _settings.AutoStart = IsAutoStartEnabled();
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // 忽略保存错误
            }
        }

        /// <summary>
        /// 设置热键
        /// </summary>
        public void SetHotkey(Keys key, Keys modifiers)
        {
            _settings.HotkeyKey = key;
            _settings.HotkeyModifiers = modifiers;
            Save();
        }

        /// <summary>
        /// 设置开机自动启动
        /// </summary>
        public void SetAutoStart(bool enabled)
        {
            _settings.AutoStart = enabled;

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enabled)
                        {
                            var exePath = Application.ExecutablePath;
                            key.SetValue(AppName, $"\"{exePath}\"");
                        }
                        else
                        {
                            key.DeleteValue(AppName, false);
                        }
                    }
                }
            }
            catch
            {
                // 忽略注册表操作错误
            }

            Save();
        }

        /// <summary>
        /// 检查是否已设置开机启动
        /// </summary>
        public bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryKey, false))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(AppName);
                        return value != null;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// 设置任务栏显示
        /// </summary>
        public void SetShowInTaskbar(bool show)
        {
            _settings.ShowInTaskbar = show;
            Save();
        }

        /// <summary>
        /// 标记已完成首次运行
        /// </summary>
        public void MarkFirstRunComplete()
        {
            _settings.IsFirstRun = false;
            Save();
        }

        /// <summary>
        /// 将 Keys 修饰符转换为 HotkeyService 需要的 uint 格式
        /// </summary>
        public uint GetHotkeyModifiersAsUint()
        {
            uint modifiers = 0;
            if ((_settings.HotkeyModifiers & Keys.Control) == Keys.Control)
                modifiers |= 0x0002; // MOD_CONTROL
            if ((_settings.HotkeyModifiers & Keys.Shift) == Keys.Shift)
                modifiers |= 0x0004; // MOD_SHIFT
            if ((_settings.HotkeyModifiers & Keys.Alt) == Keys.Alt)
                modifiers |= 0x0001; // MOD_ALT
            return modifiers;
        }
    }
}
