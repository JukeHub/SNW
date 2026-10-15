using System;
using System.Collections.Generic;

namespace SevenNineWorthy.Core
{
    /// <summary>The printed bot sent down to Earth for one run.</summary>
    public class Bot
    {
        public int Health { get; private set; }
        public int Battery { get; private set; }
        public int Attack { get; }

        // Cargo not yet sent home. Lost with the bot unless transmitted or launched.
        public int CarriedMaterials { get; set; }
        public List<LootTemplate> CarriedLoot { get; } = new List<LootTemplate>();
        public List<PatternTemplate> CarriedPatterns { get; } = new List<PatternTemplate>();

        public Bot(int health, int battery, int attack)
        {
            Health = health;
            Battery = battery;
            Attack = attack;
        }

        public bool IsDestroyed => Health <= 0;
        public bool IsOutOfBattery => Battery <= 0;
        public bool HasPhysicalCargo => CarriedMaterials > 0 || CarriedLoot.Count > 0;

        public bool CanAfford(int batteryCost) => Battery >= batteryCost;

        public bool TrySpendBattery(int cost)
        {
            if (!CanAfford(cost)) return false;
            Battery -= cost;
            return true;
        }

        public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);
    }
}
