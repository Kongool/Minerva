// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.Job.DarkKnight.TheOrphansAndTheBrokenBlade;

public enum OID : uint
{
    Boss = 0x1C5E,
    Helper = 0x233C
}

public enum AID : uint
{
    ShadowOfDeath1 = 8459, // 1C5F->location, 3.0s cast, range 5 circle
    HeadsmansDelight = 8457, // Boss->1C5C, 5.0s cast, range 5 circle
    SpiralHell = 8453, // 1C5F->self, 3.0s cast, range 40+R width 4 rect
    HeadmansDelight = 9298 // 1C5F->player/1C5C, no cast, single-target
}

class SpiralHell(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SpiralHell, new AOEShapeRect(40, 2));
class HeadsmansDelight(ModuleBase module) : Components.GenericStackSpread(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HeadsmansDelight && World.Actors.Find(spell.TargetID) is Actor tar)
            Stacks.Add(new(tar, 5, activation: Module.CastFinishAt(spell)));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.HeadmansDelight)
            Stacks.Clear();
    }
}
class ShadowOfDeath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ShadowOfDeath1, 5);
class DarkChain(ModuleBase module) : Components.Adds(module, 0x1C60)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        hints.PrioritizeTargetsByOID(0x1C60, 5);
    }
}

class OmpagneDeepblackStates : StateMachineBuilder
{
    public OmpagneDeepblackStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ShadowOfDeath>()
            .ActivateOnEnter<DarkChain>()
            .ActivateOnEnter<HeadsmansDelight>()
            .ActivateOnEnter<SpiralHell>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68453u, NameID = 6300u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class OmpagneDeepblack(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-166.8f, 290), new ArenaBoundsCircle(20))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}
