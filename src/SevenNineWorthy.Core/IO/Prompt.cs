using System;
using System.Collections.Generic;
using System.Linq;

namespace SevenNineWorthy.Core
{
    internal static class Prompt
    {
        // Keys for list items. 'B' is left out because it's the Back key.
        private const string ListKeys = "123456789ACDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>Shows a menu and returns the key the player picked.</summary>
        public static char Choose(IGameView view, IInputSource input, string title, IReadOnlyList<MenuOption> options)
        {
            view.ShowMenu(title, options);
            return input.ReadKey(new string(options.Select(o => o.Key).ToArray()));
        }

        /// <summary>Lets the player pick one item from a list, or [B]ack (returns null).</summary>
        public static T ChooseFrom<T>(IGameView view, IInputSource input, string title,
                                      IReadOnlyList<T> items, Func<T, string> label) where T : class
        {
            if (items.Count > ListKeys.Length)
                throw new InvalidOperationException($"Too many items ({items.Count}) for a single-key menu: {title}");

            var options = items.Select((item, i) => new MenuOption(ListKeys[i], label(item))).ToList();
            options.Add(new MenuOption('B', "Back"));

            char key = Choose(view, input, title, options);
            return key == 'B' ? null : items[ListKeys.IndexOf(key)];
        }
    }
}
