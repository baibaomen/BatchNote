using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BatchNote.Models;
using BatchNote.Services;

namespace BatchNote.Forms
{
    /// <summary>
    /// 历史记录查看窗口
    /// </summary>
    public class HistoryForm : Form
    {
        private FlowLayoutPanel _historyListPanel;
        private Panel _previewPanel;
        private PictureBox _previewBox;
        private Button _restoreButton;
        private Button _copyButton;
        private Button _deleteButton;
        private Button _openFolderButton;
        private Label _infoLabel;

        private readonly HistoryService _historyService;
        private HistoryService.HistoryItem _selectedItem;
        private List<HistoryItemControl> _itemControls;

        /// <summary>
        /// 恢复条目事件
        /// </summary>
        public event EventHandler<List<ScreenshotEntry>> RestoreRequested;

        public HistoryForm(HistoryService historyService)
        {
            _historyService = historyService;
            _itemControls = new List<HistoryItemControl>();
            InitializeComponents();
            LoadHistoryList();
        }

        private void InitializeComponents()
        {
            // 窗口属性
            this.Text = "📁 历史记录";
            this.Size = new Size(1100, 700);
            this.MinimumSize = new Size(900, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei", 9);

            // 左侧历史列表
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 360,
                BackColor = Color.FromArgb(248, 248, 248),
                Padding = new Padding(10)
            };

            var leftTitle = new Label
            {
                Text = "历史记录列表",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _historyListPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(248, 248, 248),
                Padding = new Padding(0, 5, 0, 0)
            };

            leftPanel.Controls.Add(_historyListPanel);
            leftPanel.Controls.Add(leftTitle);

            // 右侧预览区
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            // 工具栏
            var toolPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 5, 0, 10),
                BackColor = Color.Transparent
            };

            _restoreButton = CreateButton("📥 还原到工作区", Color.FromArgb(0, 122, 204));
            _restoreButton.Click += RestoreButton_Click;
            _restoreButton.Enabled = false;

            _copyButton = CreateButton("📋 复制到剪贴板", Color.FromArgb(90, 90, 90));
            _copyButton.Click += CopyButton_Click;
            _copyButton.Enabled = false;

            _openFolderButton = CreateButton("📂 打开文件夹", Color.FromArgb(90, 90, 90));
            _openFolderButton.Click += OpenFolderButton_Click;
            _openFolderButton.Enabled = false;

            _deleteButton = CreateButton("🗑️ 删除", Color.FromArgb(200, 60, 60));
            _deleteButton.Click += DeleteButton_Click;
            _deleteButton.Enabled = false;

            toolPanel.Controls.Add(_restoreButton);
            toolPanel.Controls.Add(_copyButton);
            toolPanel.Controls.Add(_openFolderButton);
            toolPanel.Controls.Add(_deleteButton);

            // 信息标签
            _infoLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                ForeColor = Color.Gray,
                Text = "选择一条历史记录查看详情"
            };

            // 预览区
            _previewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            _previewBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(0, 0)
            };
            _previewPanel.Controls.Add(_previewBox);

            rightPanel.Controls.Add(_previewPanel);
            rightPanel.Controls.Add(_infoLabel);
            rightPanel.Controls.Add(toolPanel);

            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);
        }

        private Button CreateButton(string text, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Width = 130,
                Height = 35,
                Margin = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei", 9)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        /// <summary>
        /// 加载历史列表
        /// </summary>
        private void LoadHistoryList()
        {
            _historyListPanel.Controls.Clear();
            _itemControls.Clear();

            var items = _historyService.GetHistoryList();

            foreach (var item in items)
            {
                var control = new HistoryItemControl(item, _historyService);
                control.Width = _historyListPanel.ClientSize.Width - 25;
                control.Selected += HistoryItem_Selected;
                
                _itemControls.Add(control);
                _historyListPanel.Controls.Add(control);
            }

            // 调整宽度
            _historyListPanel.SizeChanged += (s, e) =>
            {
                foreach (var ctrl in _itemControls)
                {
                    ctrl.Width = _historyListPanel.ClientSize.Width - 25;
                }
            };

            if (items.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "暂无历史记录",
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Padding = new Padding(10)
                };
                _historyListPanel.Controls.Add(emptyLabel);
            }
        }

        private void HistoryItem_Selected(object sender, HistoryService.HistoryItem item)
        {
            // 取消其他选中
            foreach (var ctrl in _itemControls)
            {
                ctrl.IsSelected = (ctrl.Item.Id == item.Id);
            }

            _selectedItem = item;
            UpdatePreview();
            UpdateButtons(true);
        }

        private void UpdatePreview()
        {
            if (_selectedItem == null) return;

            _previewBox.Image?.Dispose();
            _previewBox.Image = _historyService.LoadCompositeImage(_selectedItem.Id);

            _infoLabel.Text = $"📅 {_selectedItem.CreatedAt:yyyy-MM-dd HH:mm:ss}  |  📝 {_selectedItem.EntryCount} 个条目";
        }

        private void UpdateButtons(bool enabled)
        {
            _restoreButton.Enabled = enabled;
            _copyButton.Enabled = enabled;
            _openFolderButton.Enabled = enabled;
            _deleteButton.Enabled = enabled;
        }

        private void RestoreButton_Click(object sender, EventArgs e)
        {
            if (_selectedItem == null) return;

            var entries = _historyService.RestoreEntries(_selectedItem);
            if (entries.Count > 0)
            {
                RestoreRequested?.Invoke(this, entries);
                this.Close();
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            if (_selectedItem == null) return;

            var image = _historyService.LoadCompositeImage(_selectedItem.Id);
            if (image != null)
            {
                Clipboard.SetImage(image);
                MessageBox.Show("已复制到剪贴板！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OpenFolderButton_Click(object sender, EventArgs e)
        {
            if (_selectedItem == null) return;

            var path = _historyService.GetHistoryFolderPath(_selectedItem.Id);
            if (Directory.Exists(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (_selectedItem == null) return;

            var result = MessageBox.Show(
                "确定要删除这条历史记录吗？\n（将同时删除文件夹中的所有文件）",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _historyService.Delete(_selectedItem.Id);
                _selectedItem = null;
                _previewBox.Image?.Dispose();
                _previewBox.Image = null;
                _infoLabel.Text = "选择一条历史记录查看详情";
                UpdateButtons(false);
                LoadHistoryList();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _previewBox?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 历史记录条目控件
    /// </summary>
    internal class HistoryItemControl : UserControl
    {
        private readonly HistoryService.HistoryItem _item;
        private readonly HistoryService _historyService;
        private PictureBox _thumbnail;
        private Label _timeLabel;
        private Label _countLabel;
        private bool _isSelected;

        public HistoryService.HistoryItem Item => _item;

        public event EventHandler<HistoryService.HistoryItem> Selected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                this.BackColor = value ? Color.FromArgb(210, 230, 255) : Color.White;
            }
        }

        public HistoryItemControl(HistoryService.HistoryItem item, HistoryService historyService)
        {
            _item = item;
            _historyService = historyService;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Height = 80;
            this.BackColor = Color.White;
            this.Margin = new Padding(0, 0, 0, 5);
            this.Cursor = Cursors.Hand;
            this.Padding = new Padding(8);

            // 缩略图
            _thumbnail = new PictureBox
            {
                Width = 60,
                Height = 60,
                Location = new Point(8, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // 加载缩略图
            try
            {
                using (var img = _historyService.LoadCompositeImage(_item.Id))
                {
                    if (img != null)
                    {
                        _thumbnail.Image = CreateThumbnail(img, 60, 60);
                    }
                }
            }
            catch { }

            // 时间标签
            _timeLabel = new Label
            {
                Text = _item.CreatedAt.ToString("MM-dd HH:mm"),
                Location = new Point(75, 15),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50)
            };

            // 条目数标签
            _countLabel = new Label
            {
                Text = $"{_item.EntryCount} 个条目",
                Location = new Point(75, 42),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9),
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

        private Bitmap CreateThumbnail(Bitmap source, int maxWidth, int maxHeight)
        {
            float ratio = Math.Min((float)maxWidth / source.Width, (float)maxHeight / source.Height);
            int newWidth = (int)(source.Width * ratio);
            int newHeight = (int)(source.Height * ratio);

            var thumbnail = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(thumbnail))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(source, 0, 0, newWidth, newHeight);
            }
            return thumbnail;
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
}
