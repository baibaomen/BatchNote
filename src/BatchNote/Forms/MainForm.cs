using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BatchNote.Controls;
using BatchNote.Models;
using BatchNote.Services;

namespace BatchNote.Forms
{
    public partial class MainForm : Form
    {
        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        private HotkeyService _hotkeyService;
        private CompositeService _compositeService;
        private HistoryService _historyService;

        private FlowLayoutPanel _entriesPanel;
        private Panel _toolbarPanel;
        private Label _statusLabel;
        private List<ScreenshotEntryControl> _entryControls;
        private PreviewForm _previewForm;

        private int _nextIndex = 1;

        public MainForm()
        {
            InitializeComponent();
            InitializeServices();
            InitializeUI();
            RegisterHotkey();
        }

        private void InitializeServices()
        {
            _hotkeyService = new HotkeyService();
            _compositeService = new CompositeService();
            _historyService = new HistoryService();
            _entryControls = new List<ScreenshotEntryControl>();

            _hotkeyService.HotkeyPressed += (s, e) => ToggleVisibility();
        }

        private void InitializeUI()
        {
            // 条目列表面板
            _entriesPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 245, 245)
            };
            _entriesPanel.AllowDrop = true;
            _entriesPanel.DragEnter += EntriesPanel_DragEnter;
            _entriesPanel.DragOver += EntriesPanel_DragOver;
            _entriesPanel.DragDrop += EntriesPanel_DragDrop;

            // 底部工具栏
            _toolbarPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(10, 10, 10, 10)
            };

            var btnAddText = CreateToolbarButton("+ 文本", 0);
            btnAddText.Click += (s, e) => AddTextEntry();

            var btnComposite = CreateToolbarButton("合成大图", 1);
            btnComposite.Click += (s, e) => CompositeAndCopy();

            var btnHistory = CreateToolbarButton("历史记录", 2);
            btnHistory.Click += (s, e) => ShowHistory();

            var btnClear = CreateToolbarButton("清空", 3);
            btnClear.Click += (s, e) => ClearAllEntries();

            _toolbarPanel.Controls.Add(btnAddText);
            _toolbarPanel.Controls.Add(btnComposite);
            _toolbarPanel.Controls.Add(btnHistory);
            _toolbarPanel.Controls.Add(btnClear);

            // 状态提示标签
            _statusLabel = new Label
            {
                Text = "💡 Ctrl+V 粘贴截图 | 热键: Ctrl+Shift+B",
                AutoSize = true,
                Location = new Point(420, 16),
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei", 9)
            };
            _toolbarPanel.Controls.Add(_statusLabel);

            this.Controls.Add(_entriesPanel);
            this.Controls.Add(_toolbarPanel);

            // 窗口键盘事件
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }

        /// <summary>
        /// 更新状态消息（显示当前条目状态）
        /// </summary>
        private void UpdateStatus()
        {
            int total = _entryControls.Count;
            int selected = _entryControls.Count(c => c.Entry.IsChecked);
            
            if (total == 0)
            {
                _statusLabel.ForeColor = Color.Gray;
                _statusLabel.Text = "💡 Ctrl+V 粘贴截图 | 热键: Ctrl+Shift+B";
            }
            else
            {
                _statusLabel.ForeColor = Color.FromArgb(0, 120, 180);
                _statusLabel.Text = $"📋 累计 {total} 条，选中 {selected} 条";
            }
        }

        /// <summary>
        /// 显示临时状态消息（操作反馈）
        /// </summary>
        private void ShowStatus(string message, bool isSuccess = true)
        {
            _statusLabel.ForeColor = isSuccess ? Color.Green : Color.Red;
            _statusLabel.Text = message;
        }

        private Button CreateToolbarButton(string text, int index)
        {
            var btn = new Button
            {
                Text = text,
                Width = 90,
                Height = 32,
                Location = new Point(10 + index * 100, 9),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Regular)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 151, 234);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 102, 184);
            return btn;
        }

        private void RegisterHotkey()
        {
            // 注册 Ctrl+Shift+B 热键
            var success = _hotkeyService.Register(
                this.Handle,
                HOTKEY_ID,
                Keys.B,
                MOD_CONTROL | MOD_SHIFT
            );

            if (!success)
            {
                MessageBox.Show(
                    "无法注册全局热键 Ctrl+Shift+B，可能与其他程序冲突。\n您仍可以正常使用程序，但无法通过热键呼出。",
                    "提示",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (_hotkeyService != null)
            {
                _hotkeyService.ProcessMessage(ref m);
            }
            base.WndProc(ref m);
        }

        /// <summary>
        /// 切换窗口可见性
        /// </summary>
        private void ToggleVisibility()
        {
            if (this.Visible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
                this.Activate();
                this.BringToFront();
            }
        }

        #region 条目管理

        /// <summary>
        /// 添加图片条目
        /// </summary>
        private void AddImageEntry(Bitmap image)
        {
            var entry = new ScreenshotEntry
            {
                Index = _nextIndex++,
                IsTextOnly = false,
                OriginalImage = image,
                IsChecked = true
            };

            AddEntryControl(entry);
            UpdateStatus();
        }

        /// <summary>
        /// 添加纯文本条目
        /// </summary>
        private void AddTextEntry()
        {
            var entry = new ScreenshotEntry
            {
                Index = _nextIndex++,
                IsTextOnly = true,
                IsChecked = true
            };

            AddEntryControl(entry);
            UpdateStatus();
        }

        /// <summary>
        /// 添加条目控件
        /// </summary>
        private void AddEntryControl(ScreenshotEntry entry)
        {
            var control = new ScreenshotEntryControl
            {
                Width = _entriesPanel.ClientSize.Width - 30,
                Entry = entry
            };

            control.DeleteRequested += (s, e) => RemoveEntryControl(control);
            control.ThumbnailClicked += (s, e) => ShowPreview(control);
            control.ThumbnailMouseEnter += (s, e) => ShowPreview(control);
            control.ThumbnailMouseLeave += (s, e) => HidePreview();
            control.CheckedChanged += (s, e) => UpdateStatus();

            _entryControls.Add(control);
            _entriesPanel.Controls.Add(control);

            // 调整宽度
            _entriesPanel.SizeChanged += (s, e) =>
            {
                control.Width = _entriesPanel.ClientSize.Width - 30;
            };

            // 聚焦到文本框
            control.FocusCommentBox();
        }

        /// <summary>
        /// 移除条目控件
        /// </summary>
        private void RemoveEntryControl(ScreenshotEntryControl control)
        {
            _entryControls.Remove(control);
            _entriesPanel.Controls.Remove(control);
            control.Dispose();

            // 更新编号
            UpdateAllIndexes();
            UpdateStatus();
        }

        /// <summary>
        /// 更新所有条目编号
        /// </summary>
        private void UpdateAllIndexes()
        {
            for (int i = 0; i < _entryControls.Count; i++)
            {
                _entryControls[i].UpdateIndex(i + 1);
            }
            _nextIndex = _entryControls.Count + 1;
        }

        /// <summary>
        /// 清空所有条目
        /// </summary>
        private void ClearAllEntries()
        {
            if (_entryControls.Count == 0) return;

            var result = MessageBox.Show(
                "确定要清空所有条目吗？",
                "确认清空",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                foreach (var control in _entryControls.ToList())
                {
                    _entriesPanel.Controls.Remove(control);
                    control.Dispose();
                }
                _entryControls.Clear();
                _nextIndex = 1;
                ShowStatus("🗑️ 数据已清空", true);
            }
        }

        #endregion

        #region 图片预览

        private void ShowPreview(ScreenshotEntryControl control)
        {
            if (control.Entry.IsTextOnly || control.Entry.OriginalImage == null)
                return;

            if (_previewForm == null || _previewForm.IsDisposed)
            {
                _previewForm = new PreviewForm();
                _previewForm.AnnotationChanged += (s, e) => control.Entry = control.Entry; // 触发重绘
            }

            _previewForm.SetEntry(control.Entry);
            _previewForm.Show();
        }

        private void HidePreview()
        {
            // 鼠标移出时不自动关闭，让用户可以在预览窗口上操作
        }

        #endregion

        #region 合成和复制

        /// <summary>
        /// 合成大图并复制到剪贴板
        /// </summary>
        private void CompositeAndCopy()
        {
            var allEntries = _entryControls.Select(c => c.Entry).ToList();
            var checkedEntries = allEntries.Where(e => e.IsChecked).ToList();

            if (checkedEntries.Count == 0)
            {
                ShowStatus("❌ 没有勾选的条目可以合成", false);
                return;
            }

            var compositeImage = _compositeService.Composite(allEntries);
            if (compositeImage != null)
            {
                // 复制到剪贴板
                Clipboard.SetImage(compositeImage);

                // 保存到历史（包含完整条目信息）
                _historyService.Save(compositeImage, allEntries);

                ShowStatus($"✅ 选中批注已生成大图，请到目标窗口 Ctrl+V 粘贴", true);

                compositeImage.Dispose();
            }
        }

        /// <summary>
        /// 显示历史记录窗口
        /// </summary>
        private void ShowHistory()
        {
            using (var historyForm = new HistoryForm(_historyService))
            {
                historyForm.RestoreRequested += HistoryForm_RestoreRequested;
                historyForm.ShowDialog(this);
            }
        }

        /// <summary>
        /// 从历史记录恢复条目
        /// </summary>
        private void HistoryForm_RestoreRequested(object sender, List<ScreenshotEntry> entries)
        {
            // 清空当前条目
            foreach (var control in _entryControls.ToList())
            {
                _entriesPanel.Controls.Remove(control);
                control.Dispose();
            }
            _entryControls.Clear();

            // 恢复历史条目
            foreach (var entry in entries)
            {
                AddEntryControl(entry);
            }

            _nextIndex = entries.Count + 1;
            UpdateStatus();
            ShowStatus("✅ 已从历史记录恢复，可继续编辑", true);
        }

        #endregion

        #region 键盘事件

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+V 粘贴
            if (e.Control && e.KeyCode == Keys.V)
            {
                PasteFromClipboard();
                e.Handled = true;
            }
            // Escape 隐藏窗口
            else if (e.KeyCode == Keys.Escape)
            {
                this.Hide();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 从剪贴板粘贴图片
        /// </summary>
        private void PasteFromClipboard()
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage() as Bitmap;
                if (image != null)
                {
                    AddImageEntry(image);
                }
            }
        }

        #endregion

        #region 拖拽排序

        private void EntriesPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ScreenshotEntryControl)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void EntriesPanel_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ScreenshotEntryControl)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void EntriesPanel_DragDrop(object sender, DragEventArgs e)
        {
            var draggedControl = e.Data.GetData(typeof(ScreenshotEntryControl)) as ScreenshotEntryControl;
            if (draggedControl == null) return;

            var point = _entriesPanel.PointToClient(new Point(e.X, e.Y));
            var targetControl = GetControlAtPoint(point);

            if (targetControl != null && targetControl != draggedControl)
            {
                int draggedIndex = _entryControls.IndexOf(draggedControl);
                int targetIndex = _entryControls.IndexOf(targetControl);

                // 重新排序
                _entryControls.Remove(draggedControl);
                _entryControls.Insert(targetIndex, draggedControl);

                // 重新添加控件
                _entriesPanel.Controls.Clear();
                foreach (var control in _entryControls)
                {
                    _entriesPanel.Controls.Add(control);
                }

                UpdateAllIndexes();
            }
        }

        private ScreenshotEntryControl GetControlAtPoint(Point point)
        {
            foreach (var control in _entryControls)
            {
                if (control.Bounds.Contains(point))
                {
                    return control;
                }
            }
            return null;
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 点击关闭按钮时隐藏而不是退出
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                _hotkeyService?.Dispose();
                _previewForm?.Dispose();
            }

            base.OnFormClosing(e);
        }
    }
}
