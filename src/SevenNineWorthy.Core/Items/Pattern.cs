using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>A blueprint the orbiter's printer can turn into a media item.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class Pattern : Item
    {
        public int MaterialCost { get; set; }

        /// <summary>The item this pattern makes.</summary>
        public MediaItem PrintsInto { get; set; }

        public Item Print() => PrintsInto.Copy();

        /// <summary>
        /// A ppatern is only data, so the bot transmits it instead of launching it.
        /// </summary>
        public override int TransferCost(BalanceConfig.BatteryCosts cost) => cost.Transmit;

        public override string TransferRoute() => "signal";


        public override string Describe() => $"{Name} ({MaterialCost} materials to print). {Description}";
    }
}
