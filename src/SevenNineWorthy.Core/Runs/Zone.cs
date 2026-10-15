using System.Collections.Generic;
using System.Linq;

namespace SevenNineWorthy.Core
{
    /// <summary>A rolled instance of a ZoneTemplate: what is actually here this visit.</summary>
    public class Zone
    {
        public ZoneTemplate Template { get; }
        public int Index { get; }

        /// <summary>Enemies and any boss. A Boss is just an Enemy subtype.</summary>
        public List<Enemy> Hostiles { get; }

        // Salvageable contents; each is emptied (0 / null) once picked up.
        public int MaterialsAvailable { get; set; }
        public LootTemplate Loot { get; set; }
        public PatternTemplate Pattern { get; set; }

        /// <summary>Null when no survivor is here.</summary>
        public SurvivorTemplate Survivor { get; }
        public bool SurvivorHelped { get; set; }

        public Zone(ZoneTemplate template, int index, List<Enemy> hostiles,
                    int materials, LootTemplate loot, PatternTemplate pattern, SurvivorTemplate survivor)
        {
            Template = template;
            Index = index;
            Hostiles = hostiles;
            MaterialsAvailable = materials;
            Loot = loot;
            Pattern = pattern;
            Survivor = survivor;
        }

        public IEnumerable<Enemy> LivingHostiles => Hostiles.Where(h => !h.IsDefeated);

        /// <summary>Living hostiles stop the bot from salvaging or helping here.</summary>
        public bool IsContested => LivingHostiles.Any();

        public bool HasSalvage => MaterialsAvailable > 0 || Loot != null || Pattern != null;
        public bool HasSurvivorToHelp => Survivor != null && !SurvivorHelped;
    }
}
