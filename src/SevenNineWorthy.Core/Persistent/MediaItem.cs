using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SevenNineWorthy.Core
{
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum MediaType { Book, Music, Movie }

    /// <summary>A collected book, album or film.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class MediaItem
    {
        public MediaType Type { get; set; }
        public string Title { get; set; }
        public string Flavor { get; set; }
    }
}
