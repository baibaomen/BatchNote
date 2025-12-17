using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BatchNote.Services
{
    /// <summary>
    /// 全局热键服务
    /// </summary>
    public class HotkeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// 热键被按下时触发
        /// </summary>
        public event EventHandler HotkeyPressed;

        private IntPtr _windowHandle;
        private int _hotkeyId;
        private bool _isRegistered;

        /// <summary>
        /// 注册全局热键
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <param name="id">热键 ID</param>
        /// <param name="key">按键</param>
        /// <param name="modifiers">修饰键（Alt=1, Ctrl=2, Shift=4, Win=8）</param>
        /// <returns>是否注册成功</returns>
        public bool Register(IntPtr windowHandle, int id, Keys key, uint modifiers)
        {
            _windowHandle = windowHandle;
            _hotkeyId = id;
            _isRegistered = RegisterHotKey(windowHandle, id, modifiers, (uint)key);
            return _isRegistered;
        }

        /// <summary>
        /// 注销全局热键
        /// </summary>
        public bool Unregister()
        {
            if (_isRegistered)
            {
                _isRegistered = !UnregisterHotKey(_windowHandle, _hotkeyId);
                return !_isRegistered;
            }
            return true;
        }

        /// <summary>
        /// 处理 Windows 消息，检测热键
        /// </summary>
        /// <param name="m">Windows 消息</param>
        /// <returns>是否处理了热键消息</returns>
        public bool ProcessMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _hotkeyId)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}
