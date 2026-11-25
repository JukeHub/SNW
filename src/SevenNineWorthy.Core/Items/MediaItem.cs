using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>A book, album or film in the collection. Consuming one lifts the orbiter's comfort for a while.</summary>
    public abstract class MediaItem : Item
    {
        public string Title { get; set; }
        public int ComfortBoost { get; set; }

        /// <summary>A media item is known by its title, so the two are one value (and only Title is written to JSON).</summary>
        [JsonIgnore]
        public override string Name
        {
            get => Title;
            set => Title = value;
        }

        /// <summary>Returns the Effect of spending time with this item, lasting <paramref name="turns"/> runs.</summary>
        public abstract Effect Consume(int turns);

        protected Effect EffectNamed(string name, int turns)
        {
            return new Effect { Name = name, TurnsLeft = turns, ComfortBoost = ComfortBoost };
        }
    }

    [JsonObject(ItemRequired = Required.Always)]
    public class Book : MediaItem
    {
        public int Pages { get; set; }

        public override string Describe() => $"{Title} (book, {Pages} pages). {Description}";
        public override Effect Consume(int turns) => EffectNamed($"Read {Title}", turns);
    }

    [JsonObject(ItemRequired = Required.Always)]
    public class Music : MediaItem
    {
        public int Minutes { get; set; }

        public override string Describe() => $"{Title} (album, {Minutes} minutes). {Description}";
        public override Effect Consume(int turns) => EffectNamed($"Listened to {Title}", turns);
    }

    [JsonObject(ItemRequired = Required.Always)]
    public class Movie : MediaItem
    {
        public int Minutes { get; set; }

        public override string Describe() => $"{Title} (movie, {Minutes} minutes). {Description}";
        public override Effect Consume(int turns) => EffectNamed($"Watched {Title}", turns);
    }
}
