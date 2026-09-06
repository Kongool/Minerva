// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.VowsOfVirtueDeedsOfCruelty;

public enum OID : uint
{
    Boss = 0x2C85, // R6.0
    TerminusEstVisual = 0x2C98, // R1.0
    SigniferPraetorianus = 0x2C9A, // R0.5
    LembusPraetorianus = 0x2C99, // R2.4
    MagitekBit = 0x2C9C, // R0.6
    Helper = 0x233C
}

public enum AID : uint
{
    LoadData = 18786, // Boss->self, 3.0s cast, single-target
    AutoAttack = 870, // Boss/LembusPraetorianus->player, no cast, single-target
    MagitekRayRightArm = 18783, // Boss->self, 3.2s cast, range 45+R width 8 rect
    MagitekRayLeftArm = 18784, // Boss->self, 3.2s cast, range 45+R width 8 rect
    SystemError = 18785, // Boss->self, 1.0s cast, single-target
    AngrySalamander = 18787, // Boss->self, 3.0s cast, range 40+R width 6 rect
    FireII = 18959, // SigniferPraetorianus->location, 3.0s cast, range 5 circle
    TerminusEstBossCast = 18788, // Boss->self, 3.0s cast, single-target
    TerminusEstLocationHelper = 18889, // Helper->self, 4.0s cast, range 3 circle
    TerminusEstVisual = 18789, // TerminusEstVisual->self, 1.0s cast, range 40+R width 4 rect
    HorridRoar = 18779, // 2CC5->location, 2.0s cast, range 6 circle, this is your own attack. It spawns an aoe at the location of any enemy it initally hits
    GarleanFire = 4007, // LembusPraetorianus->location, 3.0s cast, range 5 circle
    MagitekBit = 18790, // Boss->self, no cast, single-target
    MetalCutterCast = 18793, // Boss->self, 6.0s cast, single-target
    MetalCutter = 18794, // Helper->self, 6.0s cast, range 30+R 20-degree cone
    AtomicRayCast = 18795, // Boss->self, 6.0s cast, single-target
    AtomicRay = 18796, // Helper->location, 6.0s cast, range 10 circle
    MagitekRayBit = 18791, // MagitekBit->self, 6.0s cast, range 50+R width 2 rect
    SelfDetonate = 18792, // MagitekBit->self, 7.0s cast, range 40+R circle, enrage if bits are not killed before cast
}

class MagitekRay(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.MagitekRayRightArm, (uint)AID.MagitekRayLeftArm], new AOEShapeRect(45f, 4f));
class AngrySalamander(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AngrySalamander, new AOEShapeRect(40f, 3f));
class TerminusEstRects(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect _shape = new(40f, 2f);
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TerminusEstLocationHelper)
        {
            var act = Module.CastFinishAt(spell);
            _aoes.AddRange(
            [
                new(_shape, spell.LocXZ, spell.Rotation, act),
                new(_shape, spell.LocXZ, spell.Rotation - 90f.Degrees(), act),
                new(_shape, spell.LocXZ, spell.Rotation + 90f.Degrees(), act)
            ]);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.TerminusEstVisual)
        {
            _aoes.Clear();
            ++NumCasts;
        }
    }
}
class TerminusEstCircle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TerminusEstLocationHelper, 3f);
class FireII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FireII, 5f);
class GarleanFire(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GarleanFire, 5f);
class MetalCutter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MetalCutter, new AOEShapeCone(30f, 10f.Degrees()));
class MagitekRayBits(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagitekRayBit, new AOEShapeRect(50f, 1f));
class AtomicRay(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AtomicRay, 10f);
class SelfDetonate(ModuleBase module) : Components.CastHint(module, (uint)AID.SelfDetonate, "Enrage if bits are not killed before cast");

class AutoEstinien(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var h = hints.PotentialTargets[i];
            h.Priority = h.Actor.OID switch
            {
                (uint)OID.MagitekBit => 2,
                (uint)OID.LembusPraetorianus => 1,
                _ => 0
            };
        }
    }
}

class ArchUltimaStates : StateMachineBuilder
{
    public ArchUltimaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MagitekRay>()
            .ActivateOnEnter<AngrySalamander>()
            .ActivateOnEnter<TerminusEstCircle>()
            .ActivateOnEnter<TerminusEstRects>()
            .ActivateOnEnter<FireII>()
            .ActivateOnEnter<GarleanFire>()
            .ActivateOnEnter<MetalCutter>()
            .ActivateOnEnter<MagitekRayBits>()
            .ActivateOnEnter<AtomicRay>()
            .ActivateOnEnter<SelfDetonate>()
            .ActivateOnEnter<AutoEstinien>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69218u, NameID = 9189u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "croizat (ported from BMR)")]
public class ArchUltima(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(240f, 230f), new ArenaBoundsSquare(19.5f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}
