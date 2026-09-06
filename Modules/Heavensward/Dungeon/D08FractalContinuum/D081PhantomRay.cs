// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Dungeon.D08FractalContinuum.D081PhantomRay;

public enum OID : uint
{
    Boss = 0x1012, // R2.8
    Helper = 0xD25
}

public enum AID : uint
{
    AutoAttack = 871, // Boss->player, no cast, single-target

    Overclock = 3967, // Boss->self, no cast, single-target
    DoubleSeverVisual1 = 3963, // Boss->self, 2.5s cast, single-target
    DoubleSeverVisual2 = 3964, // Boss->self, 2.5s cast, single-target
    DoubleSever = 3965, // Helper->self, 3.0s cast, range 30+R 90-degree cone
    AtmosphericCompression = 3968, // Helper->location, 3.0s cast, range 5 circle
    AtmosphericDisplacement = 3966, // Boss->self, no cast, range 80+R circle
    RapidSever = 3962 // Boss->players, 3.0s cast, single-target, tankbuster
}

class DoubleSever(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DoubleSever, new AOEShapeCone(30.5f, 45f.Degrees()))
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = Casters.Count;
        if (count == 0)
            return;
        base.AddAIHints(slot, actor, assignment, hints);
        ref var aoe = ref Casters.Ref(0);
        // stay close to the origin
        hints.AddForbiddenZone(new SDInvertedCircle(aoe.Origin, 3f), aoe.Activation);
    }
}
class AtmosphericCompression(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AtmosphericCompression, 5f);
class RapidSever(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.RapidSever);

class D081PhantomRayStates : StateMachineBuilder
{
    public D081PhantomRayStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DoubleSever>()
            .ActivateOnEnter<AtmosphericCompression>()
            .ActivateOnEnter<RapidSever>();
    }
}

[ModuleInfo(CFCID = 35u, NameID = 3428u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D081PhantomRay(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(121.858f, 45.368f), 19.5f, 24)],
    [new Rectangle(new(139.011f, 35.299f), 20f, 1.25f, -60.946f.Degrees()), new Rectangle(new(104.832f, 55.061f), 20f, 1.25f, -58.455f.Degrees())]);
}
