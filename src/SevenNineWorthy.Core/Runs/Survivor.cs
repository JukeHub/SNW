using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>Someone still living on the surface, and what they could use a hand with.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class Survivor
    {
        public string Name { get; set; }
        public string Need { get; set; }
    }
}
