using System.Collections.Generic;
using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>A zone definition from zones.json. Zones are rolled from these at run time.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class ZoneTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IntRange MaterialYield { get; set; }
        public List<string> EnemyPool { get; set; }
        public List<string> BossPool { get; set; }
        public List<string> LootPool { get; set; }
        public List<string> PatternPool { get; set; }
        public List<SurvivorTemplate> Survivors { get; set; }
    }

    [JsonObject(ItemRequired = Required.Always)]
    public class IntRange
    {
        public int Min { get; set; }
        public int Max { get; set; }

        /// <summary>Inclusive of both ends.</summary>
        public int Roll(System.Random rng) => rng.Next(Min, Max + 1);
    }

    [JsonObject(ItemRequired = Required.Always)]
    public class SurvivorTemplate
    {
        public string Name { get; set; }
        public string Situation { get; set; }
    }
}
