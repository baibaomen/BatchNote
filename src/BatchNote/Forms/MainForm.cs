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

        private Panel _historyPanel;
        private FlowLayoutPanel _historyListPanel;
        private FlowLayoutPanel _entriesPanel;
        private Panel _toolbarPanel;
        private Label _statusLabel;
        private List<ScreenshotEntryControl> _entryControls;
        private List<HistoryItemControl> _historyItemControls;
        private PreviewForm _previewForm;

        private int _nextIndex = 1;
        private bool _hasUserEdits = false;  // 用户是否进行了人为编辑
        private HistoryItemControl _selectedHistoryItem = null;  // 当前选中的历史记录

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
            _historyItemControls = new List<HistoryItemControl>();

            _hotkeyService.HotkeyPressed += (s, e) => ToggleVisibility();
        }

        private void InitializeUI()
        {
            // === 左侧历史记录面板 ===
            _historyPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(0)
            };
            // 右侧边线
            _historyPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawLine(pen, _historyPanel.Width - 1, 0, _historyPanel.Width - 1, _historyPanel.Height);
                }
            };

            // 历史记录标题
            var historyTitle = new Label
            {
                Text = "📁 历史记录",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // 历史列表
            _historyListPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(8, 5, 8, 5),
                BackColor = Color.FromArgb(250, 250, 250)
            };

            _historyPanel.Controls.Add(_historyListPanel);
            _historyPanel.Controls.Add(historyTitle);

            // === 右侧条目列表面板 ===
            _entriesPanel = new DoubleBufferedFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(248, 248, 248)
            };
            _entriesPanel.AllowDrop = true;
            _entriesPanel.DragEnter += EntriesPanel_DragEnter;
            _entriesPanel.DragOver += EntriesPanel_DragOver;
            _entriesPanel.DragDrop += EntriesPanel_DragDrop;

            // === 底部工具栏 ===
            _toolbarPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            _toolbarPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(225, 225, 225), 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, _toolbarPanel.Width, 0);
                }
            };

            var btnAddText = CreateToolbarButton("+ 文本", 0);
            btnAddText.Click += (s, e) => AddTextEntry();

            var btnComposite = CreateToolbarButton("合成大图", 1);
            btnComposite.Click += (s, e) => CompositeAndCopy();

            var btnClear = CreateToolbarButton("清空", 2);
            btnClear.Click += (s, e) => ClearAllEntries();

            _toolbarPanel.Controls.Add(btnAddText);
            _toolbarPanel.Controls.Add(btnComposite);
            _toolbarPanel.Controls.Add(btnClear);

            // 状态提示标签
            _statusLabel = new Label
            {
                Text = "💡 Ctrl+V 粘贴截图 | 热键: Ctrl+Shift+B",
                AutoSize = true,
                Location = new Point(330, 16),
                ForeColor = Color.FromArgb(130, 130, 130),
                Font = new Font("Microsoft YaHei", 9)
            };
            _toolbarPanel.Controls.Add(_statusLabel);

            // 添加控件到窗体
            this.Controls.Add(_entriesPanel);
            this.Controls.Add(_historyPanel);
            this.Controls.Add(_toolbarPanel);

            // 窗口键盘事件
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // 加载历史记录
            LoadHistoryList();
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
                _statusLabel.Text = "💡 Ctrl+V 粘贴截图 | Ctrl+Shift+B";
            }
            else
            {
                _statusLabel.ForeColor = Color.FromArgb(0, 120, 180);
                _statusLabel.Text = $"📋 累计 {total} 条，选中 {selected} 条";
            }
        }

        /// <summary>
        /// 显示状态消息（持久显示，不自动重置）
        /// </summary>
        private void ShowStatus(string message, bool isSuccess = true)
        {
            int total = _entryControls.Count;
            int selected = _entryControls.Count(c => c.Entry.IsChecked);
            
            // 组合操作结果和当前统计
            string stats = total > 0 ? $" | {total}条/{selected}选中" : "";
            
            _statusLabel.ForeColor = isSuccess ? Color.FromArgb(0, 150, 80) : Color.FromArgb(200, 60, 60);
            _statusLabel.Text = message + stats;
        }

        private Button CreateToolbarButton(string text, int index)
        {
            var btn = new Button
            {
                Text = text,
                Width = 95,
                Height = 34,
                Location = new Point(10 + index * 102, 9),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 200),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Regular)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 145, 220);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 100, 175);
            return btn;
        }

        #region 历史记录面板

        /// <summary>
        /// 加载历史记录列表
        /// </summary>
        private void LoadHistoryList()
        {
            _historyListPanel.SuspendLayout();
            _historyListPanel.Visible = false;
            
            try
            {
                _historyListPanel.Controls.Clear();
                _historyItemControls.Clear();
                _selectedHistoryItem = null;

                var items = _historyService.GetHistoryList();

                foreach (var item in items)
                {
                    var control = new HistoryItemControl(item, _historyService);
                    control.Width = _historyListPanel.ClientSize.Width - 20;
                    control.Selected += HistoryItem_Selected;
                    
                    _historyItemControls.Add(control);
                    _historyListPanel.Controls.Add(control);
                }

                // 调整宽度
                _historyListPanel.SizeChanged += (s, e) =>
                {
                    foreach (var ctrl in _historyItemControls)
                    {
                        ctrl.Width = _historyListPanel.ClientSize.Width - 20;
                    }
                };

                if (items.Count == 0)
                {
                    var emptyLabel = new Label
                    {
                        Text = "暂无历史记录",
                        AutoSize = true,
                        ForeColor = Color.Gray,
                        Padding = new Padding(5)
                    };
                    _historyListPanel.Controls.Add(emptyLabel);
                }
            }
            finally
            {
                _historyListPanel.ResumeLayout(true);
                _historyListPanel.Visible = true;
            }
        }

        /// <summary>
        /// 在历史列表顶部添加一条新记录（用于合成后快速更新）
        /// </summary>
        private void AddHistoryItemAtTop(HistoryService.HistoryItem item)
        {
            // 移除空提示标签（如果有）
            foreach (Control ctrl in _historyListPanel.Controls)
            {
                if (ctrl is Label lbl && lbl.Text == "暂无历史记录")
                {
                    _historyListPanel.Controls.Remove(lbl);
                    lbl.Dispose();
                    break;
                }
            }

            var control = new HistoryItemControl(item, _historyService);
            control.Width = _historyListPanel.ClientSize.Width - 20;
            control.Selected += HistoryItem_Selected;
            
            _historyItemControls.Insert(0, control);
            _historyListPanel.Controls.Add(control);
            _historyListPanel.Controls.SetChildIndex(control, 0);  // 放到第一位
        }

        /// <summary>
        /// 历史记录项被选中
        /// </summary>
        private void HistoryItem_Selected(object sender, HistoryService.HistoryItem item)
        {
            var clickedControl = sender as HistoryItemControl;
            
            // 检查右侧是否有人为编辑的内容
            if (_hasUserEdits)
            {
                var result = MessageBox.Show(
                    "当前有编辑中的内容，加载历史记录将会替换。\n是否继续？",
                    "确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result != DialogResult.Yes)
                    return;
            }

            // 更新选中状态
            if (_selectedHistoryItem != null && _selectedHistoryItem != clickedControl)
            {
                _selectedHistoryItem.SetSelected(false);
            }
            _selectedHistoryItem = clickedControl;
            _selectedHistoryItem?.SetSelected(true);

            // 恢复历史条目
            var entries = _historyService.RestoreEntries(item);
            if (entries.Count > 0)
            {
                RestoreEntries(entries);
                _hasUserEdits = false;  // 加载历史记录后清除人为编辑标记
                ShowStatus($"✅ 已加载历史记录 ({entries.Count}条)", true);
            }
        }

        /// <summary>
        /// 恢复条目到工作区
        /// </summary>
        private void RestoreEntries(List<ScreenshotEntry> entries)
        {
            // 暂停布局更新并隐藏面板以减少闪烁
            _entriesPanel.Visible = false;
            _entriesPanel.SuspendLayout();
            
            try
            {
                // 清空当前条目
                foreach (var control in _entryControls.ToList())
                {
                    _entriesPanel.Controls.Remove(control);
                    control.Dispose();
                }
                _entryControls.Clear();

                // 预先创建所有控件
                var newControls = new List<ScreenshotEntryControl>();
                foreach (var entry in entries)
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
                    
                    newControls.Add(control);
                    _entryControls.Add(control);
                }

                // 一次性添加所有控件
                _entriesPanel.Controls.AddRange(newControls.ToArray());

                // 调整宽度
                _entriesPanel.SizeChanged += (s, e) =>
                {
                    foreach (var ctrl in newControls)
                    {
                        if (!ctrl.IsDisposed)
                            ctrl.Width = _entriesPanel.ClientSize.Width - 30;
                    }
                };

                _nextIndex = entries.Count + 1;
            }
            finally
            {
                // 恢复布局更新
                _entriesPanel.ResumeLayout(true);
                _entriesPanel.Visible = true;
            }
            
            UpdateStatus();
        }

        #endregion

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
            _hasUserEdits = true;  // 标记用户进行了编辑
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
            _hasUserEdits = true;  // 标记用户进行了编辑
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
            _hasUserEdits = true;  // 删除也是人为编辑
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
                _hasUserEdits = false;  // 清除人为编辑标记
                
                // 取消历史记录选中状态
                if (_selectedHistoryItem != null)
                {
                    _selectedHistoryItem.SetSelected(false);
                    _selectedHistoryItem = null;
                }
                
                // 清空后显示初始提示
                _statusLabel.ForeColor = Color.Gray;
                _statusLabel.Text = "🗑️ 数据已清空 | Ctrl+V 粘贴截图";
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
                var historyItem = _historyService.Save(compositeImage, allEntries);

                // 增量更新历史列表（而不是完全重新加载）
                if (historyItem != null)
                {
                    AddHistoryItemAtTop(historyItem);
                }
                
                // 清除人为编辑标记（已保存）
                _hasUserEdits = false;

                ShowStatus($"✅ 选中批注已生成大图，请到目标窗口 Ctrl+V 粘贴", true);

                compositeImage.Dispose();
            }
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

    /// <summary>
    /// 历史记录条目控件（简化版，用于左侧面板）
    /// </summary>
    internal class HistoryItemControl : UserControl
    {
        private readonly HistoryService.HistoryItem _item;
        private readonly HistoryService _historyService;
        private PictureBox _thumbnail;
        private Label _timeLabel;
        private Label _countLabel;

        public HistoryService.HistoryItem Item => _item;

        public event EventHandler<HistoryService.HistoryItem> Selected;
        
        private bool _isSelected = false;

        public HistoryItemControl(HistoryService.HistoryItem item, HistoryService historyService)
        {
            _item = item;
            _historyService = historyService;
            InitializeComponents();
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateSelectedStyle();
        }

        private void UpdateSelectedStyle()
        {
            if (_isSelected)
            {
                this.BackColor = Color.FromArgb(220, 235, 250);
            }
            else
            {
                this.BackColor = Color.White;
            }
            this.Invalidate();  // 触发重绘
        }

        private void InitializeComponents()
        {
            this.Height = 60;
            this.BackColor = Color.White;
            this.Margin = new Padding(0, 0, 0, 4);
            this.Cursor = Cursors.Hand;
            this.Padding = new Padding(5);

            // 绘制边框
            this.Paint += (s, e) =>
            {
                var borderColor = _isSelected 
                    ? Color.FromArgb(0, 120, 200)  // 选中时蓝色边框
                    : Color.FromArgb(230, 230, 230);  // 未选中时灰色边框
                var borderWidth = _isSelected ? 2 : 1;
                
                using (var pen = new Pen(borderColor, borderWidth))
                {
                    var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            // 缩略图
            _thumbnail = new PictureBox
            {
                Width = 48,
                Height = 48,
                Location = new Point(5, 6),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(248, 248, 248)
            };

            // 加载缩略图（使用预生成的缩略图，速度更快）
            try
            {
                _thumbnail.Image = _historyService.LoadThumbnail(_item.Id);
            }
            catch { }

            // 时间标签
            _timeLabel = new Label
            {
                Text = _item.CreatedAt.ToString("MM-dd HH:mm"),
                Location = new Point(58, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            // 条目数标签
            _countLabel = new Label
            {
                Text = $"{_item.EntryCount} 条",
                Location = new Point(58, 32),
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            this.Controls.Add(_thumbnail);
            this.Controls.Add(_timeLabel);
            this.Controls.Add(_countLabel);

            // 点击事件
            this.Click += (s, e) => Selected?.Invoke(this, _item);
            _thumbnail.Click += (s, e) => Selected?.Invoke(this, _item);
            _timeLabel.Click += (s, e) => Selected?.Invoke(this, _item);
            _countLabel.Click += (s, e) => Selected?.Invoke(this, _item);

            // 鼠标悬停效果
            this.MouseEnter += (s, e) => { if (!_isSelected) this.BackColor = Color.FromArgb(245, 250, 255); };
            this.MouseLeave += (s, e) => { if (!_isSelected) this.BackColor = Color.White; };
            _thumbnail.MouseEnter += (s, e) => { if (!_isSelected) this.BackColor = Color.FromArgb(245, 250, 255); };
            _thumbnail.MouseLeave += (s, e) => { if (!_isSelected) this.BackColor = Color.White; };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _thumbnail?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 支持双缓冲的 FlowLayoutPanel，减少重绘闪烁
    /// </summary>
    internal class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowLayoutPanel()
        {
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            this.UpdateStyles();
        }
    }
}
