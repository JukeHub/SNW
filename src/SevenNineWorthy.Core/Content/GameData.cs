using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SevenNineWorthy.Core
{
    /// <summary>
    /// All content templates, loaded once at startup. The host supplies the directory,
    /// so the core never hardcodes where data lives.
    /// </summary>
    public class GameData
    {
        public IReadOnlyList<ZoneTemplate> Zones { get; }
        public IReadOnlyDictionary<string, EnemyTemplate> Enemies { get; }
        public IReadOnlyDictionary<string, PatternTemplate> Patterns { get; }
        public IReadOnlyDictionary<string, LootTemplate> Loot { get; }

        /// <summary>The media collection a brand-new orbiter starts with.</summary>
        public IReadOnlyList<MediaItem> StartingMedia { get; }

        private GameData(List<ZoneTemplate> zones, List<EnemyTemplate> enemies,
                         List<PatternTemplate> patterns, List<LootTemplate> loot, List<MediaItem> startingMedia)
        {
            Zones = zones;
            StartingMedia = startingMedia;
            Enemies = ById(enemies, e => e.Id, "enemies.json");
            Patterns = ById(patterns, p => p.Id, "patterns.json");
            Loot = ById(loot, l => l.Id, "loot.json");
            Validate();
        }

        public static GameData Load(string dataDirectory)
        {
            return new GameData(
                JsonFile.Read<List<ZoneTemplate>>(Path.Combine(dataDirectory, "zones.json")),
                JsonFile.Read<List<EnemyTemplate>>(Path.Combine(dataDirectory, "enemies.json")),
                JsonFile.Read<List<PatternTemplate>>(Path.Combine(dataDirectory, "patterns.json")),
                JsonFile.Read<List<LootTemplate>>(Path.Combine(dataDirectory, "loot.json")),
                JsonFile.Read<List<MediaItem>>(Path.Combine(dataDirectory, "starting_media.json")));
        }

        private static Dictionary<string, T> ById<T>(List<T> items, System.Func<T, string> id, string file)
        {
            var map = new Dictionary<string, T>();
            foreach (var item in items)
            {
                if (map.ContainsKey(id(item)))
                    throw new GameDataException($"Duplicate id '{id(item)}' in {file}");
                map[id(item)] = item;
            }
            return map;
        }

        /// <summary>Cross-file checks: every id a zone references must exist and be the right kind.</summary>
        private void Validate()
        {
            if (Zones.Count == 0)
                throw new GameDataException("zones.json must define at least one zone");

            foreach (var pattern in Patterns.Values)
                if ((pattern.Kind == PatternKind.Media) != pattern.MediaType.HasValue)
                    throw new GameDataException($"Pattern '{pattern.Id}': mediaType is required for media patterns and only for them");

            foreach (var zone in Zones)
            {
                if (zone.MaterialYield.Min < 0 || zone.MaterialYield.Min > zone.MaterialYield.Max)
                    throw new GameDataException($"Zone '{zone.Id}' has an invalid materialYield range");

                foreach (var id in zone.EnemyPool)
                    if (!Enemies.TryGetValue(id, out var e) || e.IsBoss)
                        throw new GameDataException($"Zone '{zone.Id}' enemyPool: '{id}' is not a non-boss enemy");
                foreach (var id in zone.BossPool)
                    if (!Enemies.TryGetValue(id, out var e) || !e.IsBoss)
                        throw new GameDataException($"Zone '{zone.Id}' bossPool: '{id}' is not a boss");
                foreach (var id in zone.LootPool.Where(id => !Loot.ContainsKey(id)))
                    throw new GameDataException($"Zone '{zone.Id}' lootPool: unknown loot '{id}'");
                foreach (var id in zone.PatternPool.Where(id => !Patterns.ContainsKey(id)))
                    throw new GameDataException($"Zone '{zone.Id}' patternPool: unknown pattern '{id}'");
            }
        }
    }
}
