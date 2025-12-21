using System;
using System.Drawing;
using System.Windows.Forms;

namespace BatchNote.Forms
{
    /// <summary>
    /// 热键设置对话框
    /// </summary>
    public class HotkeySettingsForm : Form
    {
        private Label _instructionLabel;
        private TextBox _hotkeyDisplay;
        private Button _okButton;
        private Button _cancelButton;
        private Button _resetButton;

        private Keys _currentKey = Keys.None;
        private Keys _currentModifiers = Keys.None;
        private bool _isRecording = false;

        /// <summary>
        /// 选择的主键
        /// </summary>
        public Keys SelectedKey => _currentKey;

        /// <summary>
        /// 选择的修饰符
        /// </summary>
        public Keys SelectedModifiers => _currentModifiers;

        public HotkeySettingsForm(Keys currentKey, Keys currentModifiers)
        {
            _currentKey = currentKey;
            _currentModifiers = currentModifiers;
            InitializeComponents();
            UpdateHotkeyDisplay();
        }

        private void InitializeComponents()
        {
            // 窗口属性
            this.Text = "设置全局热键";
            this.Size = new Size(400, 230);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Microsoft YaHei", 9);
            this.KeyPreview = true;
            
            // 当前热键标签
            var currentLabel = new Label
            {
                Text = $"当前热键: {GetHotkeyText(_currentKey, _currentModifiers)}",
                Location = new Point(20, 15),
                Size = new Size(350, 20),
                ForeColor = Color.FromArgb(0, 120, 180),
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold)
            };

            // 说明标签
            _instructionLabel = new Label
            {
                Text = "点击下方输入框，然后按下您想要的快捷键组合：\n（需要包含 Ctrl、Alt、Shift 中至少一个修饰键）",
                Location = new Point(20, 45),
                Size = new Size(350, 40),
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            // 热键显示框
            _hotkeyDisplay = new TextBox
            {
                Location = new Point(20, 95),
                Size = new Size(350, 30),
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(0, 100, 180)
            };
            _hotkeyDisplay.Enter += HotkeyDisplay_Enter;
            _hotkeyDisplay.Leave += HotkeyDisplay_Leave;

            // 确定按钮
            _okButton = new Button
            {
                Text = "确定",
                Location = new Point(115, 145),
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 200),
                ForeColor = Color.White
            };
            _okButton.FlatAppearance.BorderSize = 0;
            _okButton.Click += (s, e) =>
            {
                if (_currentKey != Keys.None && _currentModifiers != Keys.None)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("请设置有效的快捷键组合！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            
            this.Controls.Add(currentLabel);

            // 重置按钮
            _resetButton = new Button
            {
                Text = "重置默认",
                Location = new Point(205, 145),
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White
            };
            _resetButton.FlatAppearance.BorderSize = 0;
            _resetButton.Click += (s, e) =>
            {
                _currentKey = Keys.V;
                _currentModifiers = Keys.Alt;
                UpdateHotkeyDisplay();
            };

            // 取消按钮
            _cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(295, 145),
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 180, 180),
                ForeColor = Color.White
            };
            _cancelButton.FlatAppearance.BorderSize = 0;
            _cancelButton.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(_instructionLabel);
            this.Controls.Add(_hotkeyDisplay);
            this.Controls.Add(_okButton);
            this.Controls.Add(_resetButton);
            this.Controls.Add(_cancelButton);

            this.KeyDown += HotkeySettingsForm_KeyDown;
        }

        private void HotkeyDisplay_Enter(object sender, EventArgs e)
        {
            _isRecording = true;
            _hotkeyDisplay.BackColor = Color.FromArgb(255, 255, 230);
            _hotkeyDisplay.Text = "按下快捷键...";
        }

        private void HotkeyDisplay_Leave(object sender, EventArgs e)
        {
            _isRecording = false;
            _hotkeyDisplay.BackColor = Color.White;
            UpdateHotkeyDisplay();
        }

        private void HotkeySettingsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_isRecording) return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            // 获取修饰键
            Keys modifiers = Keys.None;
            if (e.Control) modifiers |= Keys.Control;
            if (e.Shift) modifiers |= Keys.Shift;
            if (e.Alt) modifiers |= Keys.Alt;

            // 获取主键（排除单独的修饰键）
            Keys key = e.KeyCode;
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu ||
                key == Keys.LControlKey || key == Keys.RControlKey ||
                key == Keys.LShiftKey || key == Keys.RShiftKey ||
                key == Keys.LMenu || key == Keys.RMenu)
            {
                // 仅按下修饰键，显示当前状态
                _hotkeyDisplay.Text = GetModifiersText(modifiers) + "...";
                return;
            }

            // 必须包含至少一个修饰键
            if (modifiers == Keys.None)
            {
                _hotkeyDisplay.Text = "需要修饰键 (Ctrl/Alt/Shift)";
                return;
            }

            // 记录热键
            _currentKey = key;
            _currentModifiers = modifiers;
            UpdateHotkeyDisplay();

            // 自动移出焦点
            _isRecording = false;
            _hotkeyDisplay.BackColor = Color.White;
            _okButton.Focus();
        }

        private void UpdateHotkeyDisplay()
        {
            if (_currentKey == Keys.None)
            {
                _hotkeyDisplay.Text = "未设置";
            }
            else
            {
                _hotkeyDisplay.Text = GetModifiersText(_currentModifiers) + _currentKey.ToString();
            }
        }

        private string GetModifiersText(Keys modifiers)
        {
            string text = "";
            if ((modifiers & Keys.Control) == Keys.Control) text += "Ctrl + ";
            if ((modifiers & Keys.Alt) == Keys.Alt) text += "Alt + ";
            if ((modifiers & Keys.Shift) == Keys.Shift) text += "Shift + ";
            return text;
        }
        
        private string GetHotkeyText(Keys key, Keys modifiers)
        {
            if (key == Keys.None)
            {
                return "未设置";
            }
            return GetModifiersText(modifiers) + key.ToString();
        }
    }
}
