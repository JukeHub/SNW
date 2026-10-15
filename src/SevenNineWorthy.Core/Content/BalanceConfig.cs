using System;
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
        public BossSettings Boss { get; set; }

        public static BalanceConfig Load(string path) => JsonFile.Read<BalanceConfig>(path);

        [JsonObject(ItemRequired = Required.Always)]
        public class StartingOrbiterSettings
        {
            public int Materials { get; set; }
        }

        [JsonObject(ItemRequired = Required.Always)]
        public class StartingBotSettings
        {
            public int Health { get; set; }
            public int Battery { get; set; }
            public int Attack { get; set; }
        }

        [JsonObject(ItemRequired = Required.Always)]
        public class PrintingSettings
        {
            public int BotMaterialCost { get; set; }
        }

        [JsonObject(ItemRequired = Required.Always)]
        public class BatteryCosts
        {
            public int MovePerZone { get; set; }
            public int Salvage { get; set; }
            public int FightEnemy { get; set; }
            public int FightBoss { get; set; }
            public int HelpSurvivor { get; set; }
            public int TransmitPattern { get; set; }
            public int LaunchPhysicalLoot { get; set; }
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
