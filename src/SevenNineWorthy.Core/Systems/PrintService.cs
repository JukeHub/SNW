using System.Linq;

namespace SevenNineWorthy.Core
{
    /// <summary>Turns orbiter materials into things: validates cost, deducts it, produces the item.</summary>
    public class PrintService
    {
        private readonly BalanceConfig _balance;

        public PrintService(BalanceConfig balance)
        {
            _balance = balance;
        }

        public int BotCost => _balance.Printing.BotMaterialCost;

        public bool TryPrintBot(Orbiter orbiter, out Bot bot)
        {
            bot = null;
            if (orbiter.Materials < BotCost) return false;

            orbiter.Materials -= BotCost;
            var stats = _balance.StartingBot;
            bot = new Bot(stats.Health, stats.Battery, stats.Attack);
            return true;
        }

        /// <summary>Each pattern prints once: an upgrade is installed once, a comfort or media item kept once.</summary>
        public bool AlreadyPrinted(Orbiter orbiter, PatternTemplate pattern)
        {
            switch (pattern.Kind)
            {
                case PatternKind.Media: return orbiter.Media.Contains(pattern.Name);
                case PatternKind.Comfort: return orbiter.Comforts.Contains(pattern.Name);
                default: return orbiter.Upgrades.Any(u => u.Name == pattern.Name);
            }
        }

        /// <summary>Returns false (and changes nothing) if the orbiter can't afford it.</summary>
        public bool TryPrint(Orbiter orbiter, PatternTemplate pattern)
        {
            if (orbiter.Materials < pattern.MaterialCost) return false;
            orbiter.Materials -= pattern.MaterialCost;

            switch (pattern.Kind)
            {
                case PatternKind.Media:
                    orbiter.Media.Add(new MediaItem { Type = pattern.MediaType.Value, Title = pattern.Name, Flavor = pattern.Effect });
                    break;
                case PatternKind.Comfort:
                    orbiter.Comforts.Add(pattern.Name);
                    break;
                case PatternKind.OrbiterUpgrade:
                    orbiter.Upgrades.Add(new Upgrade { Name = pattern.Name, Effect = pattern.Effect });
                    break;
            }
            return true;
        }
    }
}
