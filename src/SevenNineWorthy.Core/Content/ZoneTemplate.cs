using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>A zone definition from zones.json. Each visit builds a fresh Zone from one of these.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class ZoneTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinMaterials { get; set; }
        public int MaxMaterials { get; set; }

        // Pools hold names from enemies.json and patterns.json. An empty pool means "never one of these".
        public List<string> EnemyPool { get; set; }
        public List<string> BossPool { get; set; }
        public List<string> PatternPool { get; set; }
        public List<Survivor> Survivors { get; set; }

        /// <summary>Rolls what is in the zone this visit. <paramref name="zoneIndex"/> is how deep into the run it is (0 = first).</summary>
        public Zone Build(GameData data, BalanceConfig.BossSettings boss, int zoneIndex, Random rng)
        {
            string bossName = Pick(BossPool, rng);
            var zone = new Zone(Name, Description, boss.ChanceAt(zoneIndex),
                                bossName == null ? null : (Boss)data.Bosses[bossName].Spawn());

            string enemyName = Pick(EnemyPool, rng);
            if (enemyName != null) zone.Enemies.Add(data.Enemies[enemyName].Spawn());

            int materials = rng.Next(MinMaterials, MaxMaterials + 1);
            if (materials > 0) zone.Items.Add(new Material(materials));

            string patternName = Pick(PatternPool, rng);
            if (patternName != null) zone.Items.Add(data.Patterns[patternName].Copy());

            var survivor = Pick(Survivors, rng);
            if (survivor != null) zone.Survivors.Add(survivor);

            return zone;
        }

        /// <summary>Uniform pick; returns default (null) for an empty list.</summary>
        private static T Pick<T>(IReadOnlyList<T> items, Random rng)
        {
            return items.Count == 0 ? default : items[rng.Next(items.Count)];
        }
    }
}
