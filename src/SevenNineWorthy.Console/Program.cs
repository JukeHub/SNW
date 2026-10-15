using System;
using System.IO;
using SevenNineWorthy.Core;
using SevenNineWorthy.ConsoleHost;

// Usage: SevenNineWorthy.Console [--data DIR] [--save FILE] [--seed N]
string dataDir = "data";
string savePath = "orbiter.save.json";
int? seed = null;

for (int i = 0; i < args.Length; i += 2)
{
    string value = i + 1 < args.Length ? args[i + 1] : null;
    switch (value == null ? null : args[i])
    {
        case "--data": dataDir = value; break;
        case "--save": savePath = value; break;
        case "--seed" when int.TryParse(value, out int s): seed = s; break;
        default:
            Console.Error.WriteLine($"Bad option '{args[i]}'. Usage: [--data DIR] [--save FILE] [--seed N]");
            return 1;
    }
}

GameData data;
BalanceConfig balance;
try
{
    data = GameData.Load(dataDir);
    balance = BalanceConfig.Load(Path.Combine(dataDir, "balance.json"));
}
catch (GameDataException e)
{
    Console.Error.WriteLine($"Could not load game data: {e.Message}");
    return 1;
}

var engine = new GameEngine(new ConsoleGameView(), new ConsoleInputSource(),
                            new JsonFileSaveStore(savePath), data, balance, seed);
engine.Play();
return 0;
