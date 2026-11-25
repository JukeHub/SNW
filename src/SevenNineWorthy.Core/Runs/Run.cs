using System;
using System.Collections.Generic;

namespace SevenNineWorthy.Core
{
    /// <summary>
    /// One de-orbit, from launch to end. Transient: never serialized.
    /// Owns the run's single seeded RNG; every random roll in the run comes from it.
    /// </summary>
    public class Run
    {
        public int Seed { get; }
        public Bot Bot { get; }

        /// <summary>Every zone visited so far, in order. The last one is where the bot is now.</summary>
        public List<Zone> Zones { get; } = new List<Zone>();

        /// <summary>Number of zones entered so far (also the next zone's index).</summary>
        public int ZoneIndex { get; private set; }

        public bool IsOver { get; private set; }

        private readonly Random _rng;
        private readonly GameData _data;
        private readonly BalanceConfig _balance;

        public Run(Bot bot, int seed, GameData data, BalanceConfig balance)
        {
            Bot = bot;
            Seed = seed;
            _rng = new Random(seed); // The only `new Random` in the codebase.
            _data = data;
            _balance = balance;
        }

        /// <summary>Generates the next zone, rolls for its boss, and moves the run into it.</summary>
        public Zone AdvanceZone()
        {
            var template = _data.Zones[_rng.Next(_data.Zones.Count)];
            var zone = template.Build(_data, _balance.Boss, ZoneIndex, _rng);
            zone.RollForBoss(_rng);

            Zones.Add(zone);
            ZoneIndex++;
            return zone;
        }

        /// <summary>
        /// Ends the run. Whatever the bot still carried goes with it; this is the one place
        /// a future "recover what was left behind" feature would change.
        /// </summary>
        public void End()
        {
            IsOver = true;
            Bot.Items.Clear();
        }
    }
}
