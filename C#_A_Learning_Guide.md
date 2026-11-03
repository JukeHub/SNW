# C# by Building Seven Nine Worthy — A Learning Guide

Oct 27, 2026 · @Julian Dominguez

## How this guide works

The goal is for you to be able to confidently read, change, and extend Seven Nine Worthy in C# on your own. We get there by reading *your* code, in the order the program actually runs it, and learning each C# feature at the moment it first matters.

**The ground rule.** I will not edit, create, move, or delete any file in your SNW folder. I only read it. If I ever think an edit would help, I will describe it here and leave the typing to you. The only exception is if I explicitly ask permission for a specific file and you say yes. Writing the code yourself is also how the learning sticks, so this rule and the teaching goal point the same way.

**How the three pieces fit together.**

- *This guide* is the durable reference. Each lesson explains a slice of the program and the C# it uses, with excerpts from your real code. It grows as we go; you can reread or search it anytime.
- *Chat* is where you ask questions, push back, or ask me to re-explain. Nothing here is one-way.
- *Exercises* close each lesson. They are small tasks for you to code in your own project (or a scratch file). You write; I review and give feedback.

**What I'm assuming you know.** You already read and write C and C++, so you know pointers, structs, classes, headers, manual memory management, and the compile/link model. I'll teach C# by contrast with that: what's the same, what's renamed, and what's genuinely new. I won't re-explain loops, functions, or variables from scratch — I'll focus on where C# diverges.

**The throughline is object-oriented design.** Since the changes you most want to make are OO upgrades, every lesson calls out an **OO lens**: the design reasoning, not just the syntax — what should be an object, who owns which data, when an interface or a base class actually earns its keep, and where a new feature has one obvious home. The guide and your `CLAUDE.md` design brief agree on a principle worth keeping front of mind: use inheritance and interfaces only where they pay for themselves, never for ceremony. By Lesson 7 and the capstone, you'll be designing a real extension yourself.

## The roadmap

We follow one full play session from the first line that runs to a run ending — and object-oriented design is the spine of every lesson, because that's where your upgrades will live. Each lesson names the files it covers, the C# ideas it introduces, and its **OO lens** (the design decision at play). Your project's own `CLAUDE.md` lists the planned upgrades as *extension seams* — adding ability types, giving buffs real mechanical effects, survivor quests — and each one is an OO design exercise. The roadmap below builds toward being able to do exactly that. We're on Lesson 1.

1. **The entry point** — `Program.cs`. Startup, command-line args, wiring the game together. *(current)* — top-level statements, `using`/namespaces, `var`, nullable `int?`, string interpolation, `switch` with `when`, exceptions, `new` and garbage collection. *OO lens:* the composition root — objects are wired together through interfaces (dependency injection), not created where they're used.
2. **The IO boundary** — `IGameView`, `IInputSource`, `ISaveStore` and their Console implementations. — interfaces, implementing them, why the Core never touches `Console`. *OO lens:* program to an interface, not an implementation; how C# interfaces differ from C++ abstract classes, and the substitution principle in practice.
3. **Loading the content** — `GameData`, `BalanceConfig`, `JsonFile`. — generics, `Dictionary`, `IReadOnlyList`, JSON deserialization, static factory methods, failing loudly. *OO lens:* encapsulation — exposing read-only views over private data, and building an object that is valid the moment it exists.
4. **The between-runs loop** — `GameEngine.Play`. — properties vs fields, `enum`, LINQ (`Where`, `Select`, `OfType`), `Func<>` delegates. *OO lens:* single responsibility — an orchestrator that delegates to specialists, and objects collaborating by calling each other's methods.
5. **Starting a run** — `Orbiter`, `PrintService`. — reference vs value types, `out` parameters, the `TryPrint` pattern. *OO lens:* the aggregate / save root, and who is *allowed* to change state (encapsulation with `private set`).
6. **The run loop** — `RunController`. — tuples, local functions, `List<T>`, building menus dynamically. *OO lens:* keeping a controller thin by handing real work to the objects that own it.
7. **Inheritance and polymorphism** — `Bot`, `Enemy`, `Boss`, `CombatResolver`. — base classes, `virtual`/`override`, pattern matching (`is Boss`), expression-bodied members. *OO lens:* **the core lesson for your upgrades** — when a subclass earns its place, polymorphism vs. type-checking, and the “composition over inheritance” choice you'll face when adding ability types and buff effects.
8. **Persistence** — `JsonFileSaveStore`, the save round-trip. — serialization, nullability. *OO lens:* separating *what* persists from *how*; serializing an object graph without leaking storage concerns into the model.
9. **Make your own OO upgrade** — capstone. Pick one extension seam from `CLAUDE.md` (e.g. give a comfort buff a real mechanical effect, or add a second attack/ability type to `CombatResolver`) and design it, then build it end to end yourself. I review the *design* with you before you write a line. This is the whole point of the guide: a real object-oriented change, made by you.

The order can flex — if something grabs your curiosity, we chase it. And if you'd like, we can take a standalone detour on the four OO pillars (encapsulation, inheritance, polymorphism, abstraction) in C# terms before Lesson 7 — just say the word.

## Lesson 1 — The entry point (Program.cs)

When you run `dotnet run --project src/SevenNineWorthy.Console`, the very first of *your* code to execute is `Program.cs` in the Console project. It's short, and almost every line shows off something about C#. Here it is, in pieces.

### The file has no Main() — and that's normal now

```csharp
using System;
using System.IO;
using SevenNineWorthy.Core;
using SevenNineWorthy.ConsoleHost;

// Usage: SevenNineWorthy.Console [--data DIR] [--save FILE] [--seed N]
string dataDir = "data";
string savePath = "orbiter.save.json";
int? seed = null;
```

In C and C++, execution begins at `int main(int argc, char** argv)`. You don't see one here. C# supports **top-level statements**: the compiler takes the statements written directly in this file and wraps them in a hidden `Main` method for you. `args` (the command-line arguments) is still available — it's an implicit variable of type `string[]`. So this file *is* the body of `Main`; the `return 1;` and `return 0;` you'll see below are returning the process exit code, exactly like returning from `main`.

**`using` is not `#include`.** This is the biggest early mental shift from C/C++. There are no header files in C#, and `using` does **not** paste a file's text in. Compilation works on whole *assemblies* (compiled `.dll` libraries) and *namespaces* (logical name groupings). `using System;` means “let me write `Console` instead of `System.Console`” — it just saves typing by importing names from a namespace that's already available to the compiler. `using SevenNineWorthy.Core;` brings in the names from your Core library (which this project references — we'll see where that reference is declared in Lesson 2). There's no include-guard dance, no forward declarations, no separate `.h`/`.cpp` split. One file declares and defines a type in one place.

**Types come after, or are inferred.** `string dataDir = "data";` reads left-to-right like C, but `string` is a real first-class type (not `char*`), with a `.Length`, comparison that compares *contents*, and no manual memory to manage. `int? seed = null;` is the interesting one — see below.

### Nullable value types: `int?`

```csharp
int? seed = null;
```

In C/C++, an `int` can't be “no value” — you'd use a sentinel like `-1`, or a separate `bool hasSeed`. C# lets you write `int?` (shorthand for `Nullable<int>`), a value type that can hold either a real `int` or `null`. That's exactly what the design wants: *“if the user passed `--seed`, use it; otherwise each run picks its own.”* Later, `GameEngine` reads it as `_fixedSeed ?? Environment.TickCount` — the `??` is the **null-coalescing operator**: “use the left side if it has a value, otherwise the right.” Clean, no sentinel.

### Parsing args: var, switch expressions, when-guards, the ternary

```csharp
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
```

A lot is packed in here. Walking through it:

- **`args.Length`** — arrays know their own length in C#. No `argc` passed separately; no walking to a null terminator.
- **The ternary `? :`** works just like C/C++. `i + 1 < args.Length ? args[i + 1] : null` grabs the next token as the option's value, or `null` if there isn't one.
- **`switch` on a `string`.** In C and C++ you cannot `switch` on a string at all — only on integers/enums. C# can switch on strings (and much more). Each `case "--data":` compares by string *content*.
- **`break` is still required** per case, but C# forbids silent fall-through: if a case has code, it must end with `break` (or `return`), so the classic C bug of forgetting a `break` can't compile.
- **`when` guards.** `case "--seed" when int.TryParse(value, out int s):` means “match `--seed`, *but only if* the value also parses as an integer.” `int.TryParse` is the idiomatic C# “parse without throwing”: it returns a `bool` for success and writes the parsed number into the `out int s` parameter. `out` is like passing a pointer to be filled in C, but type-checked and explicit at the call site. Note `s` is *declared inline* right there in the argument — that's an **out variable declaration**.
- **`$"..."` is string interpolation.** Instead of `printf("Bad option '%s'", args[i])`, you write `$"Bad option '{args[i]}'."` and the `{...}` expressions are substituted in, already type-safe and converted to text. No format specifiers to match up.
- **`Console.Error.WriteLine`** writes to stderr. This line lives in the *Console* project, which is allowed to touch `System.Console`. The Core library is forbidden from doing this (Lesson 2 explains why).

### Loading data: exceptions instead of error codes

```csharp
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
```

In C you'd return an error code and check it after every call; in C++ you might, or you might throw. C# leans hard on **exceptions**. `GameData.Load(...)` either returns a fully valid `GameData` or *throws* a `GameDataException` — it never returns a half-broken object. The `try`/`catch` says “run these loads; if a `GameDataException` comes out of either one, handle it here.” This is why `GameData` and `balance` are declared *before* the `try`: the variables must exist in the outer scope so the rest of the file can use them, but they're only *assigned* inside the `try`. (The compiler actually proves you can't reach a use of them along a path where they weren't assigned — “definite assignment.”)

`GameData.Load` is a **static factory method** — a function on the type itself (not on an instance) that builds and returns one. We'll meet that pattern again in Lesson 3. `Path.Combine(dataDir, "balance.json")` joins path segments with the right separator for the OS — portable path building, from `System.IO`.

### Wiring the game together: new, interfaces, and no delete

```csharp
var engine = new GameEngine(new ConsoleGameView(), new ConsoleInputSource(),
                            new JsonFileSaveStore(savePath), data, balance, seed);
engine.Play();
return 0;
```

**`var`** asks the compiler to infer the type from the right-hand side. `var engine = new GameEngine(...)` is exactly `GameEngine engine = new GameEngine(...)` — the type is still static and fixed at compile time, you just didn't repeat it. (This is not like C++ `auto` in template-heavy code, but the instinct is the same: don't write what the compiler already knows.)

**`new` creates an object on the managed heap** — and there is no matching `delete`. C# has a **garbage collector**: once nothing refers to an object anymore, the runtime reclaims it automatically, whenever it likes. No `free`, no `delete`, no destructors to pair up, no double-free or use-after-free. This removes a whole category of C/C++ bugs. (There's a `using` *statement* form for deterministic cleanup of things like file handles — different from the `using` import directive above — and we'll see it when we hit file IO.)

The most important design point is *what* gets passed in. `GameEngine` isn't handed a `ConsoleGameView` as a `ConsoleGameView`; its constructor accepts an `IGameView`, an `IInputSource`, and an `ISaveStore` — **interfaces**. `Program.cs` happens to pass the Console implementations, but the engine only knows the interface. That's **dependency injection**: the host decides the concrete pieces and hands them in. Swap in a Unity view later and the Core doesn't change a line. That boundary is the whole architecture, and it's Lesson 2.

Finally, `engine.Play()` runs the game (it loops until the player saves and quits), and `return 0;` reports success to the shell.

### The one-paragraph summary

`Program.cs` is the composition root: it reads options, loads the data-driven content (failing loudly if it's broken), constructs the concrete Console-flavored pieces, injects them into the `GameEngine` through interfaces, and presses Play. Everything game-related lives behind that boundary.

## Lesson 1 exercises

These are yours to do — I'll review whatever you try and answer questions. Do them in any order. The first three are reading/thinking; the last two involve writing a little code (in a throwaway scratch file, or just paste your answer into chat — you decide, and remember I won't touch your SNW files).

**1. Trace the args.** Suppose the game is launched as `dotnet run --project src/SevenNineWorthy.Console -- --seed 42 --data mydata`. Walk the `for` loop by hand: what are `dataDir`, `savePath`, and `seed` after it finishes? What would happen with just `--seed` and no number after it, and which line produces that behavior?

**2. Why declared outside the try?** Explain in your own words why `GameData data;` and `BalanceConfig balance;` are declared *before* the `try` block instead of inside it. What compiler error would you expect if they were declared inside?

**3. Spot the C/C++ instinct that won't work.** Here are three things a C/C++ programmer might reflexively write. For each, say whether it works in C# and why or why not: (a) checking `if (seed == -1)` to detect “no seed given”; (b) writing `switch (args[i])` on a string; (c) calling `free(engine)` after `engine.Play()`.

**4. Write a tiny interpolation.** In a scratch `Program.cs` somewhere outside your project (e.g. a new folder, `dotnet new console`), write a few lines that declare `string name = "Seven Nine Worthy";` and `int zones = 7;` and print `Starting Seven Nine Worthy with up to 7 zones.` using a single interpolated string. Then do the same line using `string.Format` or concatenation, and notice which you prefer.

**5. Experiment with `int?` and `??`.** In that same scratch file, declare `int? maybe = null;`, print `maybe ?? 99`, then set `maybe = 7;` and print `maybe ?? 99` again. Predict both outputs before you run it, then check. Bonus: what does `maybe.HasValue` and `maybe.Value` give you in each case, and what happens if you read `.Value` while it's null?

When you've tried any of these, paste what you did or what you concluded and I'll give feedback. Or if anything in the lesson was unclear, just ask — that's what chat is for.
