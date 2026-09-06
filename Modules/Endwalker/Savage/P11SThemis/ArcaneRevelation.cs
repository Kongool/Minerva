// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P11SThemis;

// mirrors & spheres
class ArcaneRevelation(ModuleBase module) : Components.GenericAOEs(module)
{
    private uint _activeMirrors;
    private uint _activeSpheres;
    private readonly List<AOEInstance> _aoes = [];

    private static readonly AOEShapeRect _shapeMirror = new(50f, 5f);
    private static readonly AOEShapeCircle _shapeSphere = new(15f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var (mirrors, spheres) = spell.Action.ID switch
        {
            (uint)AID.ArcaneRevelationMirrorsLight => ((uint)OID.MirrorLight, 0u),
            (uint)AID.ArcaneRevelationMirrorsDark => ((uint)OID.MirrorDark, 0u),
            (uint)AID.ArcaneRevelationSpheresLight => (0u, (uint)OID.SphereLight),
            (uint)AID.ArcaneRevelationSpheresDark => (0u, (uint)OID.SphereDark),
            (uint)AID.TwofoldRevelationLight => ((uint)OID.MirrorLight, (uint)OID.SphereLight),
            (uint)AID.TwofoldRevelationDark => ((uint)OID.MirrorDark, (uint)OID.SphereDark),
            _ => (0u, 0u)
        };
        if (mirrors != 0 || spheres != 0)
        {
            _activeMirrors = mirrors;
            _activeSpheres = spheres;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.ArcheLight or (uint)AID.ArcheDark or (uint)AID.UnluckyLotLight or (uint)AID.UnluckyLotDark)
        {
            ++NumCasts;
            _aoes.Clear();
        }
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id != 0x1E46)
            return;
        if (actor.OID == _activeMirrors)
            AddAOE(_shapeMirror);
        else if (actor.OID == _activeSpheres)
            AddAOE(_shapeSphere);
        void AddAOE(AOEShape shape) => _aoes.Add(new(shape, actor.Position.Quantized(), actor.Rotation, World.FutureTime(8.7d)));
    }
}

class DismissalOverruling(ModuleBase module) : Components.GenericKnockback(module)
{
    private Actor? _source;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_source != null)
            return new Knockback[1] { new(_source.Position, 11f, Module.CastFinishAt(_source.CastInfo)) };
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.DismissalOverrulingLightAOE or (uint)AID.DismissalOverrulingDarkAOE)
            _source = caster;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.DismissalOverrulingLightAOE or (uint)AID.DismissalOverrulingDarkAOE)
        {
            _source = null;
            ++NumCasts;
        }
    }
}

class InnerLight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InnerLight, 13f);
class OuterDark(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OuterDark, new AOEShapeDonut(8f, 50f));
