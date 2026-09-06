// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A12Fafnir;

sealed class SpikeFlail(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SpikeFlail, new AOEShapeCone(80f, 135f.Degrees()))
{
    public override bool KeepOnPhaseChange => true;
}

sealed class Touchdown(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Touchdown, 24f)
{
    public override bool KeepOnPhaseChange => true;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return Casters.Count != 0 && (Module.FindComponent<DragonBreath>()?.AOE.Length == 0 || Bounds != A12Fafnir.FireArena) ? CollectionsMarshal.AsSpan(Casters) : [];
    }
}

sealed class DragonBreath(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.DragonBreath)
{
    public override bool KeepOnPhaseChange => true;
    public AOEInstance[] AOE = [];

    private static readonly AOEShapeDonut donut = new(16f, 30f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => AOE;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.OffensivePostureDragonBreath)
        {
            NumCasts = 0;
            AOE = [new(donut, Center, default, Module.CastFinishAt(spell, 1.2d))];
        }
    }

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (state == 0x00040008u && actor.OID == (uint)OID.FireVoidzone)
        {
            AOE = [];
        }
    }
}
