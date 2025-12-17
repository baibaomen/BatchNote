using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using BatchNote.Models;

namespace BatchNote.Services
{
    /// <summary>
    /// 历史记录服务
    /// </summary>
    public class HistoryService
    {
        private readonly string _historyDirectory;
        private readonly int _maxHistoryCount;

        /// <summary>
        /// 历史记录条目
        /// </summary>
        public class HistoryItem
        {
            public string Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public string CompositeImagePath { get; set; }
            public string FolderPath { get; set; }
            public int EntryCount { get; set; }
            public List<EntryMeta> Entries { get; set; } = new List<EntryMeta>();
        }

        /// <summary>
        /// 条目元数据
        /// </summary>
        public class EntryMeta
        {
            public int Index { get; set; }
            public bool IsTextOnly { get; set; }
            public string ImageFileName { get; set; }
            public string Comment { get; set; }
            public bool IsChecked { get; set; }
        }

        public HistoryService(int maxHistoryCount = 100)
        {
            _maxHistoryCount = maxHistoryCount;

            // 使用 %LOCALAPPDATA%\BatchNote\history
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _historyDirectory = Path.Combine(localAppData, "BatchNote", "history");

            // 确保目录存在
            if (!Directory.Exists(_historyDirectory))
            {
                Directory.CreateDirectory(_historyDirectory);
            }
        }

        /// <summary>
        /// 保存合成图到历史记录（包含原始素材）
        /// </summary>
        /// <returns>保存的历史记录对象</returns>
        public HistoryItem Save(Bitmap compositeImage, IList<ScreenshotEntry> entries)
        {
            if (compositeImage == null) return null;

            var timestamp = DateTime.Now;
            var id = timestamp.ToString("yyyy-MM-dd_HHmmss");
            
            // 创建此次记录的专属文件夹
            var folderPath = Path.Combine(_historyDirectory, id);
            Directory.CreateDirectory(folderPath);

            var compositeImagePath = Path.Combine(folderPath, "composite.png");
            var thumbnailPath = Path.Combine(folderPath, "thumbnail.png");
            var metaPath = Path.Combine(folderPath, "meta.json");
            var summaryPath = Path.Combine(folderPath, "summary.txt");

            // 保存合成图
            compositeImage.Save(compositeImagePath, ImageFormat.Png);
            
            // 保存缩略图（用于历史列表快速加载）
            using (var thumbnail = CreateThumbnail(compositeImage, 80, 80))
            {
                thumbnail.Save(thumbnailPath, ImageFormat.Png);
            }

            // 创建历史记录元数据
            var historyItem = new HistoryItem
            {
                Id = id,
                CreatedAt = timestamp,
                CompositeImagePath = compositeImagePath,
                FolderPath = folderPath,
                EntryCount = entries.Count(e => e.IsChecked)
            };

            // 保存每个条目的原始图片和元数据
            var summary = new StringBuilder();
            summary.AppendLine($"BatchNote 批注记录");
            summary.AppendLine($"创建时间: {timestamp:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"条目数量: {historyItem.EntryCount}");
            summary.AppendLine(new string('=', 50));
            summary.AppendLine();

            foreach (var entry in entries.Where(e => e.IsChecked))
            {
                var entryMeta = new EntryMeta
                {
                    Index = entry.Index,
                    IsTextOnly = entry.IsTextOnly,
                    Comment = entry.Comment ?? string.Empty,
                    IsChecked = entry.IsChecked
                };

                // 保存图片
                if (!entry.IsTextOnly && entry.DisplayImage != null)
                {
                    var imageFileName = $"entry_{entry.Index}.png";
                    var imagePath = Path.Combine(folderPath, imageFileName);
                    entry.DisplayImage.Save(imagePath, ImageFormat.Png);
                    entryMeta.ImageFileName = imageFileName;
                }

                historyItem.Entries.Add(entryMeta);

                // 写入汇总文本
                summary.AppendLine($"[{entry.Index}]");
                if (!entry.IsTextOnly)
                {
                    summary.AppendLine($"图片: {entryMeta.ImageFileName ?? "(无)"}");
                }
                if (!string.IsNullOrWhiteSpace(entry.Comment))
                {
                    summary.AppendLine($"批注: {entry.Comment}");
                }
                summary.AppendLine();
            }

            // 保存元数据
            var json = JsonConvert.SerializeObject(historyItem, Formatting.Indented);
            File.WriteAllText(metaPath, json, Encoding.UTF8);

            // 保存汇总文本
            File.WriteAllText(summaryPath, summary.ToString(), Encoding.UTF8);

            // 自动清理旧记录
            CleanupOldHistory();

            return historyItem;
        }

        /// <summary>
        /// 获取历史记录列表
        /// </summary>
        public List<HistoryItem> GetHistoryList()
        {
            var items = new List<HistoryItem>();

            if (!Directory.Exists(_historyDirectory))
                return items;

            var folders = Directory.GetDirectories(_historyDirectory);

            foreach (var folder in folders)
            {
                try
                {
                    var metaPath = Path.Combine(folder, "meta.json");
                    if (File.Exists(metaPath))
                    {
                        var json = File.ReadAllText(metaPath);
                        var item = JsonConvert.DeserializeObject<HistoryItem>(json);
                        if (item != null && File.Exists(item.CompositeImagePath))
                        {
                            items.Add(item);
                        }
                    }
                }
                catch
                {
                    // 忽略无效的记录
                }
            }

            return items.OrderByDescending(x => x.CreatedAt).ToList();
        }

        /// <summary>
        /// 加载合成图
        /// </summary>
        public Bitmap LoadCompositeImage(string id)
        {
            var imagePath = Path.Combine(_historyDirectory, id, "composite.png");
            if (File.Exists(imagePath))
            {
                return new Bitmap(imagePath);
            }
            return null;
        }

        /// <summary>
        /// 加载历史记录缩略图（快速加载）
        /// </summary>
        public Bitmap LoadThumbnail(string id)
        {
            var thumbnailPath = Path.Combine(_historyDirectory, id, "thumbnail.png");
            if (File.Exists(thumbnailPath))
            {
                return new Bitmap(thumbnailPath);
            }
            // 如果缩略图不存在，则从原图生成（兼容旧记录）
            var imagePath = Path.Combine(_historyDirectory, id, "composite.png");
            if (File.Exists(imagePath))
            {
                using (var original = new Bitmap(imagePath))
                {
                    var thumbnail = CreateThumbnail(original, 80, 80);
                    // 保存缩略图供下次使用
                    thumbnail.Save(thumbnailPath, ImageFormat.Png);
                    return thumbnail;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 创建缩略图
        /// </summary>
        private Bitmap CreateThumbnail(Bitmap source, int maxWidth, int maxHeight)
        {
            float ratio = Math.Min((float)maxWidth / source.Width, (float)maxHeight / source.Height);
            int newWidth = Math.Max(1, (int)(source.Width * ratio));
            int newHeight = Math.Max(1, (int)(source.Height * ratio));

            var thumbnail = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(thumbnail))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(source, 0, 0, newWidth, newHeight);
            }
            return thumbnail;
        }

        /// <summary>
        /// 加载原始条目图片
        /// </summary>
        public Bitmap LoadEntryImage(string historyId, string imageFileName)
        {
            var imagePath = Path.Combine(_historyDirectory, historyId, imageFileName);
            if (File.Exists(imagePath))
            {
                return new Bitmap(imagePath);
            }
            return null;
        }

        /// <summary>
        /// 恢复历史记录为可编辑的条目列表
        /// </summary>
        public List<ScreenshotEntry> RestoreEntries(HistoryItem historyItem)
        {
            var entries = new List<ScreenshotEntry>();

            foreach (var meta in historyItem.Entries)
            {
                var entry = new ScreenshotEntry
                {
                    Index = meta.Index,
                    IsTextOnly = meta.IsTextOnly,
                    Comment = meta.Comment,
                    IsChecked = meta.IsChecked
                };

                if (!meta.IsTextOnly && !string.IsNullOrEmpty(meta.ImageFileName))
                {
                    entry.OriginalImage = LoadEntryImage(historyItem.Id, meta.ImageFileName);
                }

                entries.Add(entry);
            }

            return entries;
        }

        /// <summary>
        /// 删除历史记录
        /// </summary>
        public bool Delete(string id)
        {
            try
            {
                var folderPath = Path.Combine(_historyDirectory, id);
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 清理超出数量限制的旧记录
        /// </summary>
        private void CleanupOldHistory()
        {
            var items = GetHistoryList();

            if (items.Count > _maxHistoryCount)
            {
                var toDelete = items.Skip(_maxHistoryCount).ToList();
                foreach (var item in toDelete)
                {
                    Delete(item.Id);
                }
            }
        }

        /// <summary>
        /// 获取历史目录路径
        /// </summary>
        public string GetHistoryDirectory()
        {
            return _historyDirectory;
        }

        /// <summary>
        /// 获取指定历史记录的文件夹路径
        /// </summary>
        public string GetHistoryFolderPath(string id)
        {
            return Path.Combine(_historyDirectory, id);
        }
    }
}
