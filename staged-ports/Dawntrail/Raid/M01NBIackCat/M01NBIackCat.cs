// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M01NBlackCat;

sealed class BloodyScratch(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.BloodyScratch);
sealed class BiscuitMaker(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.BiscuitMaker);
sealed class Clawful(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Clawful, 5f, 8, 8);
sealed class GrimalkinGale(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.GrimalkinGale, 5f);
sealed class Overshadow(ModuleBase module) : Components.LineStack(module, aidMarker: (uint)AID.OverShadowMarker, (uint)AID.Overshadow, 5.3d, 60f, 2.5f);

[ModuleInfo(CFCID = 985u, NameID = 12686u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public class M01NBlackCat(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaChanges.ArenaCenter, ArenaChanges.DefaultBounds);
