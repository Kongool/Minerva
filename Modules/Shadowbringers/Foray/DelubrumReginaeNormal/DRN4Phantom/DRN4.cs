// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN4Phantom;

sealed class VileWave(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VileWave, new AOEShapeCone(45f, 60f.Degrees()));
sealed class CreepingMiasma(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CreepingMiasma, new AOEShapeRect(50f, 6f));
sealed class MaledictionOfAgony(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.MaledictionOfAgony);
sealed class SwirlingMiasma(ModuleBase module) : Components.SimpleExaflare(module, new AOEShapeDonut(5f, 19f), (uint)AID.SwirlingMiasmaFirst, (uint)AID.SwirlingMiasmaRest, 6f, 1.6d, 8, 2, locationBased: true);

[ModuleInfo(CFCID = 760u, NameID = 9755u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class DRN4Phantom(WorldState ws, Actor primary) : Phantom(ws, primary);
