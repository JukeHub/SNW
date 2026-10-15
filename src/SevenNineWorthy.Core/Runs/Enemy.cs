using System;

namespace SevenNineWorthy.Core
{
    public class Enemy
    {
        public string Name { get; }
        public int Health { get; private set; }
        public int Attack { get; }
        public string Flavor { get; }

        public Enemy(EnemyTemplate template)
        {
            Name = template.Name;
            Health = template.Health;
            Attack = template.Attack;
            Flavor = template.Flavor;
        }

        public bool IsDefeated => Health <= 0;

        public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);
    }

    /// <summary>A tougher enemy. Same combat mechanic; the type marks it as the zone's boss.</summary>
    public class Boss : Enemy
    {
        public Boss(EnemyTemplate template) : base(template) { }
    }
}
