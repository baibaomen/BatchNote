using System.Collections.Generic;
using System.Drawing;

namespace BatchNote.Models
{
    /// <summary>
    /// 画笔笔画模型
    /// </summary>
    public class DrawingStroke
    {
        /// <summary>
        /// 笔画路径点序列
        /// </summary>
        public List<Point> Points { get; set; } = new List<Point>();

        /// <summary>
        /// 笔画颜色
        /// </summary>
        public Color Color { get; set; } = Color.Red;

        /// <summary>
        /// 笔画宽度
        /// </summary>
        public float Width { get; set; } = 3f;
    }
}
