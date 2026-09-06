// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V14ZelessGah;

sealed class PureFire(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PureFire, 6f);

sealed class FiresteelFracture(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.FiresteelFracture, new AOEShapeCone(50f, 45f.Degrees()), endsOnCastEvent: true, tankbuster: true);

sealed class ShowOfStrength(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ShowOfStrength);
sealed class CastShadow(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.CastShadow1, (uint)AID.CastShadow2], new AOEShapeCone(50f, 15f.Degrees()), 6, 12);

public abstract class VCZelessGah(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(289f, -105f), new ArenaBoundsRect(24.5f, 29.5f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 868u, CFCID = 868u, NameID = 11393u, PrimaryActorOID = (uint)OID.ZelessGah, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class V14ZelessGah(WorldState ws, Actor primary) : VCZelessGah(ws, primary);
