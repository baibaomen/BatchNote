using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BatchNote.Models;

namespace BatchNote.Forms
{
    /// <summary>
    /// 图片预览和标注窗口
    /// </summary>
    public class PreviewForm : Form
    {
        private PictureBox _pictureBox;
        private Panel _canvasPanel;
        private Button _undoButton;
        private Button _clearButton;
        private Button _closeButton;

        private ScreenshotEntry _entry;
        private List<DrawingStroke> _strokes;
        private DrawingStroke _currentStroke;
        private bool _isDrawing;
        private Bitmap _displayBitmap;
        private Timer _mouseLeaveTimer;

        // 画笔设置
        private Color _penColor = Color.Red;
        private float _penWidth = 3f;

        /// <summary>
        /// 标注变更事件
        /// </summary>
        public event EventHandler AnnotationChanged;

        public PreviewForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // 窗口属性 - 无边框
            this.Text = "图片预览";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(40, 40, 40);
            this.ShowInTaskbar = false;

            // 鼠标移出检测定时器 - 持续检测
            _mouseLeaveTimer = new Timer { Interval = 150 };
            _mouseLeaveTimer.Tick += MouseLeaveTimer_Tick;

            // 工具栏面板
            var toolPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(50, 50, 50),
                Padding = new Padding(5)
            };

            // 撤销按钮
            _undoButton = new Button
            {
                Text = "撤销",
                Width = 60,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70),
                Location = new Point(5, 6)
            };
            _undoButton.FlatAppearance.BorderColor = Color.Gray;
            _undoButton.Click += (s, e) => UndoLastStroke();

            // 清除按钮
            _clearButton = new Button
            {
                Text = "清除",
                Width = 60,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70),
                Location = new Point(70, 6)
            };
            _clearButton.FlatAppearance.BorderColor = Color.Gray;
            _clearButton.Click += (s, e) => ClearAllStrokes();

            // 完成按钮
            _closeButton = new Button
            {
                Text = "完成",
                Width = 60,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 120, 215),
                Location = new Point(140, 6)
            };
            _closeButton.FlatAppearance.BorderSize = 0;
            _closeButton.Click += (s, e) => this.Close();

            // 提示标签
            var tipLabel = new Label
            {
                Text = "💡 画笔标注 | Ctrl+Z撤销 | ESC关闭",
                AutoSize = true,
                ForeColor = Color.LightGray,
                Location = new Point(210, 12)
            };

            toolPanel.Controls.Add(_undoButton);
            toolPanel.Controls.Add(_clearButton);
            toolPanel.Controls.Add(_closeButton);
            toolPanel.Controls.Add(tipLabel);

            // 画布容器面板（支持滚动）
            _canvasPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // 图片显示控件
            _pictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(0, 0),
                Cursor = Cursors.Cross
            };
            _pictureBox.MouseDown += PictureBox_MouseDown;
            _pictureBox.MouseMove += PictureBox_MouseMove;
            _pictureBox.MouseUp += PictureBox_MouseUp;
            _pictureBox.Paint += PictureBox_Paint;

            _canvasPanel.Controls.Add(_pictureBox);

            this.Controls.Add(_canvasPanel);
            this.Controls.Add(toolPanel);

            // 键盘事件
            this.KeyDown += PreviewForm_KeyDown;

            // 窗口关闭时保存标注
            this.FormClosing += PreviewForm_FormClosing;

            // 窗口显示后启动检测
            this.Shown += (s, e) => _mouseLeaveTimer.Start();
        }

        private void MouseLeaveTimer_Tick(object sender, EventArgs e)
        {
            // 检测鼠标位置是否在窗口范围内
            Point mousePos = Control.MousePosition;
            Rectangle formBounds = new Rectangle(this.Location, this.Size);
            
            if (!formBounds.Contains(mousePos))
            {
                _mouseLeaveTimer.Stop();
                this.Close();
            }
        }

        /// <summary>
        /// 设置要预览和标注的条目
        /// </summary>
        public void SetEntry(ScreenshotEntry entry)
        {
            _entry = entry;
            _strokes = new List<DrawingStroke>(entry.Strokes);

            if (entry.OriginalImage != null)
            {
                _displayBitmap = new Bitmap(entry.OriginalImage);
                _pictureBox.Image = _displayBitmap;

                // 根据图片大小调整窗口大小，确保不小于图片实际大小
                int toolbarHeight = 40;
                int padding = 20;
                int imgWidth = entry.OriginalImage.Width;
                int imgHeight = entry.OriginalImage.Height;

                // 获取屏幕大小
                var screen = Screen.FromControl(this);
                int maxWidth = screen.WorkingArea.Width - 100;
                int maxHeight = screen.WorkingArea.Height - 100;

                // 计算窗口大小（至少等于图片大小，但不超过屏幕）
                int formWidth = Math.Min(Math.Max(imgWidth + padding, 450), maxWidth);
                int formHeight = Math.Min(Math.Max(imgHeight + toolbarHeight + padding, 300), maxHeight);

                this.Size = new Size(formWidth, formHeight);

                // 居中显示在鼠标位置附近
                Point mousePos = Control.MousePosition;
                int x = Math.Max(0, Math.Min(mousePos.X - formWidth / 2, screen.WorkingArea.Width - formWidth));
                int y = Math.Max(0, Math.Min(mousePos.Y - formHeight / 2, screen.WorkingArea.Height - formHeight));
                this.Location = new Point(x, y);

                // 居中图片
                CenterImage();
            }

            RefreshDisplay();
        }

        /// <summary>
        /// 居中图片
        /// </summary>
        private void CenterImage()
        {
            if (_pictureBox.Image == null) return;

            int x = Math.Max(0, (_canvasPanel.ClientSize.Width - _pictureBox.Width) / 2);
            int y = Math.Max(0, (_canvasPanel.ClientSize.Height - _pictureBox.Height) / 2);
            _pictureBox.Location = new Point(x, y);
        }

        /// <summary>
        /// 刷新显示（重绘标注）
        /// </summary>
        private void RefreshDisplay()
        {
            _pictureBox.Invalidate();
        }

        #region 画笔绘制

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDrawing = true;
                _currentStroke = new DrawingStroke
                {
                    Color = _penColor,
                    Width = _penWidth
                };
                _currentStroke.Points.Add(e.Location);
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing && _currentStroke != null)
            {
                _currentStroke.Points.Add(e.Location);
                RefreshDisplay();
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDrawing && _currentStroke != null && _currentStroke.Points.Count > 1)
            {
                _strokes.Add(_currentStroke);
            }
            _currentStroke = null;
            _isDrawing = false;
            RefreshDisplay();
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // 绘制已保存的笔画
            foreach (var stroke in _strokes)
            {
                DrawStroke(e.Graphics, stroke);
            }

            // 绘制当前正在绘制的笔画
            if (_currentStroke != null && _currentStroke.Points.Count > 1)
            {
                DrawStroke(e.Graphics, _currentStroke);
            }
        }

        private void DrawStroke(Graphics g, DrawingStroke stroke)
        {
            if (stroke.Points.Count < 2) return;

            using (var pen = new Pen(stroke.Color, stroke.Width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                g.DrawLines(pen, stroke.Points.ToArray());
            }
        }

        #endregion

        #region 撤销和清除

        private void UndoLastStroke()
        {
            if (_strokes.Count > 0)
            {
                _strokes.RemoveAt(_strokes.Count - 1);
                RefreshDisplay();
            }
        }

        private void ClearAllStrokes()
        {
            if (_strokes.Count > 0)
            {
                var result = MessageBox.Show(
                    "确定要清除所有标注吗？",
                    "确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _strokes.Clear();
                    RefreshDisplay();
                }
            }
        }

        #endregion

        #region 键盘快捷键

        private void PreviewForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                UndoLastStroke();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }

        #endregion

        #region 保存标注

        private void PreviewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveAnnotations();
        }

        /// <summary>
        /// 保存标注到条目
        /// </summary>
        private void SaveAnnotations()
        {
            if (_entry == null) return;

            // 保存笔画数据
            _entry.Strokes = new List<DrawingStroke>(_strokes);

            // 生成带标注的图片
            if (_entry.OriginalImage != null && _strokes.Count > 0)
            {
                var annotated = new Bitmap(_entry.OriginalImage);
                using (var g = Graphics.FromImage(annotated))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    foreach (var stroke in _strokes)
                    {
                        DrawStroke(g, stroke);
                    }
                }
                _entry.AnnotatedImage?.Dispose();
                _entry.AnnotatedImage = annotated;
            }
            else if (_strokes.Count == 0)
            {
                _entry.AnnotatedImage?.Dispose();
                _entry.AnnotatedImage = null;
            }

            AnnotationChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _displayBitmap?.Dispose();
                _mouseLeaveTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
