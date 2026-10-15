using System;
using System.Collections.Generic;

namespace SevenNineWorthy.Core
{
    /// <summary>Rolls a Zone from a random template. All randomness comes from the run's RNG.</summary>
    public class ZoneGenerator
    {
        private readonly GameData _data;
        private readonly BalanceConfig _balance;

        public ZoneGenerator(GameData data, BalanceConfig balance)
        {
            _data = data;
            _balance = balance;
        }

        public Zone Generate(int zoneIndex, Random rng)
        {
            var template = Pick(_data.Zones, rng);

            var hostiles = new List<Enemy>();
            string enemyId = Pick(template.EnemyPool, rng);
            if (enemyId != null)
                hostiles.Add(new Enemy(_data.Enemies[enemyId]));
            if (template.BossPool.Count > 0 && rng.NextDouble() < _balance.Boss.ChanceAt(zoneIndex))
                hostiles.Add(new Boss(_data.Enemies[Pick(template.BossPool, rng)]));

            int materials = template.MaterialYield.Roll(rng);
            string lootId = Pick(template.LootPool, rng);
            string patternId = Pick(template.PatternPool, rng);

            return new Zone(template, zoneIndex, hostiles, materials,
                            lootId == null ? null : _data.Loot[lootId],
                            patternId == null ? null : _data.Patterns[patternId],
                            Pick(template.Survivors, rng));
        }

        /// <summary>Uniform pick; returns default (null) for an empty list.</summary>
        private static T Pick<T>(IReadOnlyList<T> items, Random rng)
        {
            return items.Count == 0 ? default : items[rng.Next(items.Count)];
        }
    }
}
