using System;

namespace SevenNineWorthy.Core
{
    /// <summary>
    /// One de-orbit, from launch to end. Transient: never serialized.
    /// Owns the run's single seeded RNG; every random roll in the run must use <see cref="Rng"/>.
    /// </summary>
    public class Run
    {
        public int Seed { get; }
        public Random Rng { get; }
        public Bot Bot { get; }

        public Zone CurrentZone { get; set; }

        /// <summary>Number of zones entered so far this run (also the next zone's index).</summary>
        public int ZonesEntered { get; set; }

        public Run(Bot bot, int seed)
        {
            Bot = bot;
            Seed = seed;
            Rng = new Random(seed); // The only `new Random` in the codebase.
        }
    }

    public enum RunEndReason { Destroyed, BatteryDepleted, SelfDestructed }
}
