// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Dungeon.D06AlaMhigo.D063ZenosYaeGalvus;

public enum OID : uint
{
    Boss = 0x1BAA, // R0.96
    ZenosYaeGalvusClone1 = 0x1BAB, // R0.96
    ZenosYaeGalvusClone2 = 0xF88C5, // R0.5-0.8
    TheSwell = 0x1BAC, // R3.0
    AmeNoHabakiri = 0x1BAE, // R3.0
    TheStorm = 0x1BAD, // R3.0
    ArenaVoidzone = 0x1EA1A1,
    Helper1 = 0x18D6,
    Helper2 = 0x1DD3,
    Helper3 = 0x1DCB,
    Helper4 = 0x1DCC,
}

public enum AID : uint
{
    AutoAttack = 870, // Boss/Helper4/Helper2/Helper3->player/Helper2/Helper3/player/Helper4, no cast, single-target
    Teleport1 = 8292, // Boss->location, no cast, ???
    Teleport2 = 9119, // ZenosYaeGalvusClone1->location, no cast, ???
    Begin = 8287, // Boss->self, no cast, single-target

    ArtOfTheSwell1 = 8293, // Boss->self, 6.0s cast, range 33 circle, knockback 15, away from source
    ArtOfTheSwell2 = 9606, // ZenosYaeGalvusClone1->self, 6.0s cast, range 33 circle, knockback 15, away from source
    Hakaze = 9140, // Helper4->Helper2, no cast, single-target
    SpinningEdge = 9300, // Helper3->Helper2, no cast, single-target

    UnmovingTroikaFirst = 8288, // Boss->self, no cast, range 9+R 120-degree cone, starts 3.2s after kill NPC yell
    UnmovingTroikaSecond = 8289, // Helper1->self, 1.7s cast, range 9+R 120-degree cone
    UnmovingTroikaLast = 8290, // Helper1->self, 2.1s cast, range 9+R 120-degree cone

    ArtOfTheStormVisual = 8295, // Helper1->self, 1.0s cast, range 15 circle
    ArtOfTheStorm1 = 8294, // Boss->self, 6.0s cast, range 8 circle
    ArtOfTheStorm2 = 9607, // ZenosYaeGalvusClone1->self, 8.0s cast, range 8 circle

    ArtOfTheSwordVisual1 = 8296, // Boss->self, 5.5s cast, single-target, proteans
    ArtOfTheSwordVisual2 = 9608, // ZenosYaeGalvusClone1->self, 5.5s cast, single-target
    ArtOfTheSword1 = 8297, // Helper1->self, no cast, range 40+R width 6 rect
    ArtOfTheSword2 = 9609, // Helper1->self, no cast, range 40+R width 6 rect

    FastBlade = 717, // Helper2->player/Helper4/Helper3, no cast, single-target

    VeinSplitter1 = 9398, // Boss->self, 3.5s cast, range 10 circle
    VeinSplitter2 = 8300, // ZenosYaeGalvusClone1->self, 3.0s cast, range 10 circle

    LightlessSpark = 8299, // Boss->self, 3.0s cast, range 40+R 90-degree cone, bait away
    Concentrativity = 8301, // Boss->self, 3.0s cast, range 40 circle, raidwide

    LimitBreakStart = 8302, // Boss->self, no cast, single-target
    StormSwellSwordVisual = 8303, // Boss->self, no cast, single-target
    LimitBreakVisual = 9118, // TheSwell/TheStorm/AmeNoHabakiri->self, no cast, single-target
    StormSwellSwordFinish = 8304 // Helper1->self, 7.0s cast, range 40 circle
}

public enum NPCYell : ushort
{
    Begin = 5371,
    Kill = 5372
}

public enum TetherID : uint
{
    BaitAway = 41 // Boss->player
}

class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(20f, 35f);
    private AOEInstance[] _aoe = [];
    private bool begin;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (state == 0x00040008u && actor.OID == (uint)OID.ArenaVoidzone)
        {
            Bounds = D063ZenosYaeGalvus.DefaultBounds;
            _aoe = [];
            begin = true;
        }
    }

    public override void Update()
    {
        if (!begin && _aoe.Length == 0)
        {
            _aoe = [new(donut, Center, default, World.FutureTime(10d))];
        }
    }
}

class UnmovingTroikaFirst(ModuleBase module) : Components.Cleave(module, (uint)AID.UnmovingTroikaFirst, new AOEShapeCone(9.96f, 60f.Degrees()))
{
    private bool imminent;

    public override void OnActorNpcYell(Actor actor, ushort id)
    {
        if ((ushort)NPCYell.Kill == id)
            imminent = true;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (imminent)
            base.AddHints(slot, actor, hints);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (imminent)
            base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (imminent)
            base.DrawArenaForeground(pcSlot, pc);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.UnmovingTroikaFirst)
            imminent = false;
    }
}

abstract class UnmovingTroika(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(9.96f, 60f.Degrees()));
class UnmovingTroikaSecond(ModuleBase module) : UnmovingTroika(module, (uint)AID.UnmovingTroikaSecond);
class UnmovingTroikaLast(ModuleBase module) : UnmovingTroika(module, (uint)AID.UnmovingTroikaLast);

abstract class ArtOfTheStorm(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 8f);
class ArtOfTheStorm1(ModuleBase module) : ArtOfTheStorm(module, (uint)AID.ArtOfTheStorm1);
class ArtOfTheStorm2(ModuleBase module) : ArtOfTheStorm(module, (uint)AID.ArtOfTheStorm2);

abstract class VeinSplitter(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 10f);
class VeinSplitter1(ModuleBase module) : VeinSplitter(module, (uint)AID.VeinSplitter1);
class VeinSplitter2(ModuleBase module) : VeinSplitter(module, (uint)AID.VeinSplitter2);

abstract class ArtOfTheSwell(ModuleBase module, uint aid) : Components.SimpleKnockbacks(module, aid, 15f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
            hints.AddForbiddenZone(new SDInvertedCircle(Center, 5f), Casters.Ref(0).Activation);
    }
}
class ArtOfTheSwell1(ModuleBase module) : ArtOfTheSwell(module, (uint)AID.ArtOfTheSwell1);
class ArtOfTheSwell2(ModuleBase module) : ArtOfTheSwell(module, (uint)AID.ArtOfTheSwell2);

class ArtOfTheSword(ModuleBase module) : Components.GenericBaitAway(module)
{
    private static readonly AOEShapeRect rect = new(40.5f, 3f);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.ArtOfTheSwordVisual1 or (uint)AID.ArtOfTheSwordVisual2)
        {
            var party = Raid.WithoutSlot(false, true, true);
            var len = party.Length;
            for (var i = 0; i < len; ++i)
                CurrentBaits.Add(new(caster, party[i], rect, World.FutureTime(6.3d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.ArtOfTheSword1 or (uint)AID.ArtOfTheSword2)
            CurrentBaits.Clear();
    }
}

class LightlessSparkBaitaway(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeCone(40.96f, 45f.Degrees()), (uint)TetherID.BaitAway, (uint)AID.LightlessSpark, activationDelay: 8f)
{
    private readonly ArtOfTheSwell1 _kb = module.FindComponent<ArtOfTheSwell1>()!;
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_kb.Casters.Count != 0) // resolve knockback before baits
            return;
        base.AddAIHints(slot, actor, assignment, hints);
        var baits = ActiveBaitsOn(actor);
        if (baits.Count != 0)
        {
            ref var bait = ref baits.Ref(0).Source;
            hints.AddForbiddenZone(new SDInvertedCone(bait.Position, 10f, Angle.FromDirection(bait.Position - Center), 45f.Degrees()), World.FutureTime(ActivationDelay));
        }
    }
}

class LightlessSparkAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightlessSpark, new AOEShapeCone(40.96f, 45f.Degrees()));

class Concentrativity(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Concentrativity);

class D063ZenosYaeGalvusStates : StateMachineBuilder
{
    public D063ZenosYaeGalvusStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<ArtOfTheSwell1>()
            .ActivateOnEnter<ArtOfTheSwell2>()
            .ActivateOnEnter<UnmovingTroikaFirst>()
            .ActivateOnEnter<UnmovingTroikaSecond>()
            .ActivateOnEnter<UnmovingTroikaLast>()
            .ActivateOnEnter<ArtOfTheStorm1>()
            .ActivateOnEnter<ArtOfTheStorm2>()
            .ActivateOnEnter<ArtOfTheSword>()
            .ActivateOnEnter<Concentrativity>()
            .ActivateOnEnter<VeinSplitter1>()
            .ActivateOnEnter<VeinSplitter2>()
            .ActivateOnEnter<LightlessSparkAOE>()
            .ActivateOnEnter<LightlessSparkBaitaway>();
    }
}

[ModuleInfo(CFCID = 247u, NameID = 6039u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D063ZenosYaeGalvus(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(250f, -353f), new ArenaBoundsSquare(22.5f))
{
    public static readonly ArenaBoundsCircle DefaultBounds = new(20f);
    private static readonly uint[] swords = [(uint)OID.TheStorm, (uint)OID.TheSwell, (uint)OID.AmeNoHabakiri];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(this, swords);
    }
}
