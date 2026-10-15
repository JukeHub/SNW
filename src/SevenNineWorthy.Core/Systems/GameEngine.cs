using System;
using System.Collections.Generic;
using System.Linq;

namespace SevenNineWorthy.Core
{
    /// <summary>Top-level orchestrator: owns the Orbiter, runs the between-runs menu, hands runs to RunController.</summary>
    public class GameEngine
    {
        private readonly IGameView _view;
        private readonly IInputSource _input;
        private readonly ISaveStore _saveStore;
        private readonly GameData _data;
        private readonly BalanceConfig _balance;
        private readonly PrintService _printer;
        private readonly RunController _runController;
        private readonly int? _fixedSeed;

        private Orbiter _orbiter;

        /// <param name="fixedSeed">If set, every run uses this seed (for reproducing a run). Otherwise each run gets a fresh one.</param>
        public GameEngine(IGameView view, IInputSource input, ISaveStore saveStore,
                          GameData data, BalanceConfig balance, int? fixedSeed = null)
        {
            _view = view;
            _input = input;
            _saveStore = saveStore;
            _data = data;
            _balance = balance;
            _fixedSeed = fixedSeed;
            _printer = new PrintService(balance);
            _runController = new RunController(view, input, new ZoneGenerator(data, balance), balance);
        }

        public void Play()
        {
            _orbiter = _saveStore.Load();
            if (_orbiter == null)
            {
                _orbiter = Orbiter.CreateNew(_balance, _data.StartingMedia);
                _view.ShowText("A new orbiter hums to life above a quiet Earth.");
            }
            else
            {
                _view.ShowText("Orbiter systems restored.");
            }

            while (true)
            {
                _view.ShowText($"[Orbiter] Materials {_orbiter.Materials} | Survivors helped {_orbiter.SurvivorsHelped} | " +
                               $"Patterns on file {_orbiter.KnownPatternIds.Count}\n{BuffNarrator.Describe(_orbiter)}");

                var choice = Prompt.Choose(_view, _input, "Orbiter", new[]
                {
                    new MenuOption('1', $"Start run (print a bot: {_printer.BotCost} materials)"),
                    new MenuOption('2', "Browse collection"),
                    new MenuOption('3', "Print item"),
                    new MenuOption('4', "Upgrade orbiter"),
                    new MenuOption('Q', "Save & quit"),
                });

                switch (choice)
                {
                    case '1': StartRun(); break;
                    case '2': BrowseCollection(); break;
                    case '3': PrintFrom("Print item", p => p.Kind != PatternKind.OrbiterUpgrade); break;
                    case '4': UpgradeOrbiter(); break;
                    case 'Q':
                        _saveStore.Save(_orbiter);
                        _view.ShowText("Saved. The orbiter drifts on.");
                        return;
                }
            }
        }

        private void StartRun()
        {
            if (!_printer.TryPrintBot(_orbiter, out var bot))
            {
                _view.ShowText($"Not enough materials to print a bot (need {_printer.BotCost}).");
                return;
            }

            int seed = _fixedSeed ?? Environment.TickCount;
            _runController.Play(new Run(bot, seed), _orbiter);
            _saveStore.Save(_orbiter);
        }

        private void BrowseCollection()
        {
            while (true)
            {
                var media = _orbiter.Media;
                char choice = Prompt.Choose(_view, _input, "Collection", new[]
                {
                    new MenuOption('1', $"Books ({media.OfType(MediaType.Book).Count})"),
                    new MenuOption('2', $"Music ({media.OfType(MediaType.Music).Count})"),
                    new MenuOption('3', $"Movies ({media.OfType(MediaType.Movie).Count})"),
                    new MenuOption('4', $"Keepsakes ({_orbiter.KeepsakeIds.Count})"),
                    new MenuOption('5', $"Survivors met ({_orbiter.Contacts.Count})"),
                    new MenuOption('6', $"Places logged ({_orbiter.Locations.Count})"),
                    new MenuOption('B', "Back"),
                });

                switch (choice)
                {
                    case '1': ConsumeMedia(MediaType.Book, "Read something"); break;
                    case '2': ConsumeMedia(MediaType.Music, "Put something on"); break;
                    case '3': ConsumeMedia(MediaType.Movie, "Watch something"); break;
                    case '4': ShowList("Keepsakes", _orbiter.KeepsakeIds.Select(DescribeKeepsake)); break;
                    case '5': ShowList("Survivors met", _orbiter.Contacts.Select(c => $"{c.Name}: {c.Situation}")); break;
                    case '6': ShowList("Places logged", _orbiter.Locations.Select(l => $"{l.Name}: {l.Flavor}")); break;
                    case 'B': return;
                }
            }
        }

        private void ConsumeMedia(MediaType type, string title)
        {
            var item = Prompt.ChooseFrom(_view, _input, title, _orbiter.Media.OfType(type), m => m.Title);
            if (item == null) return;

            _orbiter.Media.Consume(item);
            _view.ShowText($"{item.Title}\n{item.Flavor}\n{BuffNarrator.Describe(_orbiter)}");
        }

        private void UpgradeOrbiter()
        {
            if (_orbiter.Upgrades.Count > 0)
                ShowList("Installed upgrades", _orbiter.Upgrades.Select(u => $"{u.Name}: {u.Effect}"));
            PrintFrom("Upgrade orbiter", p => p.Kind == PatternKind.OrbiterUpgrade);
        }

        /// <summary>Offers every known, not-yet-printed pattern that passes the filter, and prints the one chosen.</summary>
        private void PrintFrom(string title, Func<PatternTemplate, bool> filter)
        {
            var printable = _orbiter.KnownPatternIds
                .Where(_data.Patterns.ContainsKey)
                .Select(id => _data.Patterns[id])
                .Where(p => filter(p) && !_printer.AlreadyPrinted(_orbiter, p))
                .ToList();

            if (printable.Count == 0)
            {
                _view.ShowText("No new patterns on file for this. Bots can recover them on the surface and transmit them home.");
                return;
            }

            var pattern = Prompt.ChooseFrom(_view, _input, $"{title} ({_orbiter.Materials} materials)", printable,
                                            p => $"{p.Name} ({p.MaterialCost} materials)");
            if (pattern == null) return;

            _view.ShowText(_printer.TryPrint(_orbiter, pattern)
                ? $"Printed {pattern.Name}. {pattern.Effect}"
                : $"Not enough materials for {pattern.Name} (need {pattern.MaterialCost}).");
        }

        private string DescribeKeepsake(string id)
        {
            return _data.Loot.TryGetValue(id, out var loot) ? $"{loot.Name}: {loot.Flavor}" : id;
        }

        private void ShowList(string title, IEnumerable<string> lines)
        {
            var list = lines.ToList();
            _view.ShowText($"-- {title} --\n" + (list.Count == 0 ? "(nothing yet)" : string.Join("\n", list)));
        }
    }
}
