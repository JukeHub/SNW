using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>Something a bot can do in a fight.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class Ability
    {
        public string Name { get; set; }

        /// <summary>Battery spent each time it is used.</summary>
        public int EnergyCost { get; set; }

        /// <summary>Damage dealt each time it is used.</summary>
        public int Power { get; set; }
    }
}
