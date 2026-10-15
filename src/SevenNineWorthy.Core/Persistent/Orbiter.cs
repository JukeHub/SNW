using System.Collections.Generic;
using System.Linq;

namespace SevenNineWorthy.Core
{
    /// <summary>
    /// The save root. Everything that survives between runs hangs off this object,
    /// and it is the only thing ISaveStore persists.
    /// </summary>
    public class Orbiter
    {
        public int Materials { get; set; }

        /// <summary>Lifetime counter, kept for a future ending or prestige layer to read.</summary>
        public int SurvivorsHelped { get; set; }

        /// <summary>Ids of patterns transmitted home, from patterns.json.</summary>
        public List<string> KnownPatternIds { get; set; } = new List<string>();

        /// <summary>Ids of physical loot launched home, from loot.json.</summary>
        public List<string> KeepsakeIds { get; set; } = new List<string>();

        public List<Upgrade> Upgrades { get; set; } = new List<Upgrade>();
        public List<string> Comforts { get; set; } = new List<string>();
        public MediaCollection Media { get; set; } = new MediaCollection();
        public List<SurvivorContact> Contacts { get; set; } = new List<SurvivorContact>();
        public List<LocationRecord> Locations { get; set; } = new List<LocationRecord>();

        public static Orbiter CreateNew(BalanceConfig balance, IEnumerable<MediaItem> startingMedia)
        {
            var orbiter = new Orbiter { Materials = balance.StartingOrbiter.Materials };
            foreach (var item in startingMedia)
                orbiter.Media.Add(new MediaItem { Type = item.Type, Title = item.Title, Flavor = item.Flavor });
            return orbiter;
        }

        public void LogLocation(ZoneTemplate zone)
        {
            if (Locations.All(l => l.Name != zone.Name))
                Locations.Add(new LocationRecord { Name = zone.Name, Flavor = zone.Description });
        }

        public void RecordHelped(SurvivorTemplate survivor)
        {
            SurvivorsHelped++;
            if (Contacts.All(c => c.Name != survivor.Name))
                Contacts.Add(new SurvivorContact { Name = survivor.Name, Situation = survivor.Situation });
        }
    }
}
