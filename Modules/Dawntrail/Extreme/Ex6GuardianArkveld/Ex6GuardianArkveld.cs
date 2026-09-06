// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex6GuardianArkveld;

sealed class Roar(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.Roar1, (uint)AID.Roar2, (uint)AID.Roar3]);
sealed class ForgedFury(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.ForgedFury1, (uint)AID.ForgedFury2, (uint)AID.ForgedFury3]);
sealed class WhiteFlash(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.WhiteFlash, 6f, 4, 4);
sealed class Dragonspark(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Dragonspark, 6f, 4, 4);
sealed class WildEnergy(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.WildEnergy, 6f);
sealed class WyvernsRadianceGuardianResonanceCircle(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.WyvernsRadianceCircle, (uint)AID.GuardianResonanceAOE], 6f);
sealed class WyvernsRadianceChainbladeCharge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WyvernsRadianceChainbladeCharge, 12f);
sealed class WyvernsOuroblade(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.WyvernsOuroblade1, (uint)AID.WyvernsOuroblade2,
(uint)AID.WyvernsOuroblade3, (uint)AID.WyvernsOuroblade4], new AOEShapeCone(40f, 90f.Degrees()));
sealed class SteeltailThrust(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SteeltailThrust1, (uint)AID.SteeltailThrust2], new AOEShapeRect(60f, 3f));
sealed class ChainbladeCharge(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.ChainbladeCharge, (uint)AID.ChainbladeCharge, 6f, 8.4d, PartyState.MaxPartySize, PartyState.MaxPartySize);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1044u, CFCID = 1044u, NameID = 14237u, PrimaryActorOID = (uint)OID.GuardianArkveld, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class Ex6GuardianArkveld(WorldState ws, Actor primary) : ModuleBase(ws, primary, arenaCenter, new ArenaBoundsCustom([new Polygon(arenaCenter, 20.030838f, 40)]))
{
    private static readonly WPos arenaCenter = new(100f, 100f);
}
