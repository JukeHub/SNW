using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SevenNineWorthy.Core
{
    /// <summary>
    /// All content, loaded once at startup straight into the model classes. The host supplies
    /// the directory, so the core never hardcodes where data lives.
    /// </summary>
    public class GameData
    {
        public IReadOnlyList<ZoneTemplate> Zones { get; }

        // Looked up by name, which is how zones.json refers to them.
        public IReadOnlyDictionary<string, Enemy> Enemies { get; }
        public IReadOnlyDictionary<string, Boss> Bosses { get; }
        public IReadOnlyDictionary<string, Pattern> Patterns { get; }

        /// <summary>Every upgrade the orbiter can buy.</summary>
        public IReadOnlyList<Upgrade> Upgrades { get; }

        /// <summary>The media collection a brand-new orbiter starts with.</summary>
        public IReadOnlyList<MediaItem> StartingMedia { get; }

        private GameData(List<ZoneTemplate> zones, List<Enemy> enemies, List<Pattern> patterns,
                         List<Upgrade> upgrades, List<MediaItem> startingMedia)
        {
            Zones = zones;
            Upgrades = upgrades;
            StartingMedia = startingMedia;

            // enemies.json holds both kinds; a "type": "boss" entry was loaded as a Boss.
            var bosses = enemies.OfType<Boss>().ToList();
            Bosses = ByName(bosses, b => b.Name, "enemies.json");
            Enemies = ByName(enemies.Except(bosses).ToList(), e => e.Name, "enemies.json");
            Patterns = ByName(patterns, p => p.Name, "patterns.json");
            ByName(upgrades, u => u.Name, "upgrades.json");
            ByName(startingMedia, m => m.Title, "starting_media.json");
            Validate();
        }

        public static GameData Load(string dataDirectory)
        {
            return new GameData(
                JsonFile.Read<List<ZoneTemplate>>(Path.Combine(dataDirectory, "zones.json")),
                JsonFile.Read<List<Enemy>>(Path.Combine(dataDirectory, "enemies.json")),
                JsonFile.Read<List<Pattern>>(Path.Combine(dataDirectory, "patterns.json")),
                JsonFile.Read<List<Upgrade>>(Path.Combine(dataDirectory, "upgrades.json")),
                JsonFile.Read<List<MediaItem>>(Path.Combine(dataDirectory, "starting_media.json")));
        }

        private static Dictionary<string, T> ByName<T>(List<T> items, System.Func<T, string> name, string file)
        {
            var map = new Dictionary<string, T>();
            foreach (var item in items)
            {
                if (map.ContainsKey(name(item)))
                    throw new GameDataException($"Duplicate name '{name(item)}' in {file}");
                map[name(item)] = item;
            }
            return map;
        }

        /// <summary>Cross-file checks: every name a zone refers to must exist and be the right kind.</summary>
        private void Validate()
        {
            if (Zones.Count == 0)
                throw new GameDataException("zones.json must define at least one zone");

            foreach (var zone in Zones)
            {
                if (zone.MinMaterials < 0 || zone.MinMaterials > zone.MaxMaterials)
                    throw new GameDataException($"Zone '{zone.Name}' has an invalid minMaterials/maxMaterials range");

                foreach (var name in zone.EnemyPool.Where(name => !Enemies.ContainsKey(name)))
                    throw new GameDataException($"Zone '{zone.Name}' enemyPool: '{name}' is not a non-boss enemy");
                foreach (var name in zone.BossPool.Where(name => !Bosses.ContainsKey(name)))
                    throw new GameDataException($"Zone '{zone.Name}' bossPool: '{name}' is not a boss");
                foreach (var name in zone.PatternPool.Where(name => !Patterns.ContainsKey(name)))
                    throw new GameDataException($"Zone '{zone.Name}' patternPool: unknown pattern '{name}'");
            }
        }
    }
}
