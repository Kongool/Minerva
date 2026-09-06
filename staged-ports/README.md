# Staged ports

BossmodReborn modules that are ported but do not compile against Minerva's API. Excluded from the build by
`Minerva.csproj` (`<Compile Remove="staged-ports/**/*.cs" />`), kept in the tree because the mechanical work
is already done.

**Do not delete these to tidy up.** Re-running `tools/port_bmr_module.py` reproduces the same files and the
same failures — what is missing is in Minerva, not in the port.

Current state: **809 modules landed, 261 staged**, of 1070 ported (75%). Build, tests (632) and validator
are green with no warnings. **All eight Ultimates land** — FRU, TOP, DSW1, DSW2, TEA, UCOB, UWU and DMU.

A separate `_superseded-by-bmr/` holds seven recording-derived Minerva modules retired in favour of the
BossmodReborn import of the same boss. They compile — they are not blocked work. Each covers *more*
distinct casts than the BMR module that replaced it (Metamorph 26 AIDs to CE202's 20, Pallmagia 15 to
CE204's 13) while BMR's run three to four times longer, meaning deeper per-mechanic resolution rather
than one component per cast. They are kept so that extra cast coverage can be merged back in.

---

## Measuring this folder correctly

There is a trap here that produced two wrong answers before it was spotted, so read this before you trust
any count you take yourself.

**Roslyn does not report method-body errors while declaration errors exist.** Re-include every staged file,
build, and you get 221 errors in 130 files — all `CS0246`/`CS0234`/`CS0115`, all declaration-level. Fix or
remove those, build again, and a second wave of 1878 errors appears in files that looked clean a moment ago:
`CS0103`, `CS1061`, `CS1501` — method bodies, which the compiler had not bound yet.

So a single build **understates** how much is blocked, and it understates it by an order of magnitude. The
first measurement of this folder concluded "only 150 files actually fail, ~6% blocked, every Occult Crescent
module compiles." All three claims were artefacts of stopping at the declaration layer. The true figure,
accumulated across every layer down to a green build, is **2117 distinct errors**.

To measure honestly: re-include everything, then loop `build → stage what failed → build` until green,
accumulating the error output from *every* pass. The union is the real blocker set. The union is what the
table below is built from.

**Stage by namespace, never by file.** A fight is landed only if all of its files compile. Move one
component out and its siblings stop resolving, so they get staged too, and the module class leaves its states
file behind staring at a bare namespace (`CS0118: 'X' is a namespace but is used like a type`). An early
folder-level pass staged 35 folders and produced 3016 new errors in one go. `nsstage.py` — build, map each
failing file to its namespace, move that whole namespace — converges in five passes with no thrash.

**Some of this still lands untouched.** Sastasha and Porta Decumana sat here for a day and compiled the
moment they were tried; `DAL3SaunionDawon`'s `Obey.cs` and `OneMind.cs` did the same. Before working on
anything, move its namespace back and build.

---

## What actually blocks them

The import started at **2117 distinct errors**, dominated by `CS1061` (no such member), `CS0103` (name not in
context) and `CS1501` (wrong overload) — all method-body codes, all invisible behind the declaration layer
until the trap above was understood. Four rounds of closing gaps took it to roughly **700**, and the landed
count from 553 to 736.

Almost none of it was hard. It is overwhelmingly **small API gaps between Minerva and BossmodReborn** — a
missing overload, a helper Minerva never needed until now — each one multiplied across dozens of files.
Ordered by leverage, with what is left:

| Gap | errors | files | What it is |
| --- | --- | --- | --- |
| `AIHints` intent fields | 58 | 21 | Mostly closed — `InteractWithTarget`, `Teleporters`, `GoalRectangle`/`GoalDonut`, `ForcedTarget`, `StatusesToCancel` and friends are now carried. What remains is call-shape mismatches, not missing members. |
| `AOEShapeCustom` | 19 | 5 | Arbitrary-polygon AOEs. Needs the polygon clipper below. |
| `WPos` passed where `Actor` expected | 23 | 14 | Call-shape differences, not a missing type — each needs a look at the site. |
| `ArenaChange` | 16 | 4 | Mid-fight arena resize. |
| `StrategyValues` / `Autorotation` | 9 | 7 | Autorotation plumbing. Minerva runs no rotation; **strip** these rather than port them. |
| long tail | ~700 | ~200 | No cluster above ~25 errors remains. Each is a handful of files wanting one member. |

**The config subsystem is built.** `Minerva.Core/Config/` now has `ConfigNode`, the `[PropertyDisplay]` /
`[PropertyCombo]` / `[PropertySlider]` / `[GroupDetails]` / `[GroupPreset]` attributes, the
`GroupAssignment` family, and a `ConfigRoot` registry reached as `Service.Config.Get<T>()`. All 37 of
BossmodReborn's module config classes are ported. `Windows/EncounterSettingsWindow.cs` renders them
generically from the attributes, and `EncounterConfigStore` persists them to `EncounterSettings.json`
beside the plugin config.

Two decisions worth knowing. `ConfigRoot.Instance` always hands out a defaulted node rather than throwing
or returning null, because modules read their config in their *constructors* and the offline validator
builds every module with no plugin and no file — otherwise every configurable module becomes unloadable
exactly where it gets checked. And loading a file copies values *into* the existing node instances rather
than replacing them, since a module captures its node once and would otherwise read a detached object
forever. Both are pinned by tests.

Three config nodes' `DrawCustom(UITree, WorldState)` overrides were dropped: they are ImGui code, which
`Minerva.Core` cannot reference, and both surviving bodies were UI niceties (a "set your role first"
warning, and a reorderable autorotation priority list Minerva does not act on). Their ordinary fields still
render.

**The polygon clipper is in.** BossmodReborn *vendors* Clipper2 rather than taking it from NuGet, which
removed the dependency question entirely — Minerva now vendors the same 8,156 lines under
`Minerva.Core/ThirdParty/Clipper2/`, byte-identical to upstream (Boost 1.0, recorded in
`THIRD-PARTY-NOTICES.txt`), plus BMR's wrappers under `Minerva.Core/Geometry/Polygon/`. The global usings
that code expects are supplied by `Minerva.Core.csproj` rather than by editing it, so it stays updatable by
replacing files. That unblocks `AOEShapeCustom`, `SDInvertedPolygonWithHoles`, boolean-composed arenas and
the Masked Carnivale `*Blockers` layouts.

One thing to know about it: **landed dipped from 822 to 809, and that is the clipper working, not failing.**
Those ~20 files previously failed at the declaration layer, so they were staged before their method bodies
were ever bound. With the clipper present they get past declarations and their body-level errors surface —
`PartyState.MaxAllianceSize`, `DonutSegmentV`, `AllDestroyed`, `ActionDefinitions` and friends — which
stages them, and namespace-level staging takes their fight-mates along. The next body-level pass wins them
back and more. `MaxAllianceSize` and `DonutSegmentV` are already done.

A duplicate `OperandType` turned up during this: Minerva had declared one with `Intersection` and `Xor`
**swapped** relative to BMR's. Nothing cast it to an integer, so nothing was broken, but it would have been
a genuinely nasty bug the first time a config or a save round-tripped one. BMR's is now the only copy.

**Fifty-four BMR files had never been ported at all.** That, not missing API, was the bulk of what remained:
re-including everything showed 85 declaration errors, and porting the missing files took it to 22. Among
them were four whole Forked Tower (Magic) Extreme bosses, the shared arena-bounds files several landed
modules reference (`DRSharedBounds`, `SugarRiotSharedBounds`, `BruteAbombinatorSharedBounds`), and a
scattering of per-fight `ArenaChange`/`Intermission` components whose absence read as a missing framework
type. **Check for unported files before concluding an API gap.**

**`AOEShapeArcCapsule` is in**, with `SDArcCapsule`/`SDInvertedArcCapsule` written from the geometry rather
than ported from BossmodReborn's hand-optimised version: inside the swept wedge the nearest centreline point
is at the same bearing so the distance is `|d - R|`; outside it, it is whichever end cap is nearer. That
unblocks `CE208 Familiar Tactics` and `CE211 Lost on the Wind`. `BitMatrix`, `SDCross` and `SDInvertedCross`
landed alongside it.

**The porter was corrupting every file it touched, and now does not.** `tools/port_bmr_module.py` held
literal control bytes where regex escapes belonged — `` had become a backspace (0x08) and `` a
start-of-heading (0x01), from an edit made in a non-raw string. It emitted `using Minerva<SOH>Pathfinding;`
into each file it ported. This had been "fixed" twice before by repairing the *output*; the source is fixed
now, and the tree contains no stray control bytes at all.

Also stripped, by the same policy as `QuestBattle`: the deep-dungeon **zone modules** (`ZoneModuleInfo` —
minimap, floor pathfinding, trap avoidance and auto-clear, which is navigation and auto-play rather than
encounter drawing) and the Eureka/Bozja NM-farm zone modules.

**The Ultimates are landed.** They took 232 errors' worth of small API gaps and one real feature, closed in
one pass. Nothing about them was structurally hard; the count was just the accumulated tail.

The feature was **waymarks** — `WaymarkState`, `WorldState.Waymarks`, replay `WAY+`/`WAY-` ops and a
`MarkingController` poll in the game sync. Field markers are how a party writes its plan onto the floor, and
several fights resolve against them ("the tower on A", "stack on 1"), so a module reading them resolves the
way that party actually plays rather than the way its author did. An unplaced marker reads as null rather
than the origin: (0,0) is a real spot on many arenas and would send people to a corner.

The rest were gaps: `Utils` (`MakeArray`/`GenArray`/`Swap`/`AlmostEqual`), `Intersect` (ported whole, minus
its two polygon-clipper overloads), `PartyState.Members`/`MaxAllies`/`PlayerSlot`, `AsSpan` and
`BoundSafeAt`, `ArenaBounds.MapResolution` (which `ArenaBoundsCustom` had been accepting as a constructor
argument and silently dropping), the `Arena` path builder (`PathArcTo`/`PathLineTo`/`PathStroke`,
`AddTriangleFilled`, `AddPolygon`, `WorldPositionToScreenPosition`), and the shape distances
`SDPrecisePosition`, `SDHalfPlane`, `SDInvertedUnion`, `SDInvertedCapsule`, `SDInvertedDonutSector`.

Three judgement calls worth knowing:

- **`RaidByEnmity` is a guess and says so.** Minerva does not mirror the enmity table, so it orders by
  "whoever the boss is targeting, then tanks, then DD, then healers, ties by slot" — which is what
  BossmodReborn falls back to when it cannot read the table either. Right in the ordinary case, wrong during
  a tank swap, which is exactly when a buster is most likely in the air. The XML doc says so at the call site.
- **`Actor.PendingKnockbacks` is always empty and `PendingHPRatio` is just `HPRatio`.** BMR predicts pending
  effects from cast events; Minerva deliberately does not. A module asking "is a knockback already on me" is
  told no and acts a moment later, rather than being handed invented effects that may not land.
- **`BitMask.NumSetBits` and `Any` became methods**, and `Adds.OID` became `AddOID`, because a name cannot be
  both a property and a method and the ports outnumbered the internal callers. `FRUAI.cs` was deleted
  outright — it is a rotation module, the same call as `QuestBattle`.

`ArcList`, `DisjointSegmentList`, `SDCapsule` and `SDUnion` came up twice each during this work: two of them
already existed in Minerva and my ports duplicated them. Check before adding.

**`QuestBattle` was on this list by mistake, and is now gone.** The name suggests a module base for solo
quest duties, alongside `OpenWorldFate` — it is not. `QuestBattle.UnmanagedRotation` is a hand-written
*rotation* for the temporary jobs those duties hand you: it picks a target, sets `ForcedTarget`, and pushes
actions onto `ActionsToExecute`. `QuestBattle.RotationModule<R>` is the component that runs one each frame.
That is the `StrategyValues` bucket — strip, do not port — and 29 errors across 18 files were spent on it.

`tools/strip_questbattle.py` did it: 24 rotation classes deleted, and the 5 `RotationModule` subclasses that
override `AddAIHints` to do real work re-based onto `ModuleComponent` with the delegating `base` call
dropped. Those five were worth keeping — they mark which add is invincible, which one is on you, and where
one should be dragged, all of which Minerva records. Every quest module's actual mechanics were always fine;
they were held out by the rotation half of the same file. **80 quest modules now land, 23 remain.**

Closed since the last count, in rough order of what each was worth — this is what took the tree from 553
landed to 736, and the accumulated error total from 2117 to about 700:

- **`ActorEnumeration`** — BMR's whole actor-query vocabulary (`InRadius`, `InRadiusExcluding`, `Exclude`,
  `SortedByRange`, `Tethered`, `InShape`, `ClockOrder`, `Farthest`, `CountByCondition`). `Raid.WithoutSlot().InRadius(...)`
  is how ported fights are written; missing it was the single largest share of body-level errors.
- **`PartyState.WithSlot`** 3-arg overload (144 errors, 82 files) and `MaxPartySize`.
- **state-machine surface** — `PhaseDef.DeactivateOnEnter`/`OnExit`, `ActivateOnEnter(condition)`, `CastStartMulti`.
- **`Adds.OID` renamed to `AddOID`** — the inherited member was shadowing each module's own namespace-level
  `OID` enum, so `Components.Adds(module, (uint)OID.Whatever)` failed in 37 files. A name collision, not a
  missing feature, and invisible until you read the error properly.
- **`ModuleComponent.NumCasts`** moved up from `CastCounter`: ported state machines ask *any* component for
  it, including ones that track their mechanic through tethers or icons.
- **`Arena`** zone fills (`ZoneCircle`/`ZoneRect`/`ZoneDonut`/`ZoneCone`), outlines, and `ClampToBounds`.
- **`AIHints.Enemy`** tank-AI fields, and `AIHints`' non-positional intent fields.
- **`ActorCastInfo.IsSpell`/`NPCRemainingTime`**, `ActorModelState`, `ClassShared.AID`, `ClassCategory`/
  `IsSupport`/`IsDD`, `BitMask.Reset`/`HighestSetBit`, `CosPI`, `Angle.DoublePI`, `WPos.InRect(WDir)`,
  `WDir.Rotate(WDir)`, the remaining `Colors` palette slots, and Masked Carnivale's shared `Layouts`.
- **`ArcList` + `DisjointSegmentList`** — disjoint circle-arc sets, for picking safe spots along an arena
  edge or a melee ring. Plus `Angle.Acos`/`Asin`, clamped to their domain: the cosine rule fed
  near-degenerate geometry lands a hair outside [-1, 1], and an unclamped `Acos` answers NaN, which
  then poisons every angle it touches instead of failing where you can see it.
- **`ModuleBase.StateMachine`** — exposes `ActivePhaseIndex` under BossmodReborn's name, alongside
  `StateMachine.PhaseHint` and `PhaseDef.SetHint`. The property and its type deliberately share the
  name `StateMachine`, so C#'s "Color Color" rule lets `StateMachine.ActivePhaseIndex` (instance
  member) and `StateMachine.PhaseHint` (nested type) both resolve, exactly as ported files spell them.
- **`ModuleRegistry.InferBossOID`** — it only recognised an enum member literally named `Boss`; BMR names it
  after the boss, so 467 of 553 landed modules registered under OID 0 and collided. Most of two Dawntrail
  dungeons were dark because of it. Inference cannot cover everything: `ZenosP1`/`ZenosP2` share a
  namespace and an `Enums.cs` holding `BossP1`/`BossP2`, so both inferred the first member and collided.
  That is the one case in the tree where two modules share a namespace and an OID enum, and it is what
  `PrimaryActorOID` is for — set it explicitly rather than teaching the inference another convention.

Earlier: the state-timeline API's first wave (101 errors, 53 files — `StateMachineTimeline.cs`), 25 dead
`using` directives, four `SDKnockback*` stubs with no `Distance(WPos)` body, and the module-level virtuals
`DrawArenaForeground` / `CheckReset` / `ShouldPrioritizeAllEnemies`.

---

## Occult Crescent status

The zone worth caring about first, since every recording comes from it — and the zone this import exists to
make self-sufficient. Three North Horn bosses have no BossmodReborn module at all, so the
recording-to-module pipeline has to stand on its own there.

| | landed | staged |
| --- | --- | --- |
| Critical Engagements | 18 | 14 |
| Foray FATEs | 18 | 3 |
| Forked Tower (Blood) | 1 | 3 |
| Forked Tower (Magic) | 0 | 2 |

The Forked Tower modules are the hardest content in the zone and the furthest from compiling. They are also
the least useful until the rest works.

The classification comes from `ModuleGroup` on `[ModuleInfo]`: `CriticalEngagement` resolves its name through
the **DynamicEvent** sheet, `ForayFATE` through the **Fate** sheet. That split matters beyond the menu — a CE
seals its arena, so nothing wanders in and the bounds are trustworthy; a FATE is open world, so trash gets
dragged into cast radii and the arena has to be bounded by the FATE radius or the player will chase a dodge
straight out of the event.

---

## Landing them: two steps, never one

```bash
python tools/land_staged.py       # copy in, build-drop until green
python tools/converge_modules.py  # alternate build + validator until BOTH are quiet
```

**Landing alone is not safe, and the failure is invisible.** These scripts land per FILE, so a fight whose
`XStates.cs` still fails compiles and lands its `X.cs` anyway. The module then registers, claims the boss
and draws **nothing** — strictly worse than not landing, and a plain `dotnet build` is green throughout.
That is what the "stage by namespace, never by file" rule above is protecting against; obey it by running
the second script, which unlands exactly the modules `Minerva.Validate` names.

Two traps inside that, both found the hard way:

- **A folder is not a fight.** Flat category folders — `Dawntrail/Foray/CriticalEngagement` and friends —
  hold dozens of independent fights. Unlanding a whole folder because one file in it is staged destroyed 29
  Critical Engagement modules over one unrelated blocker. Unland the module the validator names, nothing
  else, then let the build drop whatever siblings that orphans.
- **`dotnet run --project Minerva.Validate` does not rebuild the plugin.** It loads `bin/Debug/Minerva.dll`
  by path, so a validator call that is not preceded by a real `dotnet build` reports the tree as it was
  before your last change — forever, if you loop on it. `converge_modules.py` builds first for this reason.

## Working through them

```bash
dotnet build
```

```bash
dotnet run --project Minerva.Tests
```

The validator reports two classes of silent breakage against the real plugin assembly, and both have caught
real bugs — read its warnings, not just its exit code:

```bash
dotnet run --project Minerva.Validate -- <recordings-folder> --modules bin/Debug/Minerva.dll --quiet
```

- **two modules claiming one boss** — the registry takes whichever it reaches first, so one module's
  mechanics are never drawn. Two causes. The same fight can exist under two names: BMR names Occult Crescent
  modules after the FATE (`WavedAway` is Arch Kelpie, `ScaleModel` is Demi-Medusa), Minerva names
  recording-derived ones after the boss — only the OID is reliable, so searching BMR by boss name will
  mislead you. Or several modules can infer no OID at all and collide on 0, which took out most of two
  Dawntrail dungeons until `ModuleRegistry.InferBossOID` learned BMR's naming conventions.
- **a module with no matching `<name>States` class** — BMR names its states class after the *file* and binds
  it explicitly with `StatesType`, which the porter drops. The module registers, activates, and draws
  nothing. Four modules were dead this way before the check existed.

A green build, green tests and a green validator still do not prove the plugin loads. Only `dalamud.log`
does — a `DaedalusRosterIPC` field initializer ran before `Create<Service>()` and took the whole plugin down
with all three checks green. Construction order is not something the compiler can see for you.
