using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BatchNote.Models;

namespace BatchNote.Services
{
    /// <summary>
    /// 图片合成服务
    /// </summary>
    public class CompositeService
    {
        private const int Padding = 20;
        private const int TextHeight = 60;
        private const int SeparatorHeight = 2;

        /// <summary>
        /// 合成多个截图条目为一张大图
        /// </summary>
        /// <param name="entries">截图条目列表（只处理已勾选的）</param>
        /// <returns>合成后的图片</returns>
        public Bitmap Composite(IList<ScreenshotEntry> entries)
        {
            var checkedEntries = entries.Where(e => e.IsChecked).ToList();
            if (checkedEntries.Count == 0)
            {
                return null;
            }

            // 计算画布尺寸
            int maxWidth = 0;
            int totalHeight = Padding;

            foreach (var entry in checkedEntries)
            {
                if (!entry.IsTextOnly && entry.DisplayImage != null)
                {
                    maxWidth = Math.Max(maxWidth, entry.DisplayImage.Width);
                }
                totalHeight += CalculateEntryHeight(entry) + Padding + SeparatorHeight;
            }

            maxWidth = Math.Max(maxWidth, 400); // 最小宽度
            maxWidth += Padding * 2;

            // 创建画布
            var result = new Bitmap(maxWidth, totalHeight);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.White);

                int currentY = Padding;

                foreach (var entry in checkedEntries)
                {
                    currentY = DrawEntry(g, entry, Padding, currentY, maxWidth - Padding * 2);
                    
                    // 绘制分隔线
                    using (var pen = new Pen(Color.LightGray, SeparatorHeight))
                    {
                        g.DrawLine(pen, 0, currentY, maxWidth, currentY);
                    }
                    currentY += SeparatorHeight + Padding;
                }
            }

            return result;
        }

        private int CalculateEntryHeight(ScreenshotEntry entry)
        {
            int height = TextHeight + 30; // 编号 + 文本区域
            if (!entry.IsTextOnly && entry.DisplayImage != null)
            {
                height += entry.DisplayImage.Height + Padding;
            }
            return height;
        }

        private int DrawEntry(Graphics g, ScreenshotEntry entry, int x, int y, int width)
        {
            // 绘制编号
            using (var font = new Font("Microsoft YaHei", 14, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(50, 50, 50)))
            {
                g.DrawString($"[{entry.Index}]", font, brush, x, y);
            }
            y += 30;

            // 绘制图片（如果有）
            if (!entry.IsTextOnly && entry.DisplayImage != null)
            {
                g.DrawImage(entry.DisplayImage, x, y);
                y += entry.DisplayImage.Height + Padding;
            }

            // 绘制文本
            if (!string.IsNullOrWhiteSpace(entry.Comment))
            {
                using (var font = new Font("Microsoft YaHei", 11))
                using (var brush = new SolidBrush(Color.FromArgb(30, 30, 30)))
                {
                    var textRect = new RectangleF(x, y, width, TextHeight);
                    g.DrawString(entry.Comment, font, brush, textRect);
                }
                y += TextHeight;
            }

            return y;
        }
    }
}
