using System;
using System.Collections.Generic;
using SevenNineWorthy.Core;

namespace SevenNineWorthy.ConsoleHost
{
    public class ConsoleGameView : IGameView
    {
        public void ShowText(string text)
        {
            Console.WriteLine();
            Console.WriteLine(text);
        }

        public void ShowMenu(string title, IReadOnlyList<MenuOption> options)
        {
            Console.WriteLine();
            Console.WriteLine($"-- {title} --");
            foreach (var option in options)
                Console.WriteLine($"  [{option.Key}] {option.Label}");
            Console.Write("> ");
        }
    }
}
