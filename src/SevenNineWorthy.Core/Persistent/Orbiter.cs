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
        public string Name { get; set; }

        /// <summary>Extra battery every printed bot starts with.</summary>
        public int ComfortLevel { get; set; }

        /// <summary>Extra health every printed bot starts with, from bot upgrades.</summary>
        public int BotHealthBonus { get; set; }

        /// <summary>The single stack of the one currency.</summary>
        public Material Materials { get; set; } = new Material();

        /// <summary>Stored media and patterns.</summary>
        public List<Item> Items { get; set; } = new List<Item>();
        public List<Upgrade> Upgrades { get; set; } = new List<Upgrade>();
        public List<Effect> Effects { get; set; } = new List<Effect>();
        public List<Contact> Contacts { get; set; } = new List<Contact>();

        public static Orbiter CreateNew(BalanceConfig balance, IEnumerable<MediaItem> startingMedia)
        {
            var orbiter = new Orbiter { Name = balance.StartingOrbiter.Name };
            orbiter.Store(new Material(balance.StartingOrbiter.Materials));
            foreach (var item in startingMedia)
                orbiter.Store(item.Copy());
            return orbiter;
        }

        /// <summary>Prints the pattern if materials cover it, and stores the result. Returns null (and changes nothing) if they don't.</summary>
        public Item Print(Pattern pattern)
        {
            if (Materials.Quantity < pattern.MaterialCost) return null;

            Materials.Quantity -= pattern.MaterialCost;
            var item = pattern.Print();
            Store(item);
            return item;
        }

        /// <summary>Prints a bot and sends it down. Returns null (and changes nothing) if materials don't cover the bot.</summary>
        public Run StartRun(GameData data, BalanceConfig balance, int seed)
        {
            if (Materials.Quantity < balance.Printing.BotMaterialCost) return null;

            Materials.Quantity -= balance.Printing.BotMaterialCost;
            var bot = new Bot(balance.StartingBot.Health + BotHealthBonus,
                              balance.StartingBot.Battery + ComfortLevel,
                              balance.StartingBot.Abilities);

            // Each launch uses up one turn of every effect. An expired effect takes its comfort with it.
            foreach (var effect in Effects)
                effect.TurnsLeft--;
            foreach (var expired in Effects.Where(e => e.TurnsLeft <= 0).ToList())
            {
                ComfortLevel -= expired.ComfortBoost;
                Effects.Remove(expired);
            }

            return new Run(bot, seed, data, balance);
        }

        /// <summary>Materials join the single stack. Anything else joins the stored list, unless one just like it is already there.</summary>
        public void Store(Item item)
        {
            if (item is Material material)
                Materials.Quantity += material.Quantity;
            else if (!Has(item))
                Items.Add(item);
        }

        public bool Has(Item item) => Items.Any(i => i.Name == item.Name);

        /// <summary>Buys and applies the upgrade if materials cover it. Returns false (and changes nothing) if they don't.</summary>
        public bool Install(Upgrade upgrade)
        {
            if (Materials.Quantity < upgrade.MaterialCost) return false;

            Materials.Quantity -= upgrade.MaterialCost;
            Upgrades.Add(upgrade);
            upgrade.Apply(this);
            return true;
        }

        /// <summary>
        /// Spends time with a media item: its Effect is added and comfort rises by its boost.
        /// Returns null (and changes nothing) if that effect is still running or <paramref name="maxEffects"/> already are.
        /// </summary>
        public Effect Consume(MediaItem item, int turns, int maxEffects)
        {
            var effect = item.Consume(turns);
            if (Effects.Count >= maxEffects || Effects.Any(e => e.Name == effect.Name)) return null;

            Effects.Add(effect);
            ComfortLevel += effect.ComfortBoost;
            return effect;
        }

        /// <summary>Records a helped survivor, or updates where they were last seen.</summary>
        public void AddContact(Survivor survivor, Zone zone)
        {
            var contact = Contacts.FirstOrDefault(c => c.Name == survivor.Name);
            if (contact == null)
                Contacts.Add(contact = new Contact { Name = survivor.Name });
            contact.LastKnownZone = zone.Name;
        }
    }
}
