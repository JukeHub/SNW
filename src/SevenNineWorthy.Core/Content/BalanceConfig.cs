using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>Typed view over balance.json. Every tunable number lives here, not in logic.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class BalanceConfig
    {
        public StartingOrbiterSettings StartingOrbiter { get; set; }
        public StartingBotSettings StartingBot { get; set; }
        public PrintingSettings Printing { get; set; }
        public BatteryCosts Battery { get; set; }
        public ComfortSettings Comfort { get; set; }
        public BossSettings Boss { get; set; }

        public static BalanceConfig Load(string path)
        {
            var balance = JsonFile.Read<BalanceConfig>(path);
            if (balance.StartingBot.Abilities.Count == 0)
                throw new GameDataException("balance.json: startingBot needs at least one ability");
            return balance;
        }

        [JsonObject(ItemRequired = Required.Always)]
        public class StartingOrbiterSettings
        {
            public string Name { get; set; }
            public int Materials { get; set; }
        }

        [JsonObject(ItemRequired = Required.Always)]
        public class StartingBotSettings
        {
            public int Health { get; set; }
            public int Battery { get; set; }
            public List<Ability> Abilities { get; set; }
        }

        [JsonObject(ItemRequired = Required.Always)]
        public class PrintingSettings
        {
            public int BotMaterialCost { get; set; }
        }

        /// <summary>Battery cost of each action. Fighting isn't here: an Ability carries its own energy cost.</summary>
        [JsonObject(ItemRequired = Required.Always)]
        public class BatteryCosts
        {
            public int MovePerZone { get; set; }
            public int Salvage { get; set; }
            public int HelpSurvivor { get; set; }
            /// <summary>Per item sent by signal</summary>
            public int Transmit { get; set; }
            ///<summary>Per item packed into the launch pod (everything physical).</summary>
            public int LaunchPod { get; set; }
        }   

        [JsonObject(ItemRequired = Required.Always)]
        public class ComfortSettings
        {
            /// <summary>How many run launches a consumed media item's Effect lasts.</summary>
            public int EffectTurns { get; set; }

            /// <summary>How many Effects can run at once.</summary>
            public int MaxActiveEffects { get; set; }
        }

        [JsonObject(ItemRequired = Required.Always)]
        public class BossSettings
        {
            public double BaseChance { get; set; }
            public double ChancePerZone { get; set; }
            public double MaxChance { get; set; }

            /// <summary>Boss chance for the zone at this index in the run (0 = first zone).</summary>
            public double ChanceAt(int zoneIndex) => Math.Min(MaxChance, BaseChance + ChancePerZone * zoneIndex);
        }
    }
}
