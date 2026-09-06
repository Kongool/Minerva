// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.DSW1;

sealed class EmptyDimension(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EmptyDimension, new AOEShapeDonut(6f, 70f));
sealed class FullDimension(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FullDimension, 6f);
sealed class HoliestHallowing(ModuleBase module) : Components.CastHint(module, (uint)AID.HoliestHallowing, "Interrupt!");

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 788u, CFCID = 788u, NameID = 11319u, PrimaryActorOID = (uint)OID.SerAdelphel, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class DSW1(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(22f))
{
    private Actor? _grinnaux;
    private Actor? _charibert;
    public Actor? SerAdelphel() => PrimaryActor;
    public Actor? SerGrinnaux() => _grinnaux;
    public Actor? SerCharibert() => _charibert;

    protected override void UpdateModule()
    {
        _grinnaux ??= GetActor((uint)OID.SerGrinnaux);
        _charibert ??= GetActor((uint)OID.SerCharibert);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_grinnaux);
        Arena.Actor(_charibert);
    }
}
