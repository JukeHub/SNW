using System;
using System.Collections.Generic;
using System.Linq;

namespace SevenNineWorthy.Core
{
    /// <summary>
    /// The top half of the activity diagram: load, the Orbiter menu, launch, save.
    /// Menus and sequencing only; the Orbiter does the work.
    /// </summary>
    public class GameEngine
    {
        private readonly IGameView _view;
        private readonly IInputSource _input;
        private readonly ISaveStore _saveStore;
        private readonly GameData _data;
        private readonly BalanceConfig _balance;
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
            _runController = new RunController(view, input, balance);
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
                _view.ShowText(DescribeOrbiter());

                var choice = Prompt.Choose(_view, _input, _orbiter.Name, new[]
                {
                    new MenuOption('1', $"Launch (print a bot: {_balance.Printing.BotMaterialCost} materials)"),
                    new MenuOption('2', "Browse collection"),
                    new MenuOption('3', "Print or upgrade"),
                    new MenuOption('Q', "Save & quit"),
                });

                switch (choice)
                {
                    case '1': Launch(); break;
                    case '2': BrowseCollection(); break;
                    case '3': PrintOrUpgrade(); break;
                    case 'Q':
                        _saveStore.Save(_orbiter);
                        _view.ShowText("Saved. The orbiter drifts on.");
                        return;
                }
            }
        }

        private string DescribeOrbiter()
        {
            string mind = _orbiter.Effects.Count == 0
                ? "Nothing's playing. The hum of the orbiter fills the quiet."
                : string.Join(" ", _orbiter.Effects.Select(e => $"{e.Name} ({Plural(e.TurnsLeft, "launch", "launches")} left)."));

            return $"[{_orbiter.Name}] {_orbiter.Materials.Describe()} | Comfort {_orbiter.ComfortLevel} | " +
                   $"Contacts {_orbiter.Contacts.Count}\nState of mind: {mind}";
        }

        private void Launch()
        {
            int seed = _fixedSeed ?? Environment.TickCount;
            var run = _orbiter.StartRun(_data, _balance, seed);
            if (run == null)
            {
                _view.ShowText($"Not enough materials to print a bot (need {_balance.Printing.BotMaterialCost}).");
                return;
            }

            _runController.Play(run, _orbiter);
            _saveStore.Save(_orbiter);
        }

        private void BrowseCollection()
        {
            while (true)
            {
                char choice = Prompt.Choose(_view, _input, "Collection", new[]
                {
                    new MenuOption('1', $"Books ({Stored<Book>().Count})"),
                    new MenuOption('2', $"Music ({Stored<Music>().Count})"),
                    new MenuOption('3', $"Movies ({Stored<Movie>().Count})"),
                    new MenuOption('4', $"Patterns ({Stored<Pattern>().Count})"),
                    new MenuOption('5', $"Contacts ({_orbiter.Contacts.Count})"),
                    new MenuOption('B', "Back"),
                });

                switch (choice)
                {
                    case '1': Consume(Stored<Book>(), "Read something"); break;
                    case '2': Consume(Stored<Music>(), "Put something on"); break;
                    case '3': Consume(Stored<Movie>(), "Watch something"); break;
                    case '4': ShowList("Patterns", Stored<Pattern>().Select(p => p.Describe())); break;
                    case '5': ShowList("Contacts", _orbiter.Contacts.Select(c => $"{c.Name}, last known at: {c.LastKnownZone}")); break;
                    case 'B': return;
                }
            }
        }

        /// <summary>The orbiter's stored Items of one kind.</summary>
        private List<T> Stored<T>() where T : Item => _orbiter.Items.OfType<T>().ToList();

        private void Consume<T>(List<T> shelf, string title) where T : MediaItem
        {
            var item = Prompt.ChooseFrom(_view, _input, title, shelf, m => m.Title);
            if (item == null) return;

            var effect = _orbiter.Consume(item, _balance.Comfort.EffectTurns, _balance.Comfort.MaxActiveEffects);
            _view.ShowText(item.Describe() + "\n" + (effect == null
                ? "It doesn't land right now. Either it's still with you, or your head is full already."
                : $"{effect.Name}. Comfort +{effect.ComfortBoost} for the next {Plural(effect.TurnsLeft, "launch", "launches")}."));
        }

        /// <summary>One menu for both: stored patterns that haven't been printed, and upgrades that haven't been bought.</summary>
        private void PrintOrUpgrade()
        {
            var labels = new List<string>();
            var actions = new List<Action>();

            foreach (var pattern in Stored<Pattern>().Where(p => !_orbiter.Has(p.PrintsInto)))
            {
                labels.Add($"Print {pattern.PrintsInto.Title} ({pattern.MaterialCost} materials)");
                actions.Add(() => Print(pattern));
            }
            foreach (var upgrade in _data.Upgrades.Where(u => _orbiter.Upgrades.All(owned => owned.Name != u.Name)))
            {
                labels.Add($"Upgrade: {upgrade.Name} ({upgrade.MaterialCost} materials)");
                actions.Add(() => Install(upgrade));
            }

            if (labels.Count == 0)
            {
                _view.ShowText("Nothing new to print or build. Bots can find patterns on the surface and transfer them home.");
                return;
            }

            string picked = Prompt.ChooseFrom(_view, _input, $"Print or upgrade ({_orbiter.Materials.Describe()})", labels, l => l);
            if (picked != null) actions[labels.IndexOf(picked)]();
        }

        private void Print(Pattern pattern)
        {
            var item = _orbiter.Print(pattern);
            _view.ShowText(item == null
                ? $"Not enough materials (need {pattern.MaterialCost})."
                : $"Printed, and added to the collection: {item.Describe()}");
        }

        private void Install(Upgrade upgrade)
        {
            _view.ShowText(_orbiter.Install(upgrade)
                ? $"Installed {upgrade.Name}. {upgrade.Description}"
                : $"Not enough materials for {upgrade.Name} (need {upgrade.MaterialCost}).");
        }

        private void ShowList(string title, IEnumerable<string> lines)
        {
            var list = lines.ToList();
            _view.ShowText($"-- {title} --\n" + (list.Count == 0 ? "(nothing yet)" : string.Join("\n", list)));
        }

        private static string Plural(int count, string one, string many) => $"{count} {(count == 1 ? one : many)}";
    }
}
