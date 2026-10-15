using System.Collections.Generic;
using System.Linq;

namespace SevenNineWorthy.Core
{
    /// <summary>The orbiter's media library, plus what was last consumed (the source of the current "state of mind").</summary>
    public class MediaCollection
    {
        public List<MediaItem> Items { get; set; } = new List<MediaItem>();
        public string LastConsumedTitle { get; set; }

        public void Add(MediaItem item) => Items.Add(item);

        public bool Contains(string title) => Items.Any(m => m.Title == title);

        public List<MediaItem> OfType(MediaType type) => Items.Where(m => m.Type == type).ToList();

        public void Consume(MediaItem item) => LastConsumedTitle = item.Title;

        /// <summary>Null if nothing has been consumed yet.</summary>
        public MediaItem GetLastConsumed() => Items.FirstOrDefault(m => m.Title == LastConsumedTitle);
    }
}
