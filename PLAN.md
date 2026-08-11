# Minerva — Plan

A clean-room FFXIV Dalamud plugin inspired by BossmodReborn (BMR). Covers **boss
modules + radar**, **AI/automation**, and **replay**, with **AI-assisted module
generation** as the headline differentiator. **No autorotation.**

Reference mirror (read-only): `C:\Dev\BossmodReborn`. Your existing plugin
`C:\Dev\Mercury` is the proof you already own the Dalamud toolchain.

> "Clean reimplementation" here means: *own the code and the design*, but study BMR
> as the reference for game offsets, packet semantics, and mechanic patterns. We are
> not bound by BMR's `CONTRIBUTING.md` no-AI rule — this is your project.

---

## 1. How BMR is built (the mental model)

BMR splits into an **engine** and **content**. This split is the single most
important thing to preserve.

```
                    ┌─────────────────────────────────────────┐
   Game memory ───► │  GameSync: hooks + FFXIVClientStructs    │  ENGINE
   Game packets ──► │  → emits Operations                      │  (hard, ~100k LOC)
                    └───────────────────┬─────────────────────┘
                                        ▼
                    ┌─────────────────────────────────────────┐
                    │  WorldState  (Actors, Party, Casts,      │
                    │  Statuses, Tethers, Icons, MapEffects)   │
                    └───────────────────┬─────────────────────┘
                       ┌────────────────┼────────────────┐
                       ▼                ▼                ▼
                 ┌───────────┐   ┌────────────┐   ┌────────────┐
                 │ Replay    │   │ BossModule │   │ AIHints    │   CONTENT / FEATURES
                 │ record +  │   │ registry + │   │ (auto-     │
                 │ analysis  │   │ Components │   │  dodge)    │
                 └─────┬─────┘   └─────┬──────┘   └─────┬──────┘
                       │               ▼                │
                       │        Arena radar (ImGui) ◄───┘
                       ▼
                 AI-assisted module generation (Minerva's differentiator)
```

### The engine (the expensive part)
- `Framework/WorldStateGameSync.cs` (1,436 LOC): the heart. Hooks game functions
  (`ProcessPacketActorCast`, `EffectResult`, `ActorControl`, `MapEffect`, `RSVData`,
  `NpcYell`, …) via Dalamud `Hook<T>`, and reads `FFXIVClientStructs` memory offsets
  each frame. Converts all of that into a stream of `WorldState.Operation`s.
- `Network/` (2k LOC): opcode map + packet decode (`PacketDecoder`, `ServerIPC`).
- `Data/` (4.6k LOC): the clean model — `WorldState`, `Actor`, `ActorState`,
  `PartyState`, `ClientState`, `WaymarkState`, `NetworkState`, `FrameState`. This is
  what every feature reads. **Deterministic and game-agnostic by design** — it can be
  driven live *or* from a replay, which is why replay analysis works.
- `Pathfinding/` (3.5k LOC): map rasterization + A*/flood for AI movement.

### The content layer (the cheap, high-volume part)
- `Components/` (~40 files): reusable mechanic primitives. Each is a small class that
  reads `WorldState` and draws hints/AOEs. Examples: `SimpleAOEs`, `RaidwideCast`,
  `Knockback`, `StackSpread`, `Voidzone`, `Adds`, `Gaze`, `Exaflare`, `GenericAOEs`
  (the escape hatch you subclass for anything irregular).
- `Modules/` (2,763 files): one folder per boss. A module = an `OID`/`AID`/`SID` enum
  + a handful of one-line `Components.X` subclasses + a `StateMachineBuilder`.
- `BossModule/BossModuleRegistry.cs`: reflection-based auto-discovery. Every
  `BossModule` subclass with a `[ModuleInfo]` attribute is found and registered at
  startup — no central list to maintain.

### How a module is actually authored (BMR's real pipeline)
1. **Record** a replay of the fight (low DPS so the boss shows its whole rotation).
2. In Replay Manager, **right-click the boss → "Generate module stub"** and
   **"Generate missing enum values"**. The `Replay/Analysis/*.cs` classes mine the
   event log and emit enums with comments like:
   `PunutiyPress = 36492, // Boss->self, 5.0s cast, range 60 circle`.
3. Map each cast to a component one-liner:
   `sealed class Hydrowave(BossModule module) : Components.SimpleAOEs(module, (uint)AID.Hydrowave, new AOEShapeCone(60f, 15f.Degrees()));`
4. Wire them into `StateMachineBuilder.TrivialPhase().ActivateOnEnter<T>()`.
5. Validate in replay, then in a live duty.

**Key insight for Minerva:** steps 2–4 are mechanical and already half-automated by
BMR's analysis code. That is exactly where "AI-assisted generation" plugs in — we take
the *same replay event log* and emit the *whole* module (components + state machine),
not just a stub.

---

## 2. The one open decision: how much of the engine to rebuild

You said "not sure yet — help me decide." Here's the honest breakdown.

| Layer | Rebuild difficulty | Why | Recommendation |
|-------|-------------------|-----|----------------|
| **GameSync (hooks + offsets)** | ★★★★★ | Reverse-engineering game internals; offsets churn every patch; BMR is the *result* of years of RE by many people | **Reference, don't blindly retype.** Rebuild the *structure* cleanly, but the offsets/hook signatures ARE the accumulated knowledge — reimplementing them from scratch means re-deriving them, which is not realistic. |
| **Network decode** | ★★★★☆ | Opcodes randomize per patch; needs a maintained opcode source | Same: reuse the knowledge, rewrite the shell. |
| **WorldState / Data model** | ★★★☆☆ | Pure logic, no game hooks. This is where a *clean* design pays off | **Rebuild this yourself.** It's the spine and it's tractable. Owning it cleanly is the whole point of Minerva. |
| **Components / Modules** | ★★☆☆☆ | Straightforward once WorldState exists | **Rebuild — your own component taxonomy.** |
| **Replay** | ★★★☆☆ | Serialization of Operations; needs the WorldState model first | **Rebuild**, format is yours. |
| **Pathfinding / AI** | ★★★★☆ | Math-heavy but self-contained | **Rebuild the AI decision layer; port pathfinding math** (geometry is universal, not game-specific). |

**My recommendation — "clean core, referenced sync":**
Write the **WorldState/Data model, Components, Modules, Replay, and AI layers from
scratch** with a design you own. For the **GameSync + Network** layer, treat BMR as a
*datasheet of hard-won constants* — reimplement the hooking cleanly in Minerva's style,
but reuse the offset/opcode/packet-semantic knowledge rather than pretending to
rediscover it. Trying to reverse-engineer the game yourself would turn a multi-month
project into a multi-year one for zero product benefit.

This keeps ~70% of the codebase genuinely yours (the parts where design quality
matters) while not throwing away the ~30% that is pure encoded game-knowledge.

---

## 3. Proposed Minerva architecture

Same engine/content split, cleaner boundaries, generation-first.

```
Minerva/
├─ Minerva.csproj                 # .NET 10, Dalamud refs (copy Mercury's setup)
├─ manifest.json
├─ Core/
│  ├─ Data/                       # WorldState, Actor, *State — clean rebuild
│  ├─ Geometry/                   # WPos/WDir/Angle/Shapes — pure math, port
│  └─ GameSync/                   # hooks + offsets — referenced from BMR
├─ Network/                       # opcode map + decode — referenced
├─ Modules/
│  ├─ Framework/                  # ModuleBase, StateMachine, Registry, Arena
│  ├─ Components/                 # your mechanic primitives
│  └─ Content/<Expansion>/<Duty>/ # generated + hand-tuned boss modules
├─ Radar/                         # ImGui arena + hint windows
├─ Automation/                    # AIHints, decision loop, Pathfinding
├─ Replay/                        # recorder, log format, analysis
├─ Generation/                    # ★ AI-assisted module generator (differentiator)
└─ MinervaPlugin.cs               # Dalamud entry point
```

### The differentiator: `Generation/`
BMR stops at "generate a stub + enum list, human writes the rest." Minerva goes
further:
1. **Replay → structured mechanic facts.** Reuse BMR's analysis approach: per boss,
   emit every `AID` with caster/target/cast-time/shape/size, plus statuses, tethers,
   icons, map effects, arena geometry, and ordering.
2. **Facts → component mapping.** A rules engine (deterministic first) maps each fact
   to a Minerva component + shape (`range 60 30° cone` → `AOEShapeCone(60, 15°)`).
   Most real modules are ~80% mechanical one-liners — this covers them.
3. **LLM pass for the irregular 20%.** For mechanics that don't fit a template
   (custom `GenericAOEs`, phase logic, conditional baits), feed the fact sheet +
   your component catalog to a model and have it draft the `GenericAOEs` subclass and
   `StateMachineBuilder`. Output is a compilable `.cs` file, then human-reviewed and
   replay-validated.
4. **Round-trip validation.** Run the generated module against the *same replay* and
   diff predicted AOEs vs. actual damage events — an automatic correctness check BMR
   does by eye.

This makes "author a module" go from hours of hand-writing to *generate → review →
validate*, and it's legitimate because it's your project.

---

## 4. Phased roadmap

**Phase 0 — Scaffolding (days)**
- Create `Minerva.csproj` cloning Mercury's Dalamud reference block + BMR's
  `net10.0-windows` / unsafe / DalamudPackager setup. Bare plugin that loads, logs,
  and shows one ImGui window. *Milestone: dev plugin loads in Dalamud.*

**Phase 1 — Data spine ✅ DONE**
- Rebuilt `WorldState` + `Actor`/`ActorState`/`PartyState` cleanly around the replayable
  `Operation` pattern, in a **game-free `Minerva.Core` library** (no Dalamud refs) so it is
  unit-testable and drivable from either live sync or replays.
- Geometry (`WPos`/`WDir`/`Angle`), `Event<T>`, `ActionID`, `FrameState`, and an
  `OperationOutput` serialization seam are in place.
- **Milestone met:** `Minerva.Tests` drives a synthetic op stream (create→move→cast→status→
  die→destroy, plus a snapshot round-trip) and asserts the resulting WorldState — 37/37 pass,
  no game running. Run with `dotnet run --project Minerva.Tests`.
- Project layout: `Minerva.slnx` ties `Minerva` (plugin, net10.0-windows) → `Minerva.Core`
  (net10.0, pure) ← `Minerva.Tests` (net10.0 console self-test).

**Phase 2 — GameSync ✅ CODE-COMPLETE (needs in-game verification)**
- `GameSync/WorldStateGameSync.cs` (plugin project) mirrors the live game into the Phase-1
  `WorldState` every frame via `IFramework.Update`. Built on Dalamud's **managed** object table
  (`IObjectTable`/`IBattleChara`) rather than BMR's all-unsafe FFXIVClientStructs — a cleaner
  "referenced sync". Diffs each object → emits Create/Move/HPMP/Targetable/Dead/Combat/Target/
  Cast/Status ops; despawns via a seen-this-frame set. IDs unified on `GameObjectId` (targets +
  status sources included) so `Find()` is consistent.
- `GameSync/GameData.cs` — isolated CS bridge for the one value the managed API lacks (CFC id).
- `Windows/WorldStateDebugWindow.cs` + `/minerva debug` — live actor table (name/OID/type/pos/
  HP%/flags/cast) to verify the mirror.
- **Milestone:** build → load dev plugin → enter a duty → `/minerva debug` shows actors/casts
  updating live. *(Requires the user to run it in-game; can't be verified headless.)*

**Phase 2b — transient events + CS reads ✅ CODE-COMPLETE**
- **Polling (CS reads):** shield % (`ShieldValue`) folded into `ActorHPMP.Shield`; cast **location**
  (`GetCastInfo()->TargetLocation`) so location-targeted AOEs place correctly. (Cast rotation still
  the managed `Rotation` approximation — marked TODO.)
- **Packet hooks** (signatures reused from BMR — the hard-won constants; each wrapped in try/catch
  so a stale sig logs a warning instead of crashing): **ActorControl** → overhead icons (`OpIcon`),
  VFX (`OpVFX`), tethers (`OpTether` set/cancel), director updates (`OpDirectorUpdate`);
  **MapEffect** → `OpMapEffect` (ENVC arena changes); **RSV** → `OpRSVData`.
- Detours queue ops (main-thread `globalOps`/`actorOps`), drained next frame; per-actor events
  dispatched once the actor exists. New Core ops added: `OpMapEffect`, `OpDirectorUpdate`,
  `OpRSVData`, `OpVFX` (Core self-test still 37/37).
- `/minerva debug` gained an **Events tab** (rolling log of icons/VFX/tethers/map-effects/director/
  RSV) so 2b is verifiable in-game.
- **Genuinely deferred to Phase 4:** the caster-side **cast-resolved-with-full-target-list**
  (who-exactly-got-hit). It needs the randomized-opcode packet-decoder subsystem — an analysis
  concern, not a drawing one. Cast **start/finish** are polled, so mechanics still draw.

**Phase 3 — Radar + Modules framework ✅ CODE-COMPLETE (needs in-game verification)**
- **Core (game-free, tested):** AOE shapes (`Circle/Cone/Rect/Donut/Cross`) with `Check`
  hit-test + `Contour` outline; `ArenaBounds` (Circle/Square/Rect); `AOEInstance`; abstract
  `Arena` drawing surface + `Colors`; `ModuleComponent` (+TextHints/GlobalHints); `ModuleBase`
  (owns arena/components, subscribes to world events once and fans out to components);
  `StateMachineBuilder` (TrivialPhase/ActivateOnEnter); `[ModuleInfo]`; reflection
  `ModuleRegistry` (indexes by CFC id, infers boss OID from an `OID.Boss` member).
- **Components:** `GenericAOEs` base, `SimpleAOEs` (cast-driven), `RaidwideCast(s)`, `CastHint`.
  Live in `Minerva.Components` so modules write `Components.SimpleAOEs` (BMR ergonomics).
- **Plugin:** `ImGuiArena` (world→screen north-up; native circles, triangulated donuts, convex
  fills — no concave-fill in this ImGui build); `ModuleManager` (activates the right module for
  the current CFC + present boss, tears down on despawn/zone change); `RadarWindow` (`/minerva`
  or `/mine`) drawing the active module's arena + hints.
- **First module:** `D011PrimePunutiy` (Dawntrail dungeon), real IDs ported from BMR —
  `RaidwideCast` + `SimpleAOEs` cones + a custom `BuryDecay : GenericAOEs`. Activates on CFC 826.
- **Self-test now 49/49** (added module-framework + registry-discovery sections). Bug caught &
  fixed along the way: `CastFinished` now carries the finished cast; `SimpleAOEs`/`CastHint`
  field renamed off `AID` to avoid shadowing modules' `AID` enum in base-ctor calls.
- **Deferred components (as needed by future modules):** `SpreadFromCastTargets`, `StackWith*`,
  `BaitAwayTethers`, `ConcentricAOEs`, `Knockback`, `Voidzone`, `Gaze`, `Exaflare`.
- **Milestone:** load dev build → enter Ihuykatumu (first boss) → `/minerva` shows the arena with
  boss + AOE cones/circles drawing on casts. *(Needs in-game run; can't verify headless.)*

**Phase 4 — Replay ✅ CODE-COMPLETE**
- **Core (game-free, tested):** `ReplayRecorder` subscribes to `WorldState.Modified` and writes a
  text log (`<ticks> <serialized-op>` per line; header carries QPF/version; snapshots current state
  first). `OperationOutput` gained quote-aware string emit (names have spaces). `OpTokenReader`
  (quote-aware tokenizer + typed `Next*`) and `ReplayParser` reconstruct every op fourCC and replay
  the log back through a fresh `WorldState` offline. `ReplayAnalysis` observes the replayed world and
  mines a **fact sheet**: `OID`/`AID` enum candidates with caster→target-kind + cast time, statuses,
  tethers, icons, map-effect states, and an arena-center estimate.
- **Plugin:** `ReplayService` records to `ConfigDir/replays/*.log` and analyses offline; `/minerva
  record` toggles, `/minerva replay` opens `ReplayWindow` (record button + copyable fact sheet).
- Serialized `OpFrameStart` now carries the frame timestamp (exact time round-trip); snapshot now
  emits `OpHPMP` so mid-fight recordings keep HP.
- **Self-test now 65/65** — full record→parse→replay round-trip (incl. a name with spaces, moved
  position, rotation, status) + fact-sheet mining verified headlessly.
- **Milestone met:** record a fight → stop → fact sheet renders. This is the Phase-5 generator's input.
- **Interactive playback ✅ (self-test 154/154):** `ReplayParser.ParseTimeline` turns a log into a
  stepped `ReplayTimeline` (each op stamped with its frame's real time, so the opening snapshot ops share
  the fight start rather than the 0 their own line carries). Plugin `ReplayPlayer` steps it through a
  private `WorldState` on a real-time cursor (play/pause/speed/seek) and activates the matching boss
  module against that world — so **real AOE shapes, boundary and actors replay exactly as they did live**
  (falls back to drawing actors on an estimated arena when no module matches). Seeking rebuilds from the
  start to the target (ops are cheap, forward-only, and the module must be live before its casts replay so
  components catch them). `ReplayService` auto-loads the just-recorded fight on stop; `ReplayWindow` gained
  a **Playback** tab (Play/Pause, speed, timeline scrubber, arena canvas); `Plugin.OnUpdate` ticks it.
- **Still open (moved here from 2b):** caster-side cast-resolved-with-full-target-list needs the
  randomized-opcode packet decoder — deferrable until the generator needs per-target effect data.

**Phase 5 — AI-assisted generation ★ ✅ CODE-COMPLETE**
- **Core (game-free, tested):** `GenerationInput` structured facts (`ObjectFact`/`ActionFact`/
  `ArenaEstimate` with `TargetKind`), produced by `ReplayAnalysis.BuildGenerationInput()`.
  `IShapeResolver` + `ShapeHint` (shape kind/size, `NeedsReview` for values not in game data).
  `ModuleGenerator` maps facts→shapes into a **compilable** module: OID/AID enums, one component
  per cast classified into `SimpleAOEs` / `RaidwideCast` / a `CastHint` stub, a trivial state
  machine, `[ModuleInfo]` class + inferred arena — with a coverage report and TODO header.
  Anything unresolved is a *compiling stub with a TODO*, never a silent guess. `IModuleAugmentor`
  is the swappable LLM seam (`NullAugmentor` default — deterministic draft works with no backend).
- **Plugin:** `LuminaShapeResolver` reads the Action sheet (CastType/EffectRange/XAxisModifier) into
  real `AOEShape*` constructions; cone-angle/donut-inner (Omen-only) default + flagged for review.
  `ReplayService.GenerateModule()` writes `D<CFC>.generated.cs`; `ReplayWindow` gained a "Generate
  module" button + coverage report.
- **Validation:** self-test now **80/80** (classification: big self-circle→RaidwideCast, cone/circle→
  SimpleAOEs, unknown→stub; enums, states, ModuleInfo, coverage %). Plus a **compile smoke test** —
  the generator's verbatim output was dropped into the plugin and compiled clean against the real
  framework, then removed. So generated modules genuinely build.
- **Milestone met:** record → analyze → "Generate module" produces a compiling `.cs` draft with real
  shapes for the majority of mechanics and TODO stubs for the rest.
- **Direction decided:** NO runtime LLM integration. The "AI-assisted" value is a deterministic
  **signal-correlation extractor** (AI-designed during development, no API key / network / cost).

**Phase 5b — Correlation extractor upgrade ✅** (replaces the LLM-augmentor idea)
- The analyzer already recorded icons/tethers/statuses/map-effects/cast-targets/object-lifetimes but
  only used cast+shape. `ReplayAnalysis` now **correlates** them:
  - **Real names:** `INameResolver` (plugin `LuminaNameResolver` reads `Action.Name`) → enums read
    `PunutiyPress = 36492`; object names come from the replay directly.
  - **Cast-target counting:** player-targeted casts classified into spread (many simultaneous) /
    tankbuster (same target repeated) / bait (preceded by a tether) — no packet decoder needed.
  - **Icon/tether correlation:** an icon/tether within 8s before a cast → `SpreadFromIcon` /
    `BaitAwayTethers`. New components added: `SpreadFromIcon`, `StackFromIcon`, `BaitAwayTethers`.
  - **Voidzones:** lingering non-casting hazard objects (lifetime ≥ 4s, plausible hitbox) → `Voidzone`.
  - **Phases:** boss-sized enemies becoming targetable mark phase boundaries → activations grouped by
    phase in the state machine (transitions left for the author).
- `ModuleGenerator` classifies via these facts and names/de-dupes identifiers (`NameAllocator`).
- **Self-test now 99/99** — a synthetic replay exercises every path (raidwide/AOE/tankbuster/spread/
  bait/icon-spread/voidzone/2-phase + real names); the generated module **compiles against the
  framework** (smoke-tested then removed). The `IModuleAugmentor` seam was removed.

**Phase 6 — Automation ✅ CODE-COMPLETE (guidance; auto-move gated)**
- **Core (game-free, tested):** `AIHints` (forbidden-zone model + `InImminentDanger`),
  `ArenaPathfinder.Solve` (grid rasterization → nearest safe spot to the player, respecting a
  look-ahead horizon so it doesn't twitch for far-future casts, and honestly reporting when the
  whole arena is covered). `ModuleComponent.AddAIHints` hook; `GenericAOEs` contributes its active
  AOEs as forbidden zones; `ModuleBase.BuildAIHints` fans out to components.
- **Plugin:** `AIManager` builds hints from the active module each frame, solves, and exposes the
  `SafeSpot`; `RadarWindow` draws the dodge target + arrow when the player must move. Toggles in the
  main window (`AutoDodgeGuidance` on, `AutoDodgeEnabled` off).
- **Responsible split:** movement *execution* is an `IMovementController` seam (default
  `NullMovementController` = draw-only). The tested decision engine is the value; real character
  steering needs movement/input hooks that can't be verified headless — an opt-in future addition.
- **Self-test now 92/92** — dodge out of an AOE to a valid in-bounds safe spot, stay put when safe,
  ignore far-future casts, report no-safe-spot when trapped, and the component→AIHints→pathfinder
  pipeline through a module.
- **Milestone met (as guidance):** a telegraphed AOE produces a computed safe-spot marker + arrow;
  auto-move is one `IMovementController` implementation away.

**Phase 7 — Content + polish (ongoing) — component library slice ✅**
- **Component library expansion (Core, game-free, tested):**
  - `SpreadFromCastTargets` / `StackWithCastTargets` — cast-target-driven markers that follow the
    live target actor; spread warns nearby non-targets, stack tells far players to pile in.
  - `Voidzone` — OID-tracked lingering puddles; draws them AND feeds them to the auto-dodge engine
    as standing forbidden zones (so dodging avoids puddles for free).
  - `Gaze` — look-away; warns when the source is in the player's front hemisphere.
  - `SimpleKnockbacks` — radial knockback; predicts the landing spot and flags off-arena landings.
- **Self-test now 101/101** — spread/stack targeting + hints, voidzone forbidden-zone integration,
  gaze facing check, knockback off-arena prediction.
- **Component library slice 2 ✅ (self-test 113/113):** added `ConcentricAOEs` (bullseye of
  concentric shapes; `AddSequence`/`AdvanceSequence`, one dangerous ring at a time or `showAll`
  preview), `Exaflare` + `SimpleExaflare` (marching lines of AOEs; imminent explosion risky, a few
  upcoming steps previewed), and `LineStack` (rect from source through a marked player; warns players
  off the line to stack in). All game-free/tested.
  - **Extractor wiring:** player-targeted **rect** shape → `LineStack` (confident mapping — line
    stacks are rectangles, spreads never are). Repeated **location** casts that march across ground
    (`Count ≥ 4`, spatial spread ≥ 4y) → flagged as an exaflare: still emitted as `SimpleAOEs` (draws
    each explosion correctly) but annotated `// exaflare? … consider Components.SimpleExaflare` so the
    author can upgrade to look-ahead. `ReplayAnalysis` now tracks per-action location-cast bounding
    spread; `ActionFact.ExaflareCandidate` carries the signal. `ConcentricAOEs` is author-only (a
    replay can't reliably tell a concentric sequence apart, so no false auto-wire).
  - Generated module re-verified: compiles clean against the real framework (smoke-tested then removed).
- **Extractor classification round 2 ✅ (self-test 117/117):** the generator now classifies four more
  mechanics, each keyed on a genuine replay signal at cast *resolution* (a new `CastFinished`
  correlation pass), matched to reliability:
  - **Stack** (confident): party *converges* on the marked player (≥2 others within 6y at resolution)
    → `PlayerMechanic.Stack` → `StackFromIcon` (if a stack icon preceded) or `StackWithCastTargets`.
  - **Gaze** (review): most players *face away* from the caster at resolution (rotation dot < 0) →
    `Gaze` — only for casts with no ground shape, so raidwides/AOEs are never mistaken for it.
  - **Knockback** (review): players are *shoved radially outward* just after resolution (deferred
    position snapshot vs. a beat later; ≥2 players pushed a consistent ≥3y) → `SimpleKnockbacks(dist)`.
  - **Concentric** (review): a run of ≥3 same-origin ground casts in quick succession (≤2.5s gaps,
    ≥2 distinct AIDs) → each ring stays a functional `SimpleAOEs` but is annotated `// concentric? …
    consider Components.ConcentricAOEs` (same flag-don't-fabricate treatment as exaflare).
  - `ReplayAnalysis` hooks `CastFinished`, tracks per-action clustering/gaze votes/knockback distance
    and a global ground-cast timeline; `ActionFact` carries `ConcentricCandidate`/`GazeCandidate`/
    `KnockbackDistance`. Generated module re-smoke-tested against the framework.
- **Real phase transitions ✅ (self-test 125/125):** the state machine is now a real runtime, not just
  activation comments. `StateMachineBuilder` records ordered `PhaseDef`s (components to activate on
  enter + an optional `Func<bool>` transition); helpers `TransitionOn(cond)` /
  `TransitionOnTargetable(oid)` / `TransitionOnPrimaryHP(fraction)`. `ModuleBase.BuildStates()` enters
  phase 0; `Update()` evaluates the current phase's transition each frame and, when it fires, deactivates
  the old phase's components and activates the next (type-based (de)activation; `AnyTargetable(oid)`
  helper). `TrivialPhase()` (single phase, no transition) reproduces the old "everything on" behaviour,
  so existing modules are unchanged.
  - **Extractor emission:** `PhaseFact` carries a `Trigger` kind + `TriggerOID`/`TriggerHP`/`TriggerMap*`.
    Three boundary signals are detected and merged into one time-ordered list (cast phases are assigned at
    build time against it): (a) a **new boss form becoming targetable** → `.TransitionOnTargetable
    ((uint)OID.<nextBoss>)`; (b) the **same boss going untargetable at an HP % and returning** → an HP
    gate → `.TransitionOnPrimaryHP(<fraction>)`; (c) a **one-shot mid-fight arena change** (ENVC map
    effect) → `.TransitionOnMapEffect(index, state)`. Generated as a real state machine (`this.Phase("P1")
    ….TransitionOn…; this.Phase("P2")…`), each transition carrying a confirm-TODO. Map-effect detection is
    gated hard (index fires exactly once, non-zero state, ≥5s in, not within 3s of a stronger boundary) so
    recurring telegraph tiles don't create false phases. Same-OID re-targetable is deliberately emitted as
    HP, not targetable (a targetable check on the current boss would fire immediately). Out of scope: pure
    behavioural HP thresholds (no untargetable event) and recurring/decorative map effects — not reliably
    separable from a replay. Runtime: `ModuleBase.SawMapEffect(index,state)` records observed effects for
    the `TransitionOnMapEffect` predicate.
- **Still ongoing (open-ended):** camera-relative radar rotation (deferred — north-up default kept);
  more boss modules; config UI polish; movement controller (auto-move execution).

---

## 5. Immediate next steps
1. Confirm the recommendation in §2 (clean core, referenced sync) — or adjust.
2. Stand up Phase 0 scaffolding by cloning Mercury's csproj/manifest and renaming.
3. Decide the LLM backend for `Generation/` (local vs. API) — affects Phase 5 only,
   safe to defer.

## Resolved decisions
- **Engine strategy:** clean core, referenced sync (§2) — confirmed.
- **Brand:** namespace `Minerva.*`, display name **Minerva**, commands **`/minerva`** and
  **`/mine`** (alias).
- **Toolchain:** `Dalamud.NET.Sdk/15.0.0` (API level 15), `net9.0-windows`, `Nullable`
  enabled, `LangVersion latest`, `AllowUnsafeBlocks` — **matches Mercury.** Uses the SDK's
  auto-references (Dalamud/ImGui/Lumina/FFXIVClientStructs) instead of BMR's manual
  `<Reference>` block.
- **Radar UX:** clone BMR's look for now; redesign later.
- **Code style:** follow Mercury — `sealed` classes, `this.`-prefixed members,
  `[PluginService]` `Service` injection via `PluginInterface.Create<Service>()`,
  `WindowSystem`, XML-doc comments.

## Still open / deferred
- **LLM backend** for `Generation/` (Phase 5): TBD — Claude API vs. local vs. pluggable.
  Design `Generation/` behind an interface so the backend is swappable.
