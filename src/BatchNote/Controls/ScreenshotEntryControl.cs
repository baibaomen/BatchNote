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
        private const int ThumbnailSize = 90;
        private const int ControlMinHeight = 100;
        private const int IndexLabelWidth = 36;
        private const int CheckBoxWidth = 36;

        private CheckBox _checkBox;
        private Label _indexLabel;
        private PictureBox _thumbnail;
        private TextBox _commentTextBox;
        private Button _deleteButton;

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
            // 启用双缓冲减少闪烁
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            this.UpdateStyles();
            this.DoubleBuffered = true;
            
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // 设置控件属性
            this.Height = ControlMinHeight;
            this.AutoSize = false;
            this.Padding = new Padding(6);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.None;
            this.Margin = new Padding(0, 0, 0, 8);
            this.Cursor = Cursors.SizeAll;

            // 绘制边框
            this.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(225, 225, 225), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // 拖拽支持
            this.MouseDown += DragHandle_MouseDown;
            this.MouseMove += DragHandle_MouseMove;
            this.MouseUp += DragHandle_MouseUp;

            // 编号标签 - 放在最左边
            _indexLabel = new Label
            {
                Width = IndexLabelWidth,
                Height = ControlMinHeight - 12,
                Location = new Point(6, 6),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 100, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(240, 248, 255),
                Cursor = Cursors.SizeAll
            };
            _indexLabel.MouseDown += DragHandle_MouseDown;
            _indexLabel.MouseMove += DragHandle_MouseMove;
            _indexLabel.MouseUp += DragHandle_MouseUp;

            // 勾选框
            _checkBox = new CheckBox
            {
                Width = CheckBoxWidth,
                Height = ControlMinHeight - 12,
                Checked = true,
                Location = new Point(IndexLabelWidth + 10, 6),
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "✓",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 80)
            };
            _checkBox.FlatAppearance.BorderSize = 1;
            _checkBox.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 100);
            _checkBox.FlatAppearance.CheckedBackColor = Color.FromArgb(232, 250, 238);
            _checkBox.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 252, 248);
            _checkBox.CheckedChanged += (s, e) =>
            {
                if (_entry != null)
                {
                    _entry.IsChecked = _checkBox.Checked;
                    _checkBox.Text = _checkBox.Checked ? "✓" : "";
                    _checkBox.ForeColor = _checkBox.Checked ? Color.FromArgb(0, 150, 80) : Color.FromArgb(180, 180, 180);
                    _checkBox.BackColor = _checkBox.Checked ? Color.FromArgb(232, 250, 238) : Color.FromArgb(250, 250, 250);
                    _checkBox.FlatAppearance.BorderColor = _checkBox.Checked ? Color.FromArgb(0, 180, 100) : Color.FromArgb(200, 200, 200);
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            // 缩略图
            _thumbnail = new PictureBox
            {
                Width = ThumbnailSize,
                Height = ControlMinHeight - 12,
                Location = new Point(IndexLabelWidth + CheckBoxWidth + 14, 6),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            _thumbnail.Click += (s, e) => ThumbnailClicked?.Invoke(this, EventArgs.Empty);
            _thumbnail.MouseEnter += (s, e) => ThumbnailMouseEnter?.Invoke(this, EventArgs.Empty);
            _thumbnail.MouseLeave += (s, e) => ThumbnailMouseLeave?.Invoke(this, EventArgs.Empty);

            // 文本编辑框
            _commentTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(IndexLabelWidth + CheckBoxWidth + ThumbnailSize + 18, 6),
                Height = ControlMinHeight - 12,
                Font = new Font("Microsoft YaHei", 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(252, 252, 252)
            };
            _commentTextBox.TextChanged += (s, e) =>
            {
                if (_entry != null)
                {
                    _entry.Comment = _commentTextBox.Text;
                    CommentChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            // 删除按钮
            _deleteButton = new Button
            {
                Text = "×",
                Width = 32,
                Height = ControlMinHeight - 12,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 80, 80),
                BackColor = Color.FromArgb(255, 245, 245),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _deleteButton.FlatAppearance.BorderSize = 1;
            _deleteButton.FlatAppearance.BorderColor = Color.FromArgb(230, 180, 180);
            _deleteButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 200, 200);
            _deleteButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 100, 100);
            _deleteButton.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);

            // 添加控件
            this.Controls.Add(_indexLabel);
            this.Controls.Add(_checkBox);
            this.Controls.Add(_thumbnail);
            this.Controls.Add(_commentTextBox);
            this.Controls.Add(_deleteButton);

            // 调整布局
            this.SizeChanged += (s, e) => UpdateLayout();
        }

        /// <summary>
        /// 更新布局
        /// </summary>
        private void UpdateLayout()
        {
            const int deleteButtonWidth = 32;
            int textBoxLeft = IndexLabelWidth + CheckBoxWidth + ThumbnailSize + 18;
            int textBoxWidth = this.Width - textBoxLeft - deleteButtonWidth - 16;
            
            if (_entry != null && _entry.IsTextOnly)
            {
                textBoxLeft = IndexLabelWidth + CheckBoxWidth + 18;
                textBoxWidth = this.Width - textBoxLeft - deleteButtonWidth - 16;
            }

            _commentTextBox.Location = new Point(textBoxLeft, 6);
            _commentTextBox.Width = Math.Max(100, textBoxWidth);
            _commentTextBox.Height = ControlMinHeight - 12;

            _deleteButton.Location = new Point(this.Width - deleteButtonWidth - 8, 6);
            _deleteButton.Height = ControlMinHeight - 12;
        }

        /// <summary>
        /// 更新显示内容
        /// </summary>
        private void UpdateDisplay()
        {
            if (_entry == null) return;

            _checkBox.Checked = _entry.IsChecked;
            _checkBox.Text = _entry.IsChecked ? "✓" : "";
            _checkBox.ForeColor = _entry.IsChecked ? Color.FromArgb(0, 150, 80) : Color.FromArgb(180, 180, 180);
            _checkBox.BackColor = _entry.IsChecked ? Color.FromArgb(232, 250, 238) : Color.FromArgb(250, 250, 250);
            _checkBox.FlatAppearance.BorderColor = _entry.IsChecked ? Color.FromArgb(0, 180, 100) : Color.FromArgb(200, 200, 200);
            _indexLabel.Text = $"{_entry.Index}";
            _commentTextBox.Text = _entry.Comment;

            if (_entry.IsTextOnly)
            {
                _thumbnail.Visible = false;
            }
            else
            {
                _thumbnail.Visible = true;
                _thumbnail.Image = CreateThumbnail(_entry.DisplayImage);
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
            int maxSize = ThumbnailSize - 4;

            if (ratio > 1)
            {
                thumbWidth = maxSize;
                thumbHeight = (int)(thumbWidth / ratio);
            }
            else
            {
                thumbHeight = maxSize;
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
                _indexLabel.Text = $"{index}";
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
