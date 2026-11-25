using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>The one currency: a stack of salvage the printer can use.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class Material : Item
    {
        public int Quantity { get; set; }

        public Material() : this(0) { }

        public Material(int quantity)
        {
            Name = "Materials";
            Description = "Salvage, sorted and ready for the printer.";
            Quantity = quantity;
        }

        public override string Describe() => $"{Name} x{Quantity}";
    }
}
