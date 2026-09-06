// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M03NBruteBomber;

sealed class BrutalImpact(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.BrutalImpactFirst);
sealed class KnuckleSandwich(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.KnuckleSandwich, 6f);
sealed class BrutalLariat(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.BrutalLariat1, (uint)AID.BrutalLariat2], new AOEShapeRect(50f, 17f));

sealed class MurderousMist(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MurderousMist, new AOEShapeCone(40f, 135f.Degrees()));
sealed class BrutalBurn(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.BrutalBurn, 6f, 8, 8);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 989u, CFCID = 989u, NameID = 13356u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class M03NBruteBomber(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(15f));
