using System;
using System.Drawing;
using System.Windows.Forms;
using BatchNote.Models;

namespace BatchNote.Controls
{
    /// <summary>
    /// 截图条目自定义控件
    /// </summary>
    public class ScreenshotEntryControl : UserControl
    {
        private const int ThumbnailSize = 120;
        private const int ControlMinHeight = 110;
        private const int DragHandleWidth = 20;
        private const int CheckBoxWidth = 40;
        private const int DeleteButtonWidth = 50;

        private CheckBox _checkBox;
        private Label _indexLabel;
        private PictureBox _thumbnail;
        private TextBox _commentTextBox;
        private Button _deleteButton;
        private Panel _dragHandle;

        private ScreenshotEntry _entry;
        private bool _isDragging;
        private Point _dragStartPoint;

        /// <summary>
        /// 关联的截图条目数据
        /// </summary>
        public ScreenshotEntry Entry
        {
            get => _entry;
            set
            {
                _entry = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// 删除按钮点击事件
        /// </summary>
        public event EventHandler DeleteRequested;

        /// <summary>
        /// 缩略图点击事件（用于触发预览）
        /// </summary>
        public event EventHandler ThumbnailClicked;

        /// <summary>
        /// 鼠标进入缩略图事件
        /// </summary>
        public event EventHandler ThumbnailMouseEnter;

        /// <summary>
        /// 鼠标离开缩略图事件
        /// </summary>
        public event EventHandler ThumbnailMouseLeave;

        /// <summary>
        /// 文本变更事件
        /// </summary>
        public event EventHandler CommentChanged;

        /// <summary>
        /// 勾选状态变更事件
        /// </summary>
        public event EventHandler CheckedChanged;

        public ScreenshotEntryControl()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // 设置控件属性
            this.Height = ControlMinHeight;
            this.AutoSize = false;
            this.Padding = new Padding(5);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.None;
            this.Margin = new Padding(0, 0, 0, 8);

            // 拖拽手柄区域 (加宽到20)
            _dragHandle = new Panel
            {
                Width = DragHandleWidth,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(230, 230, 230),
                Cursor = Cursors.SizeAll
            };
            _dragHandle.MouseDown += DragHandle_MouseDown;
            _dragHandle.MouseMove += DragHandle_MouseMove;
            _dragHandle.MouseUp += DragHandle_MouseUp;
            // 添加拖动图标提示
            var dragLabel = new Label
            {
                Text = "⋮⋮",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            dragLabel.MouseDown += DragHandle_MouseDown;
            dragLabel.MouseMove += DragHandle_MouseMove;
            dragLabel.MouseUp += DragHandle_MouseUp;
            _dragHandle.Controls.Add(dragLabel);

            // 勾选框 (移到缩略图左侧，加宽到40)
            _checkBox = new CheckBox
            {
                Width = CheckBoxWidth,
                Height = ControlMinHeight - 10,
                Checked = true,
                Location = new Point(DragHandleWidth + 5, 5),
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "✓",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 80)
            };
            _checkBox.FlatAppearance.BorderSize = 0;
            _checkBox.FlatAppearance.CheckedBackColor = Color.FromArgb(230, 250, 235);
            _checkBox.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 245, 255);
            _checkBox.CheckedChanged += (s, e) =>
            {
                if (_entry != null)
                {
                    _entry.IsChecked = _checkBox.Checked;
                    _checkBox.Text = _checkBox.Checked ? "✓" : "";
                    _checkBox.ForeColor = _checkBox.Checked ? Color.FromArgb(0, 150, 80) : Color.Gray;
                    _checkBox.BackColor = _checkBox.Checked ? Color.FromArgb(230, 250, 235) : Color.Transparent;
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            // 编号标签
            _indexLabel = new Label
            {
                Width = 35,
                Height = 25,
                Location = new Point(DragHandleWidth + CheckBoxWidth + 10, 5),
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 缩略图 (在勾选框右侧)
            _thumbnail = new PictureBox
            {
                Width = ThumbnailSize,
                Height = ControlMinHeight - 20,
                Location = new Point(DragHandleWidth + CheckBoxWidth + 10, 30),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.None,
                Cursor = Cursors.Hand
            };
            _thumbnail.Click += (s, e) => ThumbnailClicked?.Invoke(this, EventArgs.Empty);
            _thumbnail.MouseEnter += (s, e) => ThumbnailMouseEnter?.Invoke(this, EventArgs.Empty);
            _thumbnail.MouseLeave += (s, e) => ThumbnailMouseLeave?.Invoke(this, EventArgs.Empty);

            // 文本编辑框 (在缩略图右侧)
            _commentTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(DragHandleWidth + CheckBoxWidth + ThumbnailSize + 20, 5),
                Height = ControlMinHeight - 20,
                Font = new Font("Microsoft YaHei", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            _commentTextBox.TextChanged += (s, e) =>
            {
                if (_entry != null)
                {
                    _entry.Comment = _commentTextBox.Text;
                    CommentChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            // 删除按钮 (在文本框右侧，加宽到50)
            _deleteButton = new Button
            {
                Text = "删除",
                Width = DeleteButtonWidth,
                Height = ControlMinHeight - 20,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(200, 60, 60),
                Font = new Font("Microsoft YaHei", 9),
                Cursor = Cursors.Hand
            };
            _deleteButton.FlatAppearance.BorderSize = 0;
            _deleteButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 70, 70);
            _deleteButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 50, 50);
            _deleteButton.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);

            // 添加控件
            this.Controls.Add(_dragHandle);
            this.Controls.Add(_checkBox);
            this.Controls.Add(_indexLabel);
            this.Controls.Add(_thumbnail);
            this.Controls.Add(_commentTextBox);
            this.Controls.Add(_deleteButton);

            // 调整文本框和删除按钮位置
            this.SizeChanged += (s, e) => UpdateLayout();
        }

        /// <summary>
        /// 更新布局
        /// </summary>
        private void UpdateLayout()
        {
            int textBoxLeft = DragHandleWidth + CheckBoxWidth + ThumbnailSize + 20;
            int textBoxWidth = this.Width - textBoxLeft - DeleteButtonWidth - 20;
            
            if (_entry != null && _entry.IsTextOnly)
            {
                textBoxLeft = DragHandleWidth + CheckBoxWidth + 50;
                textBoxWidth = this.Width - textBoxLeft - DeleteButtonWidth - 20;
            }

            _commentTextBox.Location = new Point(textBoxLeft, 5);
            _commentTextBox.Width = Math.Max(100, textBoxWidth);
            _commentTextBox.Height = ControlMinHeight - 20;

            _deleteButton.Location = new Point(this.Width - DeleteButtonWidth - 10, 5);
            _deleteButton.Height = ControlMinHeight - 20;
        }

        /// <summary>
        /// 更新显示内容
        /// </summary>
        private void UpdateDisplay()
        {
            if (_entry == null) return;

            _checkBox.Checked = _entry.IsChecked;
            _checkBox.Text = _entry.IsChecked ? "✓" : "";
            _checkBox.ForeColor = _entry.IsChecked ? Color.Green : Color.Gray;
            _indexLabel.Text = $"[{_entry.Index}]";
            _commentTextBox.Text = _entry.Comment;

            if (_entry.IsTextOnly)
            {
                _thumbnail.Visible = false;
                _indexLabel.Location = new Point(DragHandleWidth + CheckBoxWidth + 10, 5);
            }
            else
            {
                _thumbnail.Visible = true;
                _thumbnail.Image = CreateThumbnail(_entry.DisplayImage);
                _indexLabel.Location = new Point(DragHandleWidth + CheckBoxWidth + 10, 5);
            }

            UpdateLayout();
        }

        /// <summary>
        /// 创建缩略图
        /// </summary>
        private Image CreateThumbnail(Bitmap source)
        {
            if (source == null) return null;

            int thumbWidth, thumbHeight;
            float ratio = (float)source.Width / source.Height;

            if (ratio > 1)
            {
                thumbWidth = ThumbnailSize - 10;
                thumbHeight = (int)(thumbWidth / ratio);
            }
            else
            {
                thumbHeight = ThumbnailSize - 40;
                thumbWidth = (int)(thumbHeight * ratio);
            }

            var thumbnail = new Bitmap(thumbWidth, thumbHeight);
            using (var g = Graphics.FromImage(thumbnail))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(source, 0, 0, thumbWidth, thumbHeight);
            }
            return thumbnail;
        }

        /// <summary>
        /// 聚焦到文本框
        /// </summary>
        public void FocusCommentBox()
        {
            _commentTextBox.Focus();
        }

        /// <summary>
        /// 更新序号显示
        /// </summary>
        public void UpdateIndex(int index)
        {
            if (_entry != null)
            {
                _entry.Index = index;
                _indexLabel.Text = $"[{index}]";
            }
        }

        #region 拖拽处理

        private void DragHandle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
            }
        }

        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                if (Math.Abs(e.Y - _dragStartPoint.Y) > 10)
                {
                    this.DoDragDrop(this, DragDropEffects.Move);
                    _isDragging = false;
                }
            }
        }

        private void DragHandle_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        #endregion

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
