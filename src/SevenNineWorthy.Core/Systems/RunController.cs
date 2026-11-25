using System;
using System.Collections.Generic;
using System.Linq;

namespace SevenNineWorthy.Core
{
    /// <summary>
    /// The bottom half of the activity diagram: one run on the surface, from de-orbit to end.
    /// Menus and sequencing only; the Run, Zone, Bot and Enemy do the work. Nothing reaches
    /// the orbiter unless the bot transfers it (contacts are radioed up as they are made).
    /// </summary>
    public class RunController
    {
        private readonly IGameView _view;
        private readonly IInputSource _input;
        private readonly BalanceConfig.BatteryCosts _cost;

        public RunController(IGameView view, IInputSource input, BalanceConfig balance)
        {
            _view = view;
            _input = input;
            _cost = balance.Battery;
        }

        public void Play(Run run, Orbiter orbiter)
        {
            var bot = run.Bot;
            _view.ShowText($"Bot de-orbiting. (run seed {run.Seed})");
            _view.ShowText(DescribeZone(run.AdvanceZone()));

            while (!run.IsOver)
            {
                _view.ShowText($"Zone {run.ZoneIndex} | Bot: {bot.Health} HP, {bot.Battery} battery | Carrying: {(bot.Items.Count == 0 ? "nothing" : Plural(bot.Items.Count, "item"))}");

                var actions = AvailableActions(run, orbiter);
                var options = actions.Select(a => a.Option).ToList();
                options.Add(new MenuOption('S', "Self destruct (ends the run; anything not transferred is lost)"));

                char choice = Prompt.Choose(_view, _input, "What does the bot do?", options);
                if (choice != 'S')
                    actions.First(a => a.Option.Key == choice).Perform();

                // "Bot operational?" No, or the player chose to self destruct: the run ends, the same way either way.
                if (choice == 'S' || !bot.IsOperational)
                {
                    run.End();
                    _view.ShowText($"The bot's signal is gone. Run over after {Plural(run.ZoneIndex, "zone")}. " +
                                   "Only what it transferred made it home.");
                }
            }
        }

        /// <summary>Every choice the bot can afford right now, numbered in order.</summary>
        private List<(MenuOption Option, Action Perform)> AvailableActions(Run run, Orbiter orbiter)
        {
            var bot = run.Bot;
            var zone = run.Zones.Last();
            var actions = new List<(MenuOption, Action)>();
            void Offer(string label, int cost, Action perform)
            {
                if (bot.Battery >= cost)
                    actions.Add((new MenuOption((char)('1' + actions.Count), $"{label} ({cost} battery)"), perform));
            }

            foreach (var action in zone.ListActions())
            {
                switch (action)
                {
                    case ZoneAction.Salvage:
                        Offer("Salvage", _cost.Salvage, () => Salvage(bot, zone));
                        break;
                    case ZoneAction.Fight:
                        var enemy = zone.LivingEnemies.First();
                        foreach (var ability in bot.Abilities)
                            Offer($"Fight {enemy.Name} with {ability.Name}", ability.EnergyCost, () => Fight(bot, enemy, ability));
                        break;
                    case ZoneAction.Help:
                        var survivor = zone.Survivors[0];
                        Offer($"Help {survivor.Name}", _cost.HelpSurvivor, () => Help(bot, zone, survivor, orbiter));
                        break;
                }
            }

            if (bot.Items.Count > 0)
                Offer("Transfer loot to the orbiter", _cost.Transfer, () => Transfer(bot, orbiter));

            Offer("Move to a new zone", _cost.MovePerZone, () =>
            {
                bot.SpendBattery(_cost.MovePerZone);
                _view.ShowText(DescribeZone(run.AdvanceZone()));
            });

            return actions;
        }

        private void Salvage(Bot bot, Zone zone)
        {
            bot.SpendBattery(_cost.Salvage);
            bot.Items.AddRange(zone.Items);
            _view.ShowText("The bot picks through what's left. Found:\n" + DescribeAll(zone.Items));
            zone.Items.Clear();
        }

        /// <summary>One exchange of blows.</summary>
        private void Fight(Bot bot, Enemy enemy, Ability ability)
        {
            int dealt = bot.UseAbility(ability);
            enemy.TakeDamage(dealt);
            if (enemy.IsDefeated)
            {
                _view.ShowText($"{ability.Name} hits for {dealt}. {enemy.Name} goes still.");
                return;
            }

            int taken = enemy.Attack();
            bot.TakeDamage(taken);
            _view.ShowText($"{ability.Name} hits for {dealt}. {enemy.Name} ({enemy.Health} HP left) hits back for {taken}.");
        }

        private void Help(Bot bot, Zone zone, Survivor survivor, Orbiter orbiter)
        {
            bot.SpendBattery(_cost.HelpSurvivor);
            zone.Survivors.Remove(survivor);
            orbiter.AddContact(survivor, zone);
            _view.ShowText($"The bot stays a while with {survivor.Name}: hauling, patching, mostly listening. " +
                           "Their name goes into the orbiter's contacts.");
        }

        private void Transfer(Bot bot, Orbiter orbiter)
        {
            bot.SpendBattery(_cost.Transfer);
            _view.ShowText("Sent up, by signal and by pod. Safe on the orbiter:\n" + DescribeAll(bot.Items));
            foreach (var item in bot.Items)
                orbiter.Store(item);
            bot.Items.Clear();
        }

        private static string DescribeZone(Zone zone)
        {
            var lines = new List<string> { $"== {zone.Name} ==", zone.Description };
            lines.AddRange(zone.Enemies.Select(e => $"Hostile: {e.Name} ({e.Health} HP). {e.Description}"));
            lines.AddRange(zone.Survivors.Select(s => $"Survivor: {s.Name}. {s.Need}"));
            return string.Join("\n", lines);
        }

        private static string DescribeAll(List<Item> items) => string.Join("\n", items.Select(i => "- " + i.Describe()));

        private static string Plural(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
    }
}
