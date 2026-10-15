using System.Collections.Generic;

namespace SevenNineWorthy.Core
{
    /// <summary>All output from the core goes through here. The host decides how to render it.</summary>
    public interface IGameView
    {
        void ShowText(string text);
        void ShowMenu(string title, IReadOnlyList<MenuOption> options);
    }

    /// <summary>One selectable menu line: a single key and what it does.</summary>
    public class MenuOption
    {
        public char Key { get; }
        public string Label { get; }

        public MenuOption(char key, string label)
        {
            Key = char.ToUpperInvariant(key);
            Label = label;
        }
    }
}
