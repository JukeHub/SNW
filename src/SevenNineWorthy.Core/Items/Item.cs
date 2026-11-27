using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>
    /// Anything a bot can carry home. An Item is in one place at a time:
    /// offered by a Zone, carried by a Bot, or stored by the Orbiter.
    /// </summary>
    [JsonConverter(typeof(SubtypeConverter))]
    public abstract class Item
    {
        public virtual string Name { get; set; }
        public string Description { get; set; }

        /// <summary>One line saying what this is.</summary>
        public abstract string Describe();

        /// <summary>
        /// BAttery it takes to get this up to the orbiter. Most things are physical and
        /// ride the launch pod; a subclass that travels another way overrides this. 
        /// </summary>
        public virtual int TransferCost(BalanceConfig.BatteryCosts cost) => cost.LaunchPod;

        ///<summary>
        /// How this travels to the oribiter. For the transfer report.
        /// </summary>
        public virtual string TransferRoute() => "launch pod";

        /// <summary>A separate Item with the same contents, so a definition can be handed out more than once.</summary>
        public Item Copy() => (Item)MemberwiseClone();
    }
}
