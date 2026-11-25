# Seven Nine Worthy

A text-based roguelite crawler with a cozy-apocalypse theme. Design brief: [CLAUDE.md](CLAUDE.md).

## Layout

- `src/SevenNineWorthy.Core` — all game logic (netstandard2.0, no host IO; Unity-safe).
- `src/SevenNineWorthy.Console` — terminal front-end: `Program.cs` plus the `IGameView`, `IInputSource` and `ISaveStore` implementations.
- `data/` — content (`zones`, `enemies`, `patterns`, `upgrades`, `starting_media`) and tunables (`balance.json`). Edit these without recompiling.

The model follows the class diagram: one class per diagram class, each owning its own behavior. `GameEngine` and `RunController` hold only menus and sequencing, following the activity diagram.

## Run

From the repository root:

```bash
dotnet run --project src/SevenNineWorthy.Console
```

Options:

- `--data DIR` — content directory (default `data`)
- `--save FILE` — save file (default `orbiter.save.json` in the current directory)
- `--seed N` — use this seed for every run, to reproduce one exactly

Only the orbiter is saved. A run in progress is lost if you quit mid-run.

Saves from before the rebuild to the diagrams cannot be read; delete the old `orbiter.save.json` to start a new orbiter.
