# Out of scope, deliberately

Ports that compile against BossmodReborn subsystems Minerva has decided not to have. They are kept
rather than deleted so the decision is visible, and so a later change of mind starts from a port rather
than from BossmodReborn.

## DeepDungeon/AutoClear.cs

BossmodReborn's deep dungeon *assistant*: pomander tracking, floor state, auto-clear routing
(`DeepDungeonState`, `PomanderID`). Minerva wants only two things from a deep dungeon — trash AOE
dodging and the boss modules — and both work through the ordinary boss-module path with no run state.

Worth knowing what this file was costing: on its own it produced **2,484 of the compile errors** in the
staged tree, which made `DeepDungeonState` look like the single largest blocker by a factor of five.
It was one file. Measuring blockers by error count rather than by file count is how that happened.

## Autorotation/

Ports that derive from BossmodReborn's `RotationModule` / `AIRotationModule` — its autorotation framework
(`StrategyValues`, `RotationModuleDefinition`). Minerva does not press buttons and will not grow an
autorotation surface; Daedalus does rotation, and `Minerva.RequestPositional` plus the `Minerva.Hints.*`
gates are the seam between them.
