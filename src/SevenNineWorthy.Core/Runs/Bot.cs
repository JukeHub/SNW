using System;
using System.Collections.Generic;

namespace SevenNineWorthy.Core
{
    /// <summary>The printed bot sent down to Earth for one run.</summary>
    public class Bot
    {
        public int Health { get; private set; }
        public int Battery { get; private set; }
        public List<Ability> Abilities { get; }

        /// <summary>What the bot is carrying. Lost with the bot unless transferred to the orbiter.</summary>
        public List<Item> Items { get; } = new List<Item>();

        public Bot(int health, int battery, IEnumerable<Ability> abilities)
        {
            Health = health;
            Battery = battery;
            Abilities = new List<Ability>(abilities);
        }

        /// <summary>The activity diagram's "Bot operational?" check.</summary>
        public bool IsOperational => Health > 0 && Battery > 0;

        /// <summary>Spends the ability's energy cost and returns the damage dealt.</summary>
        public int UseAbility(Ability ability)
        {
            SpendBattery(ability.EnergyCost);
            return ability.Power;
        }

        public void SpendBattery(int amount) => Battery = Math.Max(0, Battery - amount);

        public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);
    }
}
