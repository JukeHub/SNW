using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>An enemy or boss definition from enemies.json.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class EnemyTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Health { get; set; }
        public int Attack { get; set; }
        public bool IsBoss { get; set; }
        public string Flavor { get; set; }
    }
}
