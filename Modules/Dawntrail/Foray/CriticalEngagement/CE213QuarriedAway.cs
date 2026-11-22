// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.CriticalEngagement.CE213QuarriedAway;

public enum OID : uint
{
    AlabasterBlade = 0x4BBE,
    Helper = 0x233C,
    AlabasterGolemVisual = 0x4BBF, // R1.650, x4
    AlabasterGolemCaster = 0x4EBD, // R1.000, x4
    LightAether = 0x4BC0, // R1.600, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 50760, // AlabasterBlade->player, no cast, single-target
    EmbrittlingBlade = 47171, // AlabasterBlade->self, 5.0s cast, single-target
    EmbrittlingBladeVisual = 47172, // Helper->self, no cast, ???
    Summon = 47154, // AlabasterBlade->self, 3.0s cast, single-target
    FourfoldAttackOrder = 47155, // AlabasterBlade->self, 10.0s cast, single-target
    AttackOrder = 47156, // AlabasterBlade->self, no cast, single-target

    AcclaimLong = 47157, // 4BBF->self, 12.0s cast, range 40 90.000-degree cone
    AcclaimShort = 47158, // 4BBF->self, 3.0s cast, range 40 90.000-degree cone

    OccultAeroIII = 47170, // Helper->self, 5.0s cast, range 50 width 10 rect
    RightLeftCombination = 47166, // AlabasterBlade->self, 5.0s cast, range 40 180.000-degree cone
    RightLeftCombinationClearout = 47169, // AlabasterBlade->self, no cast, range 40 180.000-degree cone
    LeftRightCombination = 47167, // AlabasterBlade->self, 5.0s cast, range 40 180.000-degree cone
    LeftRightCombinationClearout = 47168, // AlabasterBlade->self, no cast, range 40 180.000-degree cone

    LightPrayer = 47159, // AlabasterBlade->self, 3.0s cast, single-target
    OccultAero = 47163, // Helper->self, 5.0s cast, range 50 width 10 rect
    OccultTornado = 47165, // Helper->location, 5.0s cast, range 5 circle
    OccultStoneII = 47164, // Helper->self, 5.0s cast, range 40 60.000-degree cone

    FalseSpellbladeHoly = 47757, // AlabasterBlade->self, 32.0s cast, single-target
    FalseSpellbladeHolyVisual = 47161, // Helper->self, no cast, ???
}

public enum SID : uint
{
    BlueArrow = 2056, // none->4EBD, extra=0x43B/0x43C/0x43D - 0x43B = 3 turns, 0x43C = 2 turns, 0x43D = 1 turn
}

sealed class EmbrittlingBlade(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.EmbrittlingBlade);
sealed class OccultTornado(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OccultTornado, new AOEShapeCircle(5.0f));
sealed class FalseSpellbladeHoly(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FalseSpellbladeHoly);
sealed class OccultAeroIII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OccultAeroIII, new AOEShapeRect(50.0f, 5.0f));
sealed class OccultAero(ModuleBase module) : Components.SimpleAOEGroupsByTimewindow(module, [(uint)AID.OccultAero], new AOEShapeRect(50.0f, 5.0f));

sealed class RightLeftCombination(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeCone cone = new(40.0f, 90.0f.Degrees());

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.RightLeftCombination or (uint)AID.LeftRightCombination)
        {
            AddAOE();
            AddAOE(180.0f.Degrees(), 2.2f);
        }

        void AddAOE(Angle offset = default, double delay = default)
        {
            var loc = spell.LocXZ;
            var rot = spell.Rotation;
            var pos = delay != default ? loc - 5f * rot.ToDirection() : loc;
            var rot2 = rot + offset;
            aoes.Add(new(cone, pos, rot2, Module.CastFinishAt(spell, delay), shapeDistance: cone.Distance(pos, rot2)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (aoes.Count is var count && count != 0 && spell.Action.ID is (uint)AID.RightLeftCombination or (uint)AID.RightLeftCombinationClearout or
                (uint)AID.LeftRightCombination or (uint)AID.LeftRightCombinationClearout)
        {
            aoes.RemoveAt(0);
            if (count == 2)
            {
                ref var aoe2 = ref aoes.Ref(0);
                var rot = aoe2.Rotation;
                aoe2.Origin -= 5f * rot.ToDirection();
                aoe2.ShapeDistance = cone.Distance(aoe2.Origin, rot);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(aoes);
}

sealed class OccultStoneII : Components.SimpleAOEs
{
    public OccultStoneII(ModuleBase module) : base(module, (uint)AID.OccultStoneII, new AOEShapeCone(40.0f, 30.0f.Degrees()))
    {
        MaxDangerColor = 3;
    }
}

// Fourfold Attack Order: each golem's 90-degree cone fires after the long cast, then three more times 7.3s apart. Before each
// of those it turns 90 degrees clockwise, for as many waves as the blue arrow on its caster says, then holds. From
// BossmodReborn, which kept everything in one list, keyed on counts and list order. Saar, Occult Crescent 2026-11-19 21:19:
// the first Attack Order's arrows were applied three times (a refreshed status reads as a new one). Each copy predicted
// the waves again, also matching its own predictions, and each cast removed one cone, so the list never emptied. The second
// and third Attack Orders drew ten cones left from the first, at the golems' old spots and 80-160s stale, and none of their
// own. Saar stood still in two real cones each time and died twice.
// Now each golem is predicted once per set, a set starts clean at its first long cast, the real Acclaim cast replaces its
// wave's prediction (which runs 1.1s early on the first wave), and a cast removes its own golem's next cone.
sealed class Acclaim(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeCone shape = new(40.0f, 45.0f.Degrees());
    private readonly List<(WPos Origin, Angle Rotation, DateTime Activation)> longs = [];
    private readonly List<(Actor Caster, int Turns)> golemCasters = [];
    private readonly HashSet<ulong> armed = [];
    private const double RiskyWithSecondsLeft = 5d;
    private const double WaveSeconds = 7.3d;
    private const double SameWaveSeconds = 2.5d; // casts this close in time are one wave; the waves are 7.3s apart
    private const double StaleSeconds = 3d;      // a cone this far past its time without its cast is dropped

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.AcclaimLong when caster.OID == (uint)OID.AlabasterGolemVisual:
            {
                // a long cast from a set long gone starts a new one. Not the Attack Order's cast: it can start in the same frame
                // as the long casts and after them (11-01 14:30), and resetting there wiped the cones just added.
                if (this.longs.Count > 0 && this.longs[0].Activation < World.CurrentTime.AddSeconds(-StaleSeconds))
                    this.Reset();
                var finish = Module.CastFinishAt(spell);
                this.longs.Add((spell.LocXZ, spell.Rotation, finish));
                this.aoes.Add(new(this.shape, spell.LocXZ, spell.Rotation, finish));
                SortHelpers.SortAOEByActivation(this.aoes);
                break;
            }
            case (uint)AID.AcclaimShort:
                this.Place(spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell));
                break;
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID != (uint)SID.BlueArrow || !this.armed.Add(actor.InstanceID))
            return; // a refreshed arrow is the same arrow
        var turns = status.Extra switch
        {
            0x43B => 3,
            0x43C => 2,
            0x43D => 1,
            _ => 0,
        };
        if (turns != 0)
            this.golemCasters.Add((actor, turns));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is not ((uint)AID.AcclaimLong or (uint)AID.AcclaimShort))
            return;
        // the cone that landed is this golem's earliest (the list is in activation order)
        for (var i = 0; i < this.aoes.Count; ++i)
        {
            if (this.aoes[i].Origin.AlmostEqual(caster.Position, 0.5f))
            {
                this.aoes.RemoveAt(i);
                break;
            }
        }
    }

    public override void Update()
    {
        var added = false;
        for (var g = this.golemCasters.Count - 1; g >= 0; --g)
        {
            var (caster, turns) = this.golemCasters[g];
            foreach (var (origin, rotation0, activation) in this.longs)
            {
                if (!origin.AlmostEqual(caster.Position, 0.5f))
                    continue;
                var rotation = rotation0;
                for (var i = 0; i < 3; ++i)
                {
                    if (i < turns)
                        rotation -= 90.0f.Degrees();
                    this.aoes.Add(new(this.shape, origin, rotation.Normalized(), activation.AddSeconds(WaveSeconds * (i + 1))));
                }
                this.golemCasters.RemoveAt(g);
                added = true;
                break;
            }
        }
        if (added)
            SortHelpers.SortAOEByActivation(this.aoes);

        var stale = World.CurrentTime.AddSeconds(-StaleSeconds);
        this.aoes.RemoveAll(a => a.Activation < stale);
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = this.aoes.Count;
        if (count == 0)
            return [];

        // the next wave: every cone landing within a couple of seconds of the first
        var time = World.CurrentTime;
        var last = this.aoes[0].Activation.AddSeconds(SameWaveSeconds);
        var span = CollectionsMarshal.AsSpan(this.aoes);
        var max = 0;
        while (max < count && span[max].Activation <= last)
        {
            span[max].Risky = span[max].Activation.AddSeconds(-RiskyWithSecondsLeft) <= time;
            ++max;
        }
        return span[..max];
    }

    // The real cast for a wave: the golem's predicted cone for that wave takes its facing and time. One not predicted (an
    // arrow never seen) is added as cast.
    private void Place(WPos origin, Angle rotation, DateTime finish)
    {
        var best = -1;
        var bestGap = SameWaveSeconds;
        for (var i = 0; i < this.aoes.Count; ++i)
        {
            if (!this.aoes[i].Origin.AlmostEqual(origin, 0.5f))
                continue;
            var gap = Math.Abs((this.aoes[i].Activation - finish).TotalSeconds);
            if (gap < bestGap)
            {
                bestGap = gap;
                best = i;
            }
        }
        if (best >= 0)
        {
            ref var aoe = ref this.aoes.Ref(best);
            aoe.Rotation = rotation;
            aoe.Activation = finish;
        }
        else
        {
            this.aoes.Add(new(this.shape, origin, rotation, finish));
        }
        SortHelpers.SortAOEByActivation(this.aoes);
    }

    private void Reset()
    {
        this.aoes.Clear();
        this.longs.Clear();
        this.golemCasters.Clear();
        this.armed.Clear();
    }
}

[SkipLocalsInit]
sealed class CE213QuarriedAwayStates : StateMachineBuilder
{
    public CE213QuarriedAwayStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<EmbrittlingBlade>()
            .ActivateOnEnter<Acclaim>()
            .ActivateOnEnter<OccultAeroIII>()
            .ActivateOnEnter<RightLeftCombination>()
            .ActivateOnEnter<OccultAero>()
            .ActivateOnEnter<OccultTornado>()
            .ActivateOnEnter<OccultStoneII>()
            .ActivateOnEnter<FalseSpellbladeHoly>();
    }
}

[ModuleInfo(Group = ModuleGroup.CriticalEngagement, CFCID = 1093u, NameID = 51u, PrimaryActorOID = (uint)OID.AlabasterBlade, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
public sealed class CE213QuarriedAway : ModuleBase
{
    public CE213QuarriedAway(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private CE213QuarriedAway(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    private static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(new(-519f, -641f), 24.5f, 32)]);
        return (arena.Center, arena);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.LightAether));
    }

    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Module.Center, 25f);
}
