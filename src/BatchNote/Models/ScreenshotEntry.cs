using System;
using System.Collections.Generic;
using System.Drawing;

namespace BatchNote.Models
{
    /// <summary>
    /// 截图条目模型
    /// </summary>
    public class ScreenshotEntry
    {
        /// <summary>
        /// 条目序号（1-based，用于显示）
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 是否为纯文本条目（无图片）
        /// </summary>
        public bool IsTextOnly { get; set; }

        /// <summary>
        /// 原始截图
        /// </summary>
        public Bitmap OriginalImage { get; set; }

        /// <summary>
        /// 带标注的图片（在原图上叠加标注）
        /// </summary>
        public Bitmap AnnotatedImage { get; set; }

        /// <summary>
        /// 画笔笔画列表（用于撤销功能）
        /// </summary>
        public List<DrawingStroke> Strokes { get; set; } = new List<DrawingStroke>();

        /// <summary>
        /// 用户文本意见
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// 是否被勾选（用于选择性合成）
        /// </summary>
        public bool IsChecked { get; set; } = true;

        /// <summary>
        /// 获取用于显示的图片（优先返回标注后的图片）
        /// </summary>
        public Bitmap DisplayImage => AnnotatedImage ?? OriginalImage;
    }
}
