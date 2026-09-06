// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Quest.MSQ.AsTheHeavensBurn.P1TerminusIdolizer;

public enum OID : uint
{
    Boss = 0x35E9, // R4.95
    TerminusDetonator = 0x35E5, // R2.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 26994, // Boss->Estinien, no cast, single-target

    DeadlyTentacles = 26998, // Boss->Estinien, no cast, single-target
    TentacleWhip = 27005, // Boss->self, no cast, single-target
    Shout = 27000, // Boss->self, no cast, single-target
    Whack1 = 27008, // Boss->Estinien, no cast, single-target
    Whack2 = 27009, // Boss->Estinien, no cast, single-target

    DeadlyCharge = 26995, // Boss->location, 5.0s cast, width 10 rect charge
    GriefOfParting = 26996, // Boss->self, 5.0s cast, range 40 circle
    DeadlyTentaclesTB = 26997, // Boss->Estinien, 5.0s cast, single-target
    TentacleWhipLFirst = 27004, // Boss->self, 5.0s cast, range 60 180-degree cone
    TentacleWhipRSecond = 27006, // Helper->self, 7.0s cast, range 60 180-degree cone
    TentacleWhipRFirst = 27001, // Boss->self, 5.0s cast, range 60 180-degree cone
    TentacleWhipLSecond = 27003, // Helper->self, 7.0s cast, range 60 180-degree cone
    SelfDestruct = 26991, // TerminusDetonator->self, no cast, range 6 circle
    Petrifaction = 26999, // Boss->self, 4.0s cast, range 60 circle

    Whack = 27007, // Boss->Estinien, 5.0s cast, single-target
}

public enum TetherID : uint
{
    BombTether = 17 // TerminusDetonator->Estinien
}

class DeadlyCharge(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.DeadlyCharge, 5f);
class GriefOfParting(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.GriefOfParting);
class DeadlyTentaclesTB(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.DeadlyTentaclesTB);
class TentacleWhip : Components.SimpleAOEGroups
{
    public TentacleWhip(ModuleBase module) : base(module, [(uint)AID.TentacleWhipRFirst, (uint)AID.TentacleWhipRSecond,
    (uint)AID.TentacleWhipLFirst, (uint)AID.TentacleWhipLSecond], new AOEShapeCone(60f, 90f.Degrees()), expectedNumCasters: 2)
    {
        MaxDangerColor = 1;
        MaxRisky = 1;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (Casters.Count != 2)
            return;
        // make ai stay close to boss to ensure successfully dodging the combo
        var aoe = Casters[0];
        hints.AddForbiddenZone(new SDInvertedRect(Module.PrimaryActor.Position, aoe.Rotation, 2f, 2f, 20f), aoe.Activation);
    }
}

class SelfDestruct(ModuleBase module) : Components.GenericStackSpread(module)
{
    private uint numCasts;

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (Stacks.Count == 0 && tether.ID == (uint)TetherID.BombTether)
            Stacks.Add(new(World.Actors.Find(tether.Target)!, 3f, 2, 2, activation: World.FutureTime(9.1d)));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SelfDestruct)
        {
            if (++numCasts == 6)
                Stacks.Clear();
        }
    }
}
class Petrifaction(ModuleBase module) : Components.CastGaze(module, (uint)AID.Petrifaction);
class Whack(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Whack);

class TerminusIdolizerStates : StateMachineBuilder
{
    public TerminusIdolizerStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DeadlyCharge>()
            .ActivateOnEnter<GriefOfParting>()
            .ActivateOnEnter<TentacleWhip>()
            .ActivateOnEnter<DeadlyTentaclesTB>()
            .ActivateOnEnter<SelfDestruct>()
            .ActivateOnEnter<Petrifaction>()
            .ActivateOnEnter<Whack>()
            ;
    }
}

[ModuleInfo(CFCID = 804u, NameID = 10932u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class TerminusIdolizer(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-300.75f, 151.46f), 19.5f, 20)]);
}
