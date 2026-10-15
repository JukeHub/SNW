using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SevenNineWorthy.Core
{
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum PatternKind { OrbiterUpgrade, Comfort, Media }

    /// <summary>A printable blueprint from patterns.json.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class PatternTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PatternKind Kind { get; set; }
        public int MaterialCost { get; set; }
        public string Effect { get; set; }

        /// <summary>Required when Kind is Media, absent otherwise.</summary>
        [JsonProperty(Required = Required.Default)]
        public MediaType? MediaType { get; set; }
    }
}
