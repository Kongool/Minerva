// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.VariantCriterion.V1MerchantsTale.V10Rukhkh;

sealed class SphereOfSand(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SphereOfSand);
sealed class SandPlume(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Sandplume1, (uint)AID.Sandplume2], new AOEShapeCone(20f, 22.5f.Degrees()), 4);
sealed class SandBurst(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == (uint)OID.SandPearl && id == 0x11D1)
        {
            _aoes.Add(new(new AOEShapeCircle(5f), actor.Position, actorID: actor.InstanceID));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SandPearlGrow)
        {
            var aoes = CollectionsMarshal.AsSpan(_aoes);
            var len = aoes.Length;

            if (len == 0)
                return;

            for (var i = 0; i < len; i++)
            {
                ref var aoe = ref aoes[i];
                if (aoe.ActorID == spell.MainTargetID)
                {
                    aoe.Shape = new AOEShapeCircle(17f);
                }
            }
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.SphereShatterSmall or (uint)AID.SphereShatterLarge)
        {
            _aoes.Clear();
        }
    }
}
sealed class SonicHowl(ModuleBase module) : Components.GenericAOEs(module)
{
    // anywhere from 19.5 to 21.5 degrees between jump points
    // 9 jump points (including initial) + 1 jump point where boss appears
    // between initial to final appearing position 176.75 degrees on 1st run; safe to assumine it lands opposite of where it starts?

    private WPos _initial = default;
    private WPos _estimated = default;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _estimated == default ? [] : new([new(new AOEShapeCircle(24f), _estimated)]);

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_initial == default)
        {
            if (spell.Action.ID == (uint)AID.BanishingFootstep)
            {
                _initial = caster.Position;
                var dir = _initial - Center;
                var opp = dir.Rotate(180f.Degrees());
                _estimated = Center + opp;
            }
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SonicHowl)
        {
            _estimated = caster.Position;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SonicHowl)
        {
            _initial = default;
            _estimated = default;
        }
    }
}
sealed class DryTyphoon(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.DryTyphoonFirst, (uint)AID.DryTyphoonRest], new AOEShapeCone(20f, 15f.Degrees()), 9)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);

        var len = ActiveCasters.Length;
        if (len > 4)
        {
            hints.AddForbiddenZone(new AOEShapeDonut(5f, 20f), Center);
        }
    }
}
sealed class WindborneSeeds(ModuleBase module) : Components.GenericAOEs(module)
{
    // Dry Typhoon x24 into Seed Sprout
    private readonly List<AOEInstance> _aoes = [];
    private bool _active = false;
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _active ? CollectionsMarshal.AsSpan(_aoes) : [];

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.Growball)
        {
            _aoes.Add(new(new AOEShapeCircle(9f), actor.Position));
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Seedsprout)
        {
            _active = true;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SyrupSpout)
        {
            _aoes.Clear();
            _active = false;
        }
    }
}
sealed class StreamingSands(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.StreamingSands);
sealed class BitingScratch(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BitingScratch, new AOEShapeCone(40f, 45f.Degrees()));
sealed class BigBurst(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);
    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == (uint)OID.LargeExplosive && id == 0x1E46)
        {
            _aoes.Add(new(new AOEShapeCircle(13f), actor.Position));
        }
    }
    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.BigBurst)
        {
            _aoes.Clear();
        }
    }
}
sealed class FallingRock(ModuleBase module) : Components.GenericAOEs(module)
{
    // map effects 0-5
    // 00020001 indicator, 00080004 imminent
    // eventcast happens 4 times per row (12 total), repeats in middle for some reason
    private int _counter = 0;
    private readonly float _initialZ = -545f;
    private readonly List<AOEInstance> _aoes = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);
    public override void OnMapEffect(byte index, uint state)
    {
        if (index >= 0 && index <= 5 && state == 0x00020001)
        {
            _aoes.Add(new(new AOEShapeRect(60f, 3f), new(25f, _initialZ + index * 6f), Angle.AnglesCardinals[0]));
        }
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.FallingRock)
        {
            _counter++;
            if (_counter >= 12)
            {
                _aoes.Clear();
            }
        }
    }
}

[ModuleInfo(CFCID = 1066u, NameID = 14377u, PrimaryActorOID = (uint)OID.Rukhkh, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "gynorhino (ported from BMR)")]
public class V10Rukhkh(WorldState ws, Actor primary) : ModuleBase(ws, primary, primary.Position.X < -500f ? ArenaCenter1 : ArenaCenter2, new ArenaBoundsCircle(17.5f))
{
    public static readonly WPos ArenaCenter1 = new(-635, -415);
    public static readonly WPos ArenaCenter2 = new(5f, -530f);
}
