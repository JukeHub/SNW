using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>A physical loot definition from loot.json.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class LootTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Flavor { get; set; }
    }
}
