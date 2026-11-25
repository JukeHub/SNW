using System;
using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    [JsonConverter(typeof(SubtypeConverter))]
    [JsonObject(ItemRequired = Required.Always)]
    public class Enemy
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Health { get; set; }
        public int Damage { get; set; }

        [JsonIgnore]
        public bool IsDefeated => Health <= 0;

        /// <summary>Returns the damage this enemy deals in one exchange.</summary>
        public virtual int Attack() => Damage;

        public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);

        /// <summary>A fresh enemy for a zone, so the definition in GameData is never the one that gets hurt.</summary>
        public Enemy Spawn() => (Enemy)MemberwiseClone();
    }

    /// <summary>A tougher enemy that hits harder than its base damage.</summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class Boss : Enemy
    {
        public double Multiplier { get; set; }

        public override int Attack() => (int)(base.Attack() * Multiplier);
    }
}
