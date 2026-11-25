# CLAUDE.md — Seven Nine Worthy

A text-based, roguelite crawler with a cozy-apocalypse theme, built in **C#**. The player runs an orbiter above a dead Earth, prints bots, de-orbits them to help survivors and salvage materials, patterns and media, and upgrades their orbiter between runs. This file is the authoritative design brief. **Build the minimal version of everything here. Do not add features, systems, or abstraction layers that are not asked for.**

---

## 0. Prime directives (read first)

1. **The diagrams are the blueprint.** The class diagram and the activity diagram (kept in `docs/`) are binding. Build only the classes, attributes and operations the class diagram shows, plus the shell listed in §2. Where the diagrams are silent, §9 decides.
2. **Behavior lives in the model.** The object that owns the state guards it: the `Orbiter` prints and stores, the `Bot` spends its own battery, the `Enemy` attacks. No service classes beside the model.
3. **Keep it simple.** Favor the smallest design that satisfies the requirement. If a field and a method will do, do not introduce an interface, a manager, or a framework.
4. **No manipulative mechanics, ever.** No ad breaks, daily logins, timed/FOMO events, or endless incremental progress. This is a hard product rule (NFR).

---

## 1. Hard constraints

These are non-negotiable; violating any one means a rewrite later.

- **Unity-safe core.** All game logic lives in a class library that targets **.NET Standard 2.0** and **never references `System.Console`** (or any other host IO). A future Unity front-end must be able to reference the core unchanged.
- **IO only through interfaces.** The core talks to the outside world through `IGameView` (output), `IInputSource` (input), and `ISaveStore` (persistence). The terminal project implements them with `Console`/file IO. The core never does IO directly.
- **One seeded RNG per run.** `Run` owns the only `new Random(seed)`; every roll in a run uses it. **Never call `new Random()` anywhere else** — doing so silently breaks the reproducible-runs requirement. This is what `Run.Seed` is for.
- **Data-driven content.** Zones, enemies, patterns, upgrades and starting media are loaded from JSON data files at startup, straight into the model classes. Adding content must not require recompiling.
- **Config-driven balance.** Battery costs, boss-chance curve, the starting bot and its abilities, starting materials, and similar tunables live in a single `balance.json`. No magic numbers in logic.
- **Single-key input.** Every player choice is one number or letter. `IInputSource` returns a single key; menus present numbered/lettered options.
- **One resource name.** Use **`Battery`** everywhere for the bot's energy. (An `Ability` has an `EnergyCost`, which is paid in battery.)
- **Materials are one currency.** Materials are a `Material` Item with a `Quantity`. The Orbiter keeps a single Material stack. Do not model typed resources (metal/plastic/etc.).
- **Runs are ephemeral.** Only the `Orbiter` persists. Quitting mid-run abandons the run. The transient run state is never serialized.
- **Single-player.** "Other orbiters" is narrative flavor only. No networking, no multiplayer, no async messaging.
- **`Program.cs` does not change.** `GameData.Load(dir)`, `BalanceConfig.Load(path)`, `GameDataException` and the six-argument `GameEngine` constructor keep their signatures.

---

## 2. Solution layout

```
SevenNineWorthy.sln
docs/                        the class diagram and the activity diagram
src/SevenNineWorthy.Console/
  Program.cs, ConsoleGameView.cs, ConsoleInputSource.cs, JsonFileSaveStore.cs
src/SevenNineWorthy.Core/
  IO/          IGameView, IInputSource, ISaveStore, Prompt
  Content/     JsonFile, SubtypeConverter, GameData, BalanceConfig, ZoneTemplate
  Persistent/  Orbiter, Upgrade (+ BotUpgrade, OrbiterUpgrade), Effect, Contact
  Items/       Item, Material, Pattern, MediaItem (+ Book, Music, Movie)
  Runs/        Run, Bot, Ability, Zone, Survivor, Enemy (+ Boss)
  Systems/     GameEngine, RunController
data/
  zones.json, enemies.json, patterns.json, upgrades.json,
  starting_media.json, balance.json
```

- `Console` depends on `Core`. `Core` depends on nothing host-specific.
- Content/config file paths are passed **into** the core by the host, so Unity can supply its own. The core does not hardcode `data/`.
- **The shell** is everything outside the 21 diagram classes: the two projects, the three IO interfaces and their console implementations, `Prompt`, `JsonFile`, `SubtypeConverter`, `GameData`, `BalanceConfig`, and the two controllers. No other class belongs in `Core`.
- A test project is optional; skip it unless asked.

---

## 3. The model (21 classes, two owners)

The Orbiter owns what persists. The Run owns what disappears when the run ends. Small subclasses share a file with their parent.

### Items — everything a bot can carry home
- **`Item`** (abstract) — `Name`, `Description`, `Describe()`. An Item is in one place at a time: offered by a Zone, carried by a Bot, or stored by the Orbiter.
- **`Material : Item`** — adds `Quantity`.
- **`Pattern : Item`** — `MaterialCost`, `PrintsInto` (the media item it makes), `Print()` returns a new copy of that Item.
- **`MediaItem : Item`** (abstract) — `Title`, `ComfortBoost`, `Consume()` returns an Effect. **`Book`** (`Pages`), **`Music`** (`Minutes`) and **`Movie`** (`Minutes`) override `Describe()` and `Consume()`: read, listened to, watched.

### Persistent — hangs off the Orbiter, saved to one JSON file
- **`Orbiter`** — the save root. `Name`, `ComfortLevel`, its Material stack, and 0..\* Items, Upgrades, Effects and Contacts.
  - `Print(Pattern)` — if stored materials cover `MaterialCost`, deduct them, call `pattern.Print()`, store and return the Item. Otherwise change nothing and return null.
  - `StartRun()` — deduct the bot cost; build a Bot whose battery is the base value plus `ComfortLevel`; reduce every Effect's `TurnsLeft` by one and remove expired Effects; return the new Run.
  - `Store(Item)` — a Material adds its `Quantity` to the single stack; any other Item joins the stored list.
- **`Upgrade`** (abstract) — `Name`, `Description`, `MaterialCost`, `Apply()`. Bought with materials from `upgrades.json`; `Apply()` runs once, when bought. **`OrbiterUpgrade`** adds `ComfortBonus` to `ComfortLevel` permanently. **`BotUpgrade`** adds `StatBonus` to the health of every bot printed afterwards.
- **`Effect`** — `Name`, `TurnsLeft`, `ComfortBoost`. Lasts a number of launched runs; takes its boost back when it expires.
- **`Contact`** — `Name`, `LastKnownZone`. A survivor a bot has helped.

### Run — transient, never serialized
- **`Run`** — `Seed`, `ZoneIndex`, one `Bot`, the visited `Zones`. `AdvanceZone()` picks a ZoneTemplate with the run's RNG, builds the Zone, rolls its boss, adds it to the visited list, increments `ZoneIndex` and returns it. `End()` marks the run over and discards what the Bot still carries.
- **`Bot`** — `Health`, `Battery`, 1..\* `Abilities`, carried `Items`. `UseAbility(Ability)` spends the ability's `EnergyCost` and returns the damage dealt. `SpendBattery(int)`, `TakeDamage(int)`. `IsOperational`: health and battery both above zero.
- **`Ability`** — `Name`, `EnergyCost`, `Power`.

### World
- **`ZoneTemplate`** — a zone definition from `zones.json`. `Build()` returns a Zone.
- **`Zone`** — `Name`, `BossChance`, its Enemies, Survivors and offered Items. `ListActions()` returns which of salvage, fight and help are valid now: fight while an enemy lives; salvage while Items remain; help while a Survivor is unhelped. `RollForBoss()` rolls against `BossChance`; on success a Boss joins the Zone's enemies.
- **`Enemy`** — `Health`, `Damage`, `Attack()` returns its damage, `TakeDamage(int)`. **`Boss : Enemy`** adds `Multiplier` and overrides `Attack()`. **No code asks `is Boss`.**
- **`Survivor`** — `Name`, `Need`.

### Controllers — menus and sequencing only
- **`GameEngine`** — the Game System and Player swimlanes in the Orbiter: load, the Orbiter menu, hand off to `RunController`, save.
- **`RunController`** — the same swimlanes on the surface: the zone/action loop and the "Bot operational?" check.

The controllers call the model and print the results. A fight is three lines: the bot uses an ability, the enemy takes the damage, and a surviving enemy attacks back.

---

## 4. Game flow (the activity diagram)

1. Game System loads the saved state (or creates a new Orbiter).
2. **In the Orbiter**, the Player chooses repeatedly: **browse the collection** (→ consume media, which adds an Effect) · **print or upgrade** (one menu: patterns print Items, upgrades are bought with materials) · **launch** · **save and quit**.
3. **Launch:** the Orbiter prints a bot, the Bot de-orbits, the Game System generates a zone and rolls for a boss.
4. **On the surface**, the Player chooses repeatedly:
   - **move to a new zone**, which generates the next zone;
   - **take an action in the zone** — salvage, fight or help, as the Zone lists them. One fight action is one exchange of blows;
   - **transfer loot** — one action sends every carried Item, and the Orbiter stores each;
   - **self destruct**.
5. After each choice: **"Bot operational?"** Yes returns to the choice. No ends the run.
6. **End run** (`Run.End()`): one ending path for destroyed, drained and self-destructed. Only what was already transferred is kept. Then the Game System saves and returns to the Orbiter menu.

There is no "return to orbiter": the bot only comes home as what it transfers.

---

## 5. Win condition

**None for now.** This phase is about exploration. There is no counter; the Orbiter's Contacts list is the record of who has been helped.

---

## 6. Data & config files

- **`zones.json`** — zone templates: name, description, material yield range, enemy/boss/pattern pools (by name), survivors.
- **`enemies.json`** — enemies and bosses. `"type": "boss"` entries carry a `multiplier`.
- **`patterns.json`** — patterns bots can find: name, description, material cost, and the media item each `printsInto`.
- **`upgrades.json`** — upgrades for sale: `"type": "orbiterUpgrade"` (with `comfortBonus`) or `"type": "botUpgrade"` (with `statBonus`).
- **`starting_media.json`** — the books, albums and movies a brand-new orbiter starts with.
- **`balance.json`** — starting orbiter, starting bot and its abilities, bot print cost, battery cost per action, comfort settings, boss-chance curve.

Where a file or the save holds a class with subclasses, a `"type"` field names the subclass. `SubtypeConverter` is the one mechanism for this, used by content files and the save alike.

Loaders must fail loudly with a clear message on malformed/missing data, not silently default.

---

## 7. Extension seams (**do not build these now**)

| Feature | Now | Future home (don't build) |
|---|---|---|
| More abilities | The starting bot has one `Ability`, listed in `balance.json`; the fight menu offers every ability the bot has | Add entries to the list |
| Bot upgrades | `BotUpgrade` exists and works; `upgrades.json` has no entries of that type | Add entries |
| Survivor relationships / quests | `Contact` = name + last known zone | Add fields to the record; a quest system reads the contact list |
| Leave-loot-in-a-zone | Un-sent Items are discarded in `Run.End()` (one place) | Change that one place; add a "recover" action |

**Rule:** if honoring a row would add an interface, a manager, or config that nothing uses yet, stop — you're building scaffolding, not leaving a seam.

---

## 8. Explicitly NOT in this build

- Prestige / meta-progression of any kind.
- Multiplayer, networking, or inter-orbiter communication.
- Relationship or quest systems.
- Loot recovery / persistent zone state.
- Mid-run save/resume (runs are ephemeral).
- Typed/multiple material resources.
- Keepsakes: bots bring home Materials, Patterns and MediaItems only.
- Any manipulative retention mechanic.

---

## 9. Decisions from the author

Settled:

1. **The shell stays** — two projects, three IO interfaces, JSON content and balance files (§2).
2. **After a run ends** — save, then return to the Orbiter menu, which has a save-and-quit choice.
3. **Keepsakes** — dropped.
4. **What comfort does** — each printed bot starts with extra battery equal to `ComfortLevel`. An Effect's `TurnsLeft` drops by one per run launched.
5. **Starting state** — enough materials for 10 default bots (`balance.json`), plus 5 books, 10 albums and 10 movies (`starting_media.json`).

Defaults where the diagrams are silent (follow unless the author changes them):

| Point | Default |
|---|---|
| How much damage an Ability does | `Ability.Power` |
| How an expired Effect lowers comfort | The Effect remembers its own `ComfortBoost` and subtracts it on expiry |
| Which stat `BotUpgrade.StatBonus` raises | Health |
| How upgrades are obtained | Bought with materials from `upgrades.json`; an Upgrade is not an Item |
| "Store or print item" during a transfer | The Orbiter stores what arrives; printing happens from the Orbiter menu |
| Salvaging or helping while enemies are alive | Allowed |
| What a transfer sends | Everything the Bot carries, for one battery cost |
| `Survivor.Need` | Descriptive text; helping costs battery and adds a Contact with the zone's name |
