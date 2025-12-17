using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using BatchNote.Models;

namespace BatchNote.Services
{
    /// <summary>
    /// 图片合成服务
    /// </summary>
    public class CompositeService
    {
        private const int Padding = 30;
        private const int SeparatorHeight = 8;
        private const int EntrySpacing = 40;
        private const float MinFontSizeRatio = 0.02f;

        /// <summary>
        /// 合成多个截图条目为一张大图
        /// </summary>
        public Bitmap Composite(IList<ScreenshotEntry> entries)
        {
            var checkedEntries = entries.Where(e => e.IsChecked).ToList();
            if (checkedEntries.Count == 0)
            {
                return null;
            }

            // 计算画布宽度
            int maxImageWidth = 0;
            int maxImageDimension = 0;
            
            foreach (var entry in checkedEntries)
            {
                if (!entry.IsTextOnly && entry.DisplayImage != null)
                {
                    maxImageWidth = Math.Max(maxImageWidth, entry.DisplayImage.Width);
                    maxImageDimension = Math.Max(maxImageDimension, 
                        Math.Max(entry.DisplayImage.Width, entry.DisplayImage.Height));
                }
            }

            int canvasWidth = Math.Max(maxImageWidth, 600) + Padding * 2;
            int contentWidth = canvasWidth - Padding * 2;

            // 计算字体大小
            float baseFontSize = Math.Max(16f, maxImageDimension * MinFontSizeRatio);
            float titleFontSize = baseFontSize + 8;
            int titleHeight = (int)(titleFontSize * 2f);

            // 计算总高度
            int totalHeight = Padding;
            foreach (var entry in checkedEntries)
            {
                totalHeight += CalculateEntryHeight(entry, contentWidth, baseFontSize, titleHeight);
                totalHeight += SeparatorHeight + EntrySpacing;
            }

            // 创建画布
            var result = new Bitmap(canvasWidth, totalHeight);
            using (var g = Graphics.FromImage(result))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.White);

                int currentY = Padding;

                for (int i = 0; i < checkedEntries.Count; i++)
                {
                    var entry = checkedEntries[i];
                    currentY = DrawEntry(g, entry, Padding, currentY, contentWidth, canvasWidth, baseFontSize, titleFontSize, titleHeight);
                    
                    // 绘制分隔区域
                    currentY += 15;
                    DrawSeparator(g, currentY, canvasWidth);
                    currentY += SeparatorHeight + EntrySpacing;
                }
            }

            return result;
        }

        /// <summary>
        /// 绘制分隔区域
        /// </summary>
        private void DrawSeparator(Graphics g, int y, int width)
        {
            // 绘制渐变分隔条
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, y, width, SeparatorHeight),
                Color.FromArgb(230, 230, 230),
                Color.FromArgb(200, 200, 200),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, 0, y, width, SeparatorHeight);
            }
            
            // 上下边线
            using (var pen = new Pen(Color.FromArgb(180, 180, 180), 1))
            {
                g.DrawLine(pen, 0, y, width, y);
                g.DrawLine(pen, 0, y + SeparatorHeight - 1, width, y + SeparatorHeight - 1);
            }
        }

        /// <summary>
        /// 计算条目高度
        /// </summary>
        private int CalculateEntryHeight(ScreenshotEntry entry, int contentWidth, float fontSize, int titleHeight)
        {
            int height = titleHeight + 15;
            
            if (!entry.IsTextOnly && entry.DisplayImage != null)
            {
                height += entry.DisplayImage.Height + 20;
            }
            
            if (!string.IsNullOrWhiteSpace(entry.Comment))
            {
                int textHeight = CalculateTextHeight(entry.Comment, contentWidth, fontSize);
                height += textHeight + 15;
            }
            
            return height;
        }

        /// <summary>
        /// 计算文本高度
        /// </summary>
        private int CalculateTextHeight(string text, int width, float fontSize)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
                
            using (var font = new Font("Microsoft YaHei", fontSize))
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                var size = g.MeasureString(text, font, width);
                return Math.Max((int)(fontSize * 1.8f), (int)Math.Ceiling(size.Height) + 8);
            }
        }

        private int DrawEntry(Graphics g, ScreenshotEntry entry, int x, int y, int contentWidth, int canvasWidth, float fontSize, float titleFontSize, int titleHeight)
        {
            // 1. 绘制编号标题区域（带蓝色渐变背景）
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, y, canvasWidth, titleHeight),
                Color.FromArgb(240, 248, 255),
                Color.FromArgb(220, 235, 252),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, 0, y, canvasWidth, titleHeight);
            }
            
            // 左侧蓝色强调条
            using (var brush = new SolidBrush(Color.FromArgb(0, 122, 204)))
            {
                g.FillRectangle(brush, 0, y, 5, titleHeight);
            }
            
            // 编号文字
            using (var font = new Font("Microsoft YaHei", titleFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(0, 100, 180)))
            {
                var textY = y + (titleHeight - (int)titleFontSize) / 2 - 2;
                g.DrawString($"[{entry.Index}]", font, brush, x + 5, textY);
            }
            y += titleHeight + 15;

            // 2. 绘制图片
            if (!entry.IsTextOnly && entry.DisplayImage != null)
            {
                // 图片边框
                using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                {
                    g.DrawRectangle(pen, x - 1, y - 1, entry.DisplayImage.Width + 1, entry.DisplayImage.Height + 1);
                }
                g.DrawImage(entry.DisplayImage, x, y);
                y += entry.DisplayImage.Height + 20;
            }

            // 3. 绘制批注文本
            if (!string.IsNullOrWhiteSpace(entry.Comment))
            {
                // 文本背景
                int textHeight = CalculateTextHeight(entry.Comment, contentWidth, fontSize);
                using (var brush = new SolidBrush(Color.FromArgb(252, 252, 252)))
                {
                    g.FillRectangle(brush, x - 5, y - 5, contentWidth + 10, textHeight + 10);
                }
                
                using (var font = new Font("Microsoft YaHei", fontSize))
                using (var brush = new SolidBrush(Color.FromArgb(40, 40, 40)))
                {
                    var textRect = new RectangleF(x, y, contentWidth, textHeight);
                    g.DrawString(entry.Comment, font, brush, textRect);
                    y += textHeight + 15;
                }
            }

            return y;
        }
    }
}
