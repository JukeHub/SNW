using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>Something bought with materials that changes the orbiter for good.</summary>
    [JsonConverter(typeof(SubtypeConverter))]
    public abstract class Upgrade
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaterialCost { get; set; }

        /// <summary>Runs once, when the upgrade is bought.</summary>
        public abstract void Apply(Orbiter orbiter);
    }

    /// <summary>Makes every bot printed from now on sturdier.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class BotUpgrade : Upgrade
    {
        public int StatBonus { get; set; }

        public override void Apply(Orbiter orbiter) => orbiter.BotHealthBonus += StatBonus;
    }

    /// <summary>Makes the orbiter a nicer place to be.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class OrbiterUpgrade : Upgrade
    {
        public int ComfortBonus { get; set; }

        public override void Apply(Orbiter orbiter) => orbiter.ComfortLevel += ComfortBonus;
    }
}
