using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SevenNineWorthy.Core
{
    /// <summary>
    /// Drives a single run: the zone loop, the action menu, and end-of-run handling.
    /// Progression reaches the orbiter only when the bot transmits or launches it
    /// (contacts and the location log are radioed up as they happen).
    /// </summary>
    public class RunController
    {
        private readonly IGameView _view;
        private readonly IInputSource _input;
        private readonly ZoneGenerator _zones;
        private readonly BalanceConfig.BatteryCosts _cost;
        private readonly CombatResolver _combat = new CombatResolver();

        public RunController(IGameView view, IInputSource input, ZoneGenerator zones, BalanceConfig balance)
        {
            _view = view;
            _input = input;
            _zones = zones;
            _cost = balance.Battery;
        }

        public void Play(Run run, Orbiter orbiter)
        {
            _view.ShowText($"Bot de-orbiting. (run seed {run.Seed})");
            EnterNewZone(run, orbiter);

            RunEndReason? ended = null;
            while (ended == null)
            {
                _view.ShowText(DescribeStatus(run));

                var actions = AvailableActions(run, orbiter);
                var options = actions.Select(a => a.Option).ToList();
                options.Add(new MenuOption('S', "Self-destruct (ends the run; anything not sent home is left here)"));

                char choice = Prompt.Choose(_view, _input, "What does the bot do?", options);
                if (choice == 'S')
                {
                    ended = RunEndReason.SelfDestructed;
                    continue;
                }

                actions.First(a => a.Option.Key == choice).Perform();
                ended = CheckBotLost(run.Bot);
            }

            EndRun(run, ended.Value);
        }

        private List<(MenuOption Option, Action Perform)> AvailableActions(Run run, Orbiter orbiter)
        {
            var bot = run.Bot;
            var zone = run.CurrentZone;
            var actions = new List<(MenuOption, Action)>();
            void Offer(string label, int cost, Action perform)
            {
                if (bot.CanAfford(cost))
                    actions.Add((new MenuOption((char)('1' + actions.Count), $"{label} ({cost} battery)"), perform));
            }

            if (zone.HasSalvage && !zone.IsContested)
                Offer("Salvage", _cost.Salvage, () => Salvage(run, orbiter));

            foreach (var hostile in zone.LivingHostiles)
            {
                int cost = hostile is Boss ? _cost.FightBoss : _cost.FightEnemy;
                string label = hostile is Boss ? $"Fight boss: {hostile.Name}" : $"Fight {hostile.Name}";
                Offer(label + ", per round", cost, () => Fight(bot, hostile, cost));
            }

            if (zone.HasSurvivorToHelp && !zone.IsContested)
                Offer($"Help {zone.Survivor.Name}", _cost.HelpSurvivor, () => Help(bot, zone, orbiter));

            if (bot.CarriedPatterns.Count > 0)
                Offer($"Transmit {Plural(bot.CarriedPatterns.Count, "pattern")} home",
                      _cost.TransmitPattern * bot.CarriedPatterns.Count, () => Transmit(bot, orbiter));

            if (bot.HasPhysicalCargo)
                Offer("Launch cargo pod home", _cost.LaunchPhysicalLoot, () => Launch(bot, orbiter));

            Offer("Move to the next zone", _cost.MovePerZone, () =>
            {
                run.Bot.TrySpendBattery(_cost.MovePerZone);
                EnterNewZone(run, orbiter);
            });

            return actions;
        }

        private void EnterNewZone(Run run, Orbiter orbiter)
        {
            var zone = _zones.Generate(run.ZonesEntered, run.Rng);
            run.CurrentZone = zone;
            run.ZonesEntered++;
            orbiter.LogLocation(zone.Template);
            _view.ShowText(DescribeZone(zone));
        }

        private void Salvage(Run run, Orbiter orbiter)
        {
            var bot = run.Bot;
            var zone = run.CurrentZone;
            bot.TrySpendBattery(_cost.Salvage);

            var text = new StringBuilder("The bot picks through what's left.");
            if (zone.MaterialsAvailable > 0)
            {
                bot.CarriedMaterials += zone.MaterialsAvailable;
                text.Append($"\n+{zone.MaterialsAvailable} materials.");
                zone.MaterialsAvailable = 0;
            }
            if (zone.Loot != null)
            {
                bot.CarriedLoot.Add(zone.Loot);
                text.Append($"\nFound: {zone.Loot.Name}. {zone.Loot.Flavor}");
                zone.Loot = null;
            }
            if (zone.Pattern != null)
            {
                bool known = orbiter.KnownPatternIds.Contains(zone.Pattern.Id) || bot.CarriedPatterns.Contains(zone.Pattern);
                if (!known) bot.CarriedPatterns.Add(zone.Pattern);
                text.Append(known
                    ? $"\nA copy of the {zone.Pattern.Name} pattern. You already have it."
                    : $"\nRecovered a pattern: {zone.Pattern.Name}.");
                zone.Pattern = null;
            }
            _view.ShowText(text.ToString());
        }

        /// <summary>Trades blows round by round until the enemy falls, the bot is lost, or the player disengages.</summary>
        private void Fight(Bot bot, Enemy enemy, int costPerRound)
        {
            while (true)
            {
                var options = new List<MenuOption>();
                if (bot.CanAfford(costPerRound))
                    options.Add(new MenuOption('1', $"Attack ({costPerRound} battery)"));
                options.Add(new MenuOption('D', "Disengage"));

                string title = $"{enemy.Name}: {enemy.Health} HP | Bot: {bot.Health} HP, {bot.Battery} battery";
                if (Prompt.Choose(_view, _input, title, options) == 'D') return;

                bot.TrySpendBattery(costPerRound);
                var round = _combat.ResolveRound(bot, enemy);
                _view.ShowText(enemy.IsDefeated
                    ? $"The bot hits for {round.DamageDealt}. {enemy.Name} goes still."
                    : $"The bot hits for {round.DamageDealt}. {enemy.Name} hits back for {round.DamageTaken}.");

                if (enemy.IsDefeated || CheckBotLost(bot) != null) return;
            }
        }

        private void Help(Bot bot, Zone zone, Orbiter orbiter)
        {
            bot.TrySpendBattery(_cost.HelpSurvivor);
            orbiter.RecordHelped(zone.Survivor);
            zone.SurvivorHelped = true;
            _view.ShowText($"The bot stays a while with {zone.Survivor.Name}: hauling, patching, mostly listening. " +
                           "Their name goes into the orbiter's contacts.");
        }

        private void Transmit(Bot bot, Orbiter orbiter)
        {
            bot.TrySpendBattery(_cost.TransmitPattern * bot.CarriedPatterns.Count);
            foreach (var pattern in bot.CarriedPatterns)
                orbiter.KnownPatternIds.Add(pattern.Id);
            _view.ShowText($"Transmitted: {string.Join(", ", bot.CarriedPatterns.Select(p => p.Name))}. Safe on the orbiter.");
            bot.CarriedPatterns.Clear();
        }

        private void Launch(Bot bot, Orbiter orbiter)
        {
            bot.TrySpendBattery(_cost.LaunchPhysicalLoot);
            orbiter.Materials += bot.CarriedMaterials;
            orbiter.KeepsakeIds.AddRange(bot.CarriedLoot.Select(l => l.Id));
            _view.ShowText($"The cargo pod climbs out of sight: {DescribeCargo(bot.CarriedMaterials, bot.CarriedLoot.Count, 0)}. Safe on the orbiter.");
            bot.CarriedMaterials = 0;
            bot.CarriedLoot.Clear();
        }

        private static RunEndReason? CheckBotLost(Bot bot)
        {
            if (bot.IsDestroyed) return RunEndReason.Destroyed;
            if (bot.IsOutOfBattery) return RunEndReason.BatteryDepleted;
            return null;
        }

        private void EndRun(Run run, RunEndReason reason)
        {
            _view.ShowText(reason switch
            {
                RunEndReason.Destroyed => "The bot's signal cuts out. It's gone.",
                RunEndReason.BatteryDepleted => "The bot's battery runs dry. It powers down where it stands.",
                _ => "The bot self-destructs. A small, bright flare, then nothing.",
            });
            LeaveCargoInZone(run);
        }

        /// <summary>
        /// Whatever the bot still carried stays in the zone where the run ended. It isn't tracked yet;
        /// a future "recover" feature would record it on that zone's LocationRecord here.
        /// </summary>
        private void LeaveCargoInZone(Run run)
        {
            var bot = run.Bot;
            if (bot.HasPhysicalCargo || bot.CarriedPatterns.Count > 0)
                _view.ShowText($"Left behind in {run.CurrentZone.Template.Name}: " +
                               DescribeCargo(bot.CarriedMaterials, bot.CarriedLoot.Count, bot.CarriedPatterns.Count) + ".");
        }

        private static string DescribeZone(Zone zone)
        {
            var text = new StringBuilder();
            text.AppendLine($"== Zone {zone.Index + 1}: {zone.Template.Name} ==");
            text.AppendLine(zone.Template.Description);
            foreach (var hostile in zone.Hostiles)
                text.AppendLine($"{(hostile is Boss ? "BOSS" : "Hostile")}: {hostile.Name}. {hostile.Flavor}");
            if (zone.Survivor != null)
                text.AppendLine($"Survivor: {zone.Survivor.Name}. {zone.Survivor.Situation}");
            return text.ToString().TrimEnd();
        }

        private static string DescribeStatus(Run run)
        {
            var bot = run.Bot;
            var zone = run.CurrentZone;
            string status = $"[Bot] Health {bot.Health} | Battery {bot.Battery} | Carrying: " +
                            DescribeCargo(bot.CarriedMaterials, bot.CarriedLoot.Count, bot.CarriedPatterns.Count);
            if (zone.IsContested && (zone.HasSalvage || zone.HasSurvivorToHelp))
                status += "\nHostiles here block salvaging and helping.";
            return status;
        }

        private static string DescribeCargo(int materials, int items, int patterns)
        {
            var parts = new List<string>();
            if (materials > 0) parts.Add($"{materials} materials");
            if (items > 0) parts.Add(Plural(items, "item"));
            if (patterns > 0) parts.Add(Plural(patterns, "pattern"));
            return parts.Count == 0 ? "nothing" : string.Join(", ", parts);
        }

        private static string Plural(int n, string noun) => n == 1 ? $"1 {noun}" : $"{n} {noun}s";
    }
}
