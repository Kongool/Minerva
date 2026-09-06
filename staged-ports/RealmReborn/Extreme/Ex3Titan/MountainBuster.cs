// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex3Titan;

class MountainBuster : Components.Cleave
{
    public MountainBuster(ModuleBase module) : base(module, (uint)AID.MountainBuster, new AOEShapeCone(21.25f, 60.Degrees())) // TODO: verify angle
    {
        NextExpected = module.StateMachine.NextTransitionWithFlag(StateMachine.StateHint.Tankbuster);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        var boss = hints.PotentialTargets.Find(e => (OID)e.Actor.OID == OID.Boss);
        boss?.AttackStrength += 0.25f;
    }
}

class RockBuster : Components.Cleave
{
    public RockBuster(ModuleBase module) : base(module, (uint)AID.RockBuster, new AOEShapeCone(11.25f, 60.Degrees())) // TODO: verify angle
    {
        NextExpected = module.StateMachine.NextTransitionWithFlag(StateMachine.StateHint.Tankbuster);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        var boss = hints.PotentialTargets.Find(e => (OID)e.Actor.OID == OID.TitansHeart);
        boss?.AttackStrength += 0.25f;
    }
}
