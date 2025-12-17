using System.Collections.Generic;
using System.Drawing;
using BatchNote.Models;
using BatchNote.Services;
using Xunit;

namespace BatchNote.Tests.Services
{
    public class CompositeServiceTests
    {
        [Fact]
        public void Composite_WithNoEntries_ReturnsNull()
        {
            // Arrange
            var service = new CompositeService();
            var entries = new List<ScreenshotEntry>();

            // Act
            var result = service.Composite(entries);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Composite_WithUncheckedEntries_ReturnsNull()
        {
            // Arrange
            var service = new CompositeService();
            var entries = new List<ScreenshotEntry>
            {
                new ScreenshotEntry { Index = 1, IsChecked = false, Comment = "Test" }
            };

            // Act
            var result = service.Composite(entries);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Composite_WithTextOnlyEntry_ReturnsImage()
        {
            // Arrange
            var service = new CompositeService();
            var entries = new List<ScreenshotEntry>
            {
                new ScreenshotEntry 
                { 
                    Index = 1, 
                    IsChecked = true, 
                    IsTextOnly = true, 
                    Comment = "这是一条纯文本条目" 
                }
            };

            // Act
            var result = service.Composite(entries);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Width > 0);
            Assert.True(result.Height > 0);

            // Cleanup
            result.Dispose();
        }
    }
}
