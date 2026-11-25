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

        /// <summary>A separate Item with the same contents, so a definition can be handed out more than once.</summary>
        public Item Copy() => (Item)MemberwiseClone();
    }
}
