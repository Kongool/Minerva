// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A22AlthykNymeia;

class MythrilGreataxe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MythrilGreataxe, new AOEShapeCone(71f, 30f.Degrees()));
class Hydroptosis(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.HydroptosisAOE, 6f);

[ModuleInfo(CFCID = 911u, NameID = 12244u, PrimaryActorOID = (uint)OID.Althyk, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class A22AlthykNymeia(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(50f, -750f), new ArenaBoundsSquare(25f))
{
    private Actor? _nymeia;

    public Actor? Althyk() => PrimaryActor;
    public Actor? Nymeia() => _nymeia;

    protected override void UpdateModule()
    {
        _nymeia ??= GetActor((uint)OID.Nymeia);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_nymeia);
    }
}
