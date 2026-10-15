# CLAUDE.md — Seven Nine Worthy

A text-based, roguelite crawler with a cozy-apocalypse theme, built in **C#**. The player runs an orbiter above a dead Earth, prints bots, de-orbits them to help survivors and salvage media/patterns/loot, and upgrades their orbiter between runs. This file is the authoritative design brief. **Build the minimal version of everything here. Do not add features, systems, or abstraction layers that are not asked for.**

---

## 0. Prime directives (read first)

1. **Keep it simple.** Favor the smallest design that satisfies the requirement. If a requirement can be met with a field and a method, do not introduce an interface, a manager, or a framework.
2. **Object-oriented, but not ceremonially so.** Use inheritance/interfaces only where they earn their place (listed below). This is a student project meant to *demonstrate* clean OO, not to maximize pattern count.
3. **Forward-compatibility means seams, not scaffolding.** Several features are "not now, but don't block it later." Honor that by keeping responsibilities cleanly separated so the future feature has one obvious home — **not** by building the future feature's plumbing today. The "Extension seams" table (§7) is the whole of what forward-compat requires.
4. **No manipulative mechanics, ever.** No ad breaks, daily logins, timed/FOMO events, or endless incremental progress. This is a hard product rule (NFR).

---

## 1. Hard constraints

These are non-negotiable; violating any one means a rewrite later.

- **Unity-safe core.** All game logic lives in a class library that targets **.NET Standard 2.0** and **never references `System.Console`** (or any other host IO). A future Unity front-end must be able to reference the core unchanged.
- **IO only through interfaces.** The core talks to the outside world through `IGameView` (output), `IInputSource` (input), and `ISaveStore` (persistence). The terminal project implements them with `Console`/file IO. The core never does IO directly.
- **One seeded RNG per run.** Every random decision in a run goes through a single `Random` instance seeded from that run's seed and passed down the call chain. **Never call `new Random()` anywhere else** — doing so silently breaks the reproducible-runs requirement.
- **Data-driven content.** Zones, enemies, patterns, and loot are loaded from JSON data files at startup. Adding content must not require recompiling.
- **Config-driven balance.** Battery costs, boss-chance curve, starting materials, and similar tunables live in a single `balance.json`, loaded at startup. No magic numbers in logic.
- **Single-key input.** Every player choice is one number or letter. `IInputSource` returns a single key; menus present numbered/lettered options.
- **One resource name.** "Battery" and "bot energy" in the requirements are the same thing. Use **`Battery`** everywhere; delete the other term.
- **Materials are one currency.** A single integer `Materials` count. Do not model typed resources (metal/plastic/etc.).
- **Runs are ephemeral.** Only *orbiter progression* persists. Quitting mid-run abandons the run. The transient run state is never serialized. (This resolves the FR10 / "Save includes End Run" tension in favor of the simple path.)
- **Single-player.** "Other orbiters" is narrative flavor only. No networking, no multiplayer, no async messaging.

---

## 2. Solution layout

```
SevenNineWorthy.sln
  src/
    SevenNineWorthy.Core/        (class library, netstandard2.0 — all logic & models)
    SevenNineWorthy.Console/     (console app — Program.cs + IO implementations)
  data/
    zones.json
    enemies.json
    patterns.json
    loot.json
    starting_media.json
    balance.json
  README.md
```

- `Console` depends on `Core`. `Core` depends on nothing host-specific.
- Content/config file paths are passed **into** the core by the host (constructor/args), so Unity can supply its own paths. The core does not hardcode `data/`.
- A test project is optional for the rough build; skip it unless asked.

---

## 3. Responsibility sketch

One responsibility per type. Keep classes small.

### Persistent state (the save root — serialized to one JSON file)
- **`Orbiter`** — the save root. Holds `Materials`, orbiter `Upgrades`, `MediaCollection`, met `SurvivorContact`s, `LocationRecord`s, the lifetime `SurvivorsHelped` counter, and current comfort/buff state. Everything that survives between runs hangs off this.
- **`MediaItem`** — a collected music/book/movie entry (type + title + flavor). **`MediaCollection`** wraps the list and the browse/consume operations.
- **`Pattern`** — a printable blueprint (what it prints, material cost).
- **`Upgrade`** — an applied orbiter upgrade (name, effect description). Effects are mostly descriptive now (see §7).
- **`SurvivorContact`** — name + flavor text about who they are and their situation. A plain record for now.
- **`LocationRecord`** — a place name + flavor text, logged as the bot travels.

### Transient run state (created at run start, discarded at run end, never serialized)
- **`Run`** — owns the current `Bot`, the current `Zone`, the zone counter for this run, and the run's seeded `Random`. The lifetime of a single de-orbit.
- **`Bot`** — `Health`, `Battery`, carried `Inventory` (physical loot + patterns not yet sent home), and its attack. The thing that goes to Earth.
- **`Zone`** — generated from a `ZoneTemplate`: description, present enemies, available `materials`, and the set of actions currently valid here.
- **`Enemy`** — `Health`, attack value, flavor. **`Boss : Enemy`** (or an `IsBoss` flag) — a tougher variant, same combat mechanic. Prefer the smallest option that still reads as OO; a thin `Boss` subclass is fine.

### Systems (behavior — stateless where possible, RNG passed in)
- **`GameEngine`** — top-level orchestrator. Runs the between-runs menu loop, hands off to `RunController` when a run starts, owns the `Orbiter`, triggers saves. Talks to `IGameView`/`IInputSource`.
- **`RunController`** — drives one run: zone loop, action menu, dispatch to combat/salvage/help/transfer, and `EndRun`.
- **`CombatResolver`** — resolves bot vs. enemy. Now: trade attacks (bot physical attack reduces enemy health; enemy reduces bot health) until one drops or the bot disengages; fighting spends `Battery`. One method, one place.
- **`ZoneGenerator`** — builds a `Zone` from a random `ZoneTemplate` using the run RNG. Rolls boss presence from the config curve (chance rises with the run's zone index).
- **`PrintService`** — handles printing bots, items, and upgrades: validates material cost, deducts, produces the thing. Backs "Print Item," "Upgrade Orbiter," and starting a run.
- **`BuffNarrator`** — turns current comfort/buff state into the flavor text shown to the player ("state of mind"). No mechanical effect yet (see §7).

### IO boundary (implemented in Console; stubbed/other in Unity later)
- **`IGameView`** — output: show text, menus, run/combat results.
- **`IInputSource`** — input: return a single chosen key from a set of options.
- **`ISaveStore`** — `Load() -> Orbiter` / `Save(Orbiter)`. Console implements it as one JSON file on disk.

### Content & config loading
- **`GameData`** (or `ContentRepository`) — loads `zones/enemies/patterns/loot` JSON into template objects at startup; hands templates to `ZoneGenerator` and `PrintService`.
- **`BalanceConfig`** — typed view over `balance.json`: battery costs per action, boss-chance curve, launch-loot battery cost, starting materials, etc.

---

## 4. Game flow

### Between-runs loop (`GameEngine`, over the `Orbiter`)
Menu: **Browse Collection** (→ Consume Media for flavor buff) · **Upgrade Orbiter** (→ Print Item) · **Print Item** · **Start Run** (→ Print a bot, then de-orbit) · **Save & Quit**. Save happens automatically on quit and after each completed run.

### In-run loop (`RunController`, over the `Run`)
Per zone, present valid actions from: **salvage** (gain materials) · **fight enemy** · **fight boss** (if present) · **help survivor** · **transfer to orbiter** (→ transmit pattern / launch physical loot) · **move to next zone** · **self-destruct**. There is no "return to orbiter": the bot only comes home as what it transmits or launches. Zones are not reclaimed; once the bot moves on, a zone is simply left behind.

> Use-case consolidation: "Send Bot to Earth" and "Start Run" are **one** start-run flow in code, not two mirrored operations.

### End of run (`RunController.EndRun`)
Triggered by bot destruction (health 0, **or battery 0** — the bot shuts down) or self-destruct. Both end the run the same way: **the only progression kept is what was already transmitted (patterns) or launched (physical loot and salvaged materials ride the launch pod)**. Everything still on the bot is left in the zone where the run ended. Nothing tracks that left-behind cargo yet, but it is dropped in one place in `EndRun` so a later "recover" feature has an obvious home (§7). Then save.

---

## 5. Win condition

**None for now.** This phase of development is about exploration. `SurvivorsHelped` stays as a lifetime counter on the `Orbiter` (it persists across runs and survives saves) so a future ending or prestige layer can read it. There is no zone reclamation and no `ZonesReclaimed` counter.

---

## 6. Data & config files

- **`zones.json`** — array of zone templates: description, candidate enemy ids, material yield range, valid actions.
- **`enemies.json`** — enemy/boss defs: id, name, health, attack, flavor, isBoss.
- **`patterns.json`** — printable blueprints: id, name, material cost, what it yields.
- **`loot.json`** — physical loot defs discoverable in zones.
- **`starting_media.json`** — the media collection a brand-new orbiter starts with.
- **`balance.json`** — battery cost per action, battery cost to launch physical loot, boss-chance curve, material costs/overrides, starting materials, input timing/limits.

Loaders must fail loudly with a clear message on malformed/missing data, not silently default.

---

## 7. Extension seams (forward-compatible — **do not build these now**)

For each, implement only the "Now" column. The "Future home" column is where the later feature will slot in; keeping that boundary clean is all that's required today.

| Feature | Now | Future home (don't build) |
|---|---|---|
| Combat variety / crowd control | One physical `Bot` attack; `CombatResolver` resolves through a single method | More attack/ability types resolved by the same resolver; add ability implementations, don't rewire combat |
| State-altering buffs | `BuffNarrator` emits flavor text only; buffs are descriptive | Buffs gain an effect hook (e.g. reduce print time, improve combat control) applied at the points that read orbiter state |
| Survivor relationships / quests | `SurvivorContact` = name + flavor list | Add relationship/quest fields to the record; add a quest system that reads the contact list |
| Leave-loot-in-a-zone | Un-sent cargo left behind in `EndRun` (one place), not tracked | Change that one place to stash loot on a `LocationRecord`; add a "recover" action |
| Prestige / meta-progression | Lifetime `SurvivorsHelped` counter on persistent `Orbiter` | A prestige layer reads/resets those counters |

**Rule:** if honoring a row would add an interface, a manager, or config that nothing uses yet, stop — you're building scaffolding, not leaving a seam.

---

## 8. Explicitly NOT in this build

- Prestige / meta-progression of any kind.
- Multiplayer, networking, or inter-orbiter communication.
- Ability engine, crowd control, or multiple attack types.
- Mechanical buffs (buffs are flavor text only).
- Relationship or quest systems.
- Loot recovery / persistent zone state.
- Mid-run save/resume (runs are ephemeral).
- Typed/multiple material resources.
- Any manipulative retention mechanic.

---

## 9. Decisions from the author

1. **Zone reclaiming** — removed. Zones are explored and left behind; no counter, no win condition yet (§5).
2. **Self-destruct** — just ends the run. Un-sent loot is left in the zone, untracked for now (§7 seam).
3. **Death** — only what was already transmitted or launched is kept. Battery reaching 0 counts as destruction. There is no voluntary return.
4. **Starting state** — enough materials for 10 default bots (`balance.json`), plus 5 books, 10 albums and 10 movies (`starting_media.json`).
