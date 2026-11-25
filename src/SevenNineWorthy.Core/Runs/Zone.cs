using System;
using System.Collections.Generic;
using System.Linq;

namespace SevenNineWorthy.Core
{
    /// <summary>The things a bot can do in a zone, apart from leaving it.</summary>
    public enum ZoneAction { Salvage, Fight, Help }

    /// <summary>One place on the surface, as the bot finds it this visit.</summary>
    public class Zone
    {
        public string Name { get; }
        public string Description { get; }
        public double BossChance { get; }

        public List<Enemy> Enemies { get; } = new List<Enemy>();
        public List<Survivor> Survivors { get; } = new List<Survivor>();

        /// <summary>What is here to salvage.</summary>
        public List<Item> Items { get; } = new List<Item>();

        // The boss that turns up if the roll succeeds. Null when this kind of zone has none.
        private readonly Boss _lurkingBoss;

        public Zone(string name, string description, double bossChance, Boss lurkingBoss)
        {
            Name = name;
            Description = description;
            BossChance = bossChance;
            _lurkingBoss = lurkingBoss;
        }

        public IEnumerable<Enemy> LivingEnemies => Enemies.Where(e => !e.IsDefeated);

        /// <summary>Which actions are valid here right now.</summary>
        public List<ZoneAction> ListActions()
        {
            var actions = new List<ZoneAction>();
            if (Items.Count > 0) actions.Add(ZoneAction.Salvage);
            if (LivingEnemies.Any()) actions.Add(ZoneAction.Fight);
            if (Survivors.Count > 0) actions.Add(ZoneAction.Help);
            return actions;
        }

        /// <summary>Rolls against BossChance. On success the boss joins the zone's enemies.</summary>
        public bool RollForBoss(Random rng)
        {
            if (_lurkingBoss == null || rng.NextDouble() >= BossChance) return false;
            Enemies.Add(_lurkingBoss);
            return true;
        }
    }
}
