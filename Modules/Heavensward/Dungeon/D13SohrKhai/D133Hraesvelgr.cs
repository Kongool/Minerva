// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Dungeon.D13SohrKhai.D133Hraesvelgr;

public enum OID : uint
{
    Boss = 0x3D17, // R19.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 32142, // Boss->player, no cast, single-target

    Wyrmclaw = 32141, // Boss->player, 5.0s cast, single-target, tankbuster
    HallowedWings1 = 32136, // Boss->self, 6.0s cast, range 50 width 22 rect, left
    HallowedWings2 = 32137, // Boss->self, 6.0s cast, range 50 width 22 rect, right
    HolyStorm = 32127, // Boss->self, 5.0s cast, range 40 circle, raidwide
    HallowedDive = 32131, // Boss->self, 6.0s cast, range 40 width 20 rect
    FrigidDive = 32134, // Boss->self, 6.0s cast, range 40 width 20 rect
    HolyOrbFirst = 32129, // Helper->self, 5.0s cast, range 6 circle, exaflares
    HolyOrbRest = 32130, // Helper->self, no cast, range 6 circle
    AkhMornFirst = 32132, // Boss->players, 5.0s cast, range 6 circle, stack 5 (first) or 6 hits
    AkhMornRest = 32133, // Boss->players, no cast, range 6 circle
    HolyBreathVisual = 32138, // Boss->self, 5.0+1,0s cast, single-target, spread
    HolyBreath = 32139, // Helper->player, 6.0s cast, range 6 circle
    DiamondStorm = 32128, // Boss->self, 5.0s cast, range 40 circle, raidwide
    FrostedOrb = 32135 // Helper->self, 5.0s cast, range 6 circle
}

public enum IconID : uint
{
    Spreadmarker = 311 // player->self
}

abstract class HallowedWings(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(50f, 11f));
class HallowedWings1(ModuleBase module) : HallowedWings(module, (uint)AID.HallowedWings1);
class HallowedWings2(ModuleBase module) : HallowedWings(module, (uint)AID.HallowedWings2);

class Wyrmclaw(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Wyrmclaw);
class HolyStorm(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HolyStorm);
class DiamondStorm(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DiamondStorm);

abstract class Dive(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(40f, 10f));
class HallowedDive(ModuleBase module) : Dive(module, (uint)AID.HallowedDive);
class FrigidDive(ModuleBase module) : Dive(module, (uint)AID.FrigidDive);

class FrostedOrb(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FrostedOrb, 6f);

class AkhMorn(ModuleBase module) : Components.UniformStackSpread(module, 6f, default, 4, 4)
{
    private int numCasts;
    private bool first = true;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.AkhMornFirst)
            AddStack(World.Actors.Find(spell.TargetID)!, Module.CastFinishAt(spell));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.AkhMornFirst or (uint)AID.AkhMornRest)
        {
            ++numCasts;
            if (first && numCasts == 5 || numCasts == 6)
            {
                Stacks.Clear();
                numCasts = 0;
                first = false;
            }
        }
    }
}

class HolyOrb(ModuleBase module) : Components.Exaflare(module, 6f)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HolyOrbFirst)
            Lines.Add(new(caster.Position, 6.8f * caster.Rotation.ToDirection(), Module.CastFinishAt(spell), 1d, 5, 3));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.HolyOrbFirst or (uint)AID.HolyOrbRest)
        {
            var count = Lines.Count;
            var pos = caster.Position;
            for (var i = 0; i < count; ++i)
            {
                var line = Lines[i];
                if (line.Next.AlmostEqual(pos, 1f))
                {
                    AdvanceLine(line, pos);
                    if (line.ExplosionsLeft == 0)
                        Lines.RemoveAt(i);
                    return;
                }
            }
        }
    }
}

class HolyBreath(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Spreadmarker, (uint)AID.HolyBreath, 6f, 6f);

class ThinIce(ModuleBase module) : Components.ThinIce(module, 11f, true, stopAtWall: true)
{
    private readonly FrostedOrb _aoe1 = module.FindComponent<FrostedOrb>()!;
    private readonly FrigidDive _aoe2 = module.FindComponent<FrigidDive>()!;

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        if (_aoe2.Casters.Count != 0 && _aoe2.Casters[0].Check(pos))
            return true;
        var count = _aoe1.Casters.Count;
        var aoes = CollectionsMarshal.AsSpan(_aoe1.Casters);
        for (var i = 0; i < count; ++i)
        {
            ref readonly var aoe = ref aoes[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return false;
    }
}

class D133HraesvelgrStates : StateMachineBuilder
{
    public D133HraesvelgrStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HallowedWings1>()
            .ActivateOnEnter<HallowedWings2>()
            .ActivateOnEnter<Wyrmclaw>()
            .ActivateOnEnter<HolyStorm>()
            .ActivateOnEnter<DiamondStorm>()
            .ActivateOnEnter<HallowedDive>()
            .ActivateOnEnter<FrigidDive>()
            .ActivateOnEnter<FrostedOrb>()
            .ActivateOnEnter<AkhMorn>()
            .ActivateOnEnter<HolyOrb>()
            .ActivateOnEnter<HolyBreath>()
            .ActivateOnEnter<ThinIce>();
    }
}

[ModuleInfo(CFCID = 171u, NameID = 4954u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D133Hraesvelgr(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(400f, -400f), 19.5f, 36)]);
}