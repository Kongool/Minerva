// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Dungeon.D05CastrumAbania.D053Inferno;

public enum OID : uint
{
    Boss = 0x1AAE, // R4.5
    TwelfthLegionDeathClaw = 0x1AB0, // R1.0
    TwelfthLegionPacker = 0x1AAF, // R2.16
    Helper = 0x18D6
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target

    KetuCut = 8326, // Boss->self, no cast, single-target
    RahuCut = 8327, // Boss->self, no cast, single-target

    KetuSlash1 = 7974, // Boss->player, 2.0s cast, single-target, tankbuster
    KetuSlash2 = 8331, // Boss->player, 2.0s cast, single-target
    KetuSlash3 = 8332, // Boss->player, 2.0s cast, single-target

    RahuBlaster1 = 7977, // Boss->location, 3.0s cast, range 40+R width 6 rect
    RahuBlaster2 = 8334, // Boss->location, 2.0s cast, range 40+R width 6 rect
    RahuBlaster3 = 8335, // Boss->location, 2.0s cast, range 40+R width 6 rect

    KetuRahu = 7973, // Boss->self, 4.0s cast, single-target
    KetuCutter = 7975, // Helper->self, 4.0s cast, range 20+R 20-degree cone
    RahuRay = 7978, // Helper->player, no cast, range 10 circle

    KetuWave = 7976, // Helper->location, 4.0s cast, range 10 circle
    RahuComet1 = 7979, // Helper->location, 3.5s cast, range 40 circle, damage fall off AOE
    RahuComet2 = 8328, // Helper->location, 3.5s cast, range 40 circle, knockback 5 away from source
    RahuComet3 = 8329, // Helper->location, 3.5s cast, range 40 circle, knockback 10 away from source

    QuickCharge = 8487, // TwelfthLegionPacker->self, 15.0s cast, single-target
    DeathGrip = 8486 // TwelfthLegionDeathClaw->player, no cast, single-target, stuns player until is claw destroyed
}

public enum TetherID : uint
{
    Claw = 1 // TwelfthLegionDeathClaw->player
}

public enum IconID : uint
{
    Spreadmarker = 74, // player
}

class ClawTether(ModuleBase module) : Components.StretchTetherSingle(module, (uint)TetherID.Claw, 10f, needToKite: true);
class RahuRay(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Spreadmarker, (uint)AID.RahuRay, 10f, 4.1f);
class KetuSlash1(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.KetuSlash1);
class KetuSlash2(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.KetuSlash2);
class KetuSlash3(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.KetuSlash3);
class KetuCutter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.KetuCutter, new AOEShapeCone(20.5f, 10f.Degrees()));
class KetuWave(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.KetuWave, 10f);

class RahuBlaster(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(44.5f, 3f));
class RahuBlaster1(ModuleBase module) : RahuBlaster(module, (uint)AID.RahuBlaster1);
class RahuBlaster2(ModuleBase module) : RahuBlaster(module, (uint)AID.RahuBlaster2);
class RahuBlaster3(ModuleBase module) : RahuBlaster(module, (uint)AID.RahuBlaster3);

class RahuComet(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 15f);
class RahuComet1(ModuleBase module) : RahuComet(module, (uint)AID.RahuComet1);
class RahuComet2(ModuleBase module) : RahuComet(module, (uint)AID.RahuComet2);
class RahuComet3(ModuleBase module) : RahuComet(module, (uint)AID.RahuComet3);

class RahuCometKB(ModuleBase module, uint aid, float distance) : Components.SimpleKnockbacks(module, aid, distance, stopAtWall: true)
{
    private readonly KetuWave _aoe1 = module.FindComponent<KetuWave>()!;
    private readonly KetuCutter _aoe2 = module.FindComponent<KetuCutter>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0 && _aoe1.Casters.Count == 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var center = Center;
            hints.AddForbiddenZone(new SDInvertedCone(center, 20f, Angle.FromDirection(center - c.Origin), 20f.Degrees()), c.Activation);
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var count1 = _aoe1.Casters.Count;
        var aoes1 = CollectionsMarshal.AsSpan(_aoe1.Casters);
        for (var i = 0; i < count1; ++i)
        {
            if (aoes1[i].Check(pos))
            {
                return true;
            }
        }
        var count2 = _aoe2.Casters.Count;
        var aoes2 = CollectionsMarshal.AsSpan(_aoe1.Casters);
        for (var i = 0; i < count2; ++i)
        {
            if (aoes2[i].Check(pos))
            {
                return true;
            }
        }
        return false;
    }
}
class RahuComet2KB(ModuleBase module) : RahuCometKB(module, (uint)AID.RahuComet2, 5f);
class RahuComet3KB(ModuleBase module) : RahuCometKB(module, (uint)AID.RahuComet3, 10f);

class D053InfernoStates : StateMachineBuilder
{
    public D053InfernoStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<KetuWave>()
            .ActivateOnEnter<ClawTether>()
            .ActivateOnEnter<RahuComet1>()
            .ActivateOnEnter<RahuComet2>()
            .ActivateOnEnter<RahuComet3>()
            .ActivateOnEnter<RahuBlaster1>()
            .ActivateOnEnter<RahuBlaster2>()
            .ActivateOnEnter<RahuBlaster3>()
            .ActivateOnEnter<KetuSlash1>()
            .ActivateOnEnter<KetuSlash2>()
            .ActivateOnEnter<KetuSlash3>()
            .ActivateOnEnter<KetuCutter>()
            .ActivateOnEnter<RahuRay>()
            .ActivateOnEnter<RahuComet2KB>()
            .ActivateOnEnter<RahuComet3KB>();
    }
}

[ModuleInfo(CFCID = 242u, NameID = 6268u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D053Inferno(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Circle(new(282.5f, -27.25f), 19.51f)], [new Rectangle(new(277.157f, -7.933f), 20, 1.25f, -17.532f.Degrees())]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.TwelfthLegionPacker));
        Arena.Actors(Enemies((uint)OID.TwelfthLegionDeathClaw));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.TwelfthLegionDeathClaw => 2,
                (uint)OID.TwelfthLegionPacker => 1,
                _ => 0
            };
        }
    }
}