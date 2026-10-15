namespace SevenNineWorthy.Core
{
    /// <summary>
    /// Resolves bot-vs-enemy combat. Today that's one physical attack; future ability types
    /// should be resolved here too rather than in the controller.
    /// </summary>
    public class CombatResolver
    {
        /// <summary>One exchange: the bot strikes first, and a surviving enemy strikes back.</summary>
        public CombatRound ResolveRound(Bot bot, Enemy enemy)
        {
            int dealt = System.Math.Min(bot.Attack, enemy.Health);
            enemy.TakeDamage(dealt);

            int taken = enemy.IsDefeated ? 0 : enemy.Attack;
            bot.TakeDamage(taken);

            return new CombatRound(dealt, taken);
        }
    }

    public class CombatRound
    {
        public int DamageDealt { get; }
        public int DamageTaken { get; }

        public CombatRound(int damageDealt, int damageTaken)
        {
            DamageDealt = damageDealt;
            DamageTaken = damageTaken;
        }
    }
}
