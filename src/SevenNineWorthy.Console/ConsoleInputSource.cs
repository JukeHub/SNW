using System;
using SevenNineWorthy.Core;

namespace SevenNineWorthy.ConsoleHost
{
    public class ConsoleInputSource : IInputSource
    {
        public char ReadKey(string validKeys)
        {
            while (true)
            {
                char key = char.ToUpperInvariant(NextChar());
                if (validKeys.IndexOf(key) >= 0)
                {
                    Console.WriteLine(key);
                    return key;
                }
            }
        }

        // Single keypress when interactive; reads piped stdin (one char at a time) otherwise, so runs can be scripted.
        private static char NextChar()
        {
            if (!Console.IsInputRedirected)
                return Console.ReadKey(intercept: true).KeyChar;

            int c = Console.Read();
            if (c == -1)
                throw new InvalidOperationException("Input ended before the game did.");
            return (char)c;
        }
    }
}
