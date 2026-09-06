// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Normal.FTMN2SwordDancer;

sealed class SwordStormCast(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SwordStormCast);
sealed class RushShort1(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Rush1, 3.5f);
sealed class RushShort2(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Rush2, 3.5f);
sealed class RushSurgesword(ModuleBase module) : Components.GenericAOEs(module)
{
    // hide AOE until knockback done, less clutter
    private readonly Steelsbreath steelsbreath = module.FindComponent<Steelsbreath>()!;
    private readonly List<AOEInstance> _aoes = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var knockbacks = steelsbreath.ActiveKnockbacks(slot, actor);
        if (knockbacks.Length != 0)
        {
            return [];
        }
        return CollectionsMarshal.AsSpan(_aoes);
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == (uint)OID.DancingSwordSurgesword && id == 0x11D6)
        {

            _aoes.Add(new(new AOEShapeRect(60f, 3f), actor.Position, actor.Rotation, World.FutureTime(8.2d)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.RushSurgesword)
        {
            _aoes.Clear();
        }
    }
}
sealed class TurnInner(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.TurnInner1, (uint)AID.TurnInner2, (uint)AID.TurnaboutInner], new AOEShapeDonutSector(9f, 14f, 45f.Degrees()));
sealed class TurnOuter(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.TurnOuter1, (uint)AID.TurnOuter2, (uint)AID.TurnaboutOuter], new AOEShapeDonutSector(19f, 24f, 45f.Degrees()));
sealed class MartialMystique(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MartialMystique, new AOEShapeRect(48f, 48f));
sealed class Pierce(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pierce, 5f);
sealed class Cyclosword(ModuleBase module) : Components.GenericAOEs(module)
{
    // model state change doesn't trigger each time; if actor does the same AOE later, doesn't happen
    private readonly List<AOEInstance> _aoes = [];
    private readonly Dictionary<ulong, AOEShape> _cycloswords = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return CollectionsMarshal.AsSpan(_aoes);
    }

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        if (actor.OID == (uint)OID.DancingSwordCyclosword && animState1 == 1 && animState2 == 0)
        {
            AOEShape? shape = modelState switch
            {
                4 => new AOEShapeDonut(15f, 60f),
                5 => new AOEShapeDonut(20f, 60f),
                7 => new AOEShapeCircle(15f),
                31 => new AOEShapeCircle(20f),
                _ => null
            };

            if (shape == null)
            {
                return;
            }

            _cycloswords[actor.InstanceID] = shape;
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.Cyclosword)
        {
            if (_cycloswords.TryGetValue(actor.InstanceID, out var shape))
            {
                _aoes.Add(new(shape, actor.Position, default, World.CurrentTime.AddSeconds(8.2d)));
            }
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0)
        {
            switch (spell.Action.ID)
            {
                case (uint)AID.Spin:
                case (uint)AID.Spin1:
                case (uint)AID.Spin2:
                case (uint)AID.Spin3:
                    _aoes.RemoveAt(0);
                    break;
            }
        }
    }
}
sealed class SwordDance(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];

        var max = count > 2 ? 2 : count;
        var aoes = CollectionsMarshal.AsSpan(_aoes);

        ref var aoe = ref aoes[0];
        aoe.Color = Colors.Danger;

        return aoes[..max];
    }
    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (actor.OID == (uint)OID.SwordDanceMarker && state == 0x00010002)
        {
            // 8.8s between 1st mark and 1st cast
            // 1s between eanims, 2.4s-ish between actual cast
            var count = _aoes.Count;
            var act = World.FutureTime(8.8d + 2.4d * count);
            _aoes.Add(new(new AOEShapeRect(30f, 10f, 30f), actor.Position, actor.Rotation, act));
        }
    }
    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.SwordDance)
        {
            _aoes.RemoveAt(0);
        }
    }
}

sealed class Steelsbreath(ModuleBase module) : Components.GenericKnockback(module)
{
    // do we need to avoid getting knocked back into RushSurgesword?
    // only subset; showing all 4 is visually confusing
    private readonly List<Knockback> _knockbacks = [];
    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        var count = _knockbacks.Count;
        if (count == 0)
            return [];

        var kbs = CollectionsMarshal.AsSpan(_knockbacks);
        //var max = count > 2 ? 2 : count;
        return kbs[..1];
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.LeapingLift && status.Extra == 0x47B)
        {
            // 10.7s between 1st status and resolve, status 1.4s between each, resolve 2.5s between each
            var count = _knockbacks.Count;
            var act = World.FutureTime(10.7d + 1.3d * count);
            _knockbacks.Add(new(actor.Position, 24f, act));
        }
        base.OnStatusGain(actor, ref status);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_knockbacks.Count != 0 && spell.Action.ID == (uint)AID.Steelsbreath)
        {
            _knockbacks.RemoveAt(0);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var kbs = CollectionsMarshal.AsSpan(_knockbacks);
        var count = kbs.Length;
        if (count != 0)
        {
            ref var kb = ref kbs[0];
            var act = kb.Activation;
            if (!IsImmune(slot, act))
            {
                if (count == 1)
                {
                    hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOrigin(Center, kb.Origin, 25f, 24f), act);
                }
                else
                {
                    ref var kb1 = ref kbs[1];
                    hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOriginIntoCircle(Center, kb.Origin, 25f, 24f, kb1.Origin, 7f), act);
                }
            }
        }
    }

    private sealed class SDKnockbackInCircleAwayFromOriginInverted(WPos Center, WPos Origin, float Distance, float Radius) : ShapeDistance
    {
        private readonly WPos center = Center;
        private readonly WPos origin = Origin;
        private readonly float radius = Radius;
        private readonly float distance = Distance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Inside(WPos p)
        {
            var projected = p + distance * (p - origin).Normalized();
            return projected.InCircle(center, radius);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float Distance(WPos p) => Inside(p) ? 0f : 1f;

    }

    private sealed class SDKnockbackInCircleAwayFromOriginIntoCircleInverted(WPos Center, WPos Origin, float Distance, float Radius, WPos CircleOrigin, float CircleRadius) : ShapeDistance
    {
        private readonly WPos center = Center;
        private readonly WPos origin = Origin;
        private readonly float radius = Radius;
        private readonly float distance = Distance;
        private readonly WPos circleOrigin = CircleOrigin;
        private readonly float circleRadius = CircleRadius;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float Distance(WPos p) => Inside(p) ? 0f : 1f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Inside(WPos p)
        {
            var projected = p + distance * (p - origin).Normalized();
            if (projected.InCircle(center, radius))
            {
                return true;
            }
            return projected.InCircle(circleOrigin, circleRadius);

        }

    }
}

[ModuleInfo(Group = ModuleGroup.TheForkedTowerMagic, CFCID = 1093u, NameID = 14820u, PrimaryActorOID = (uint)OID.SwordDancer, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "gynorhino (ported from BMR)")]
[SkipLocalsInit]
public sealed class SwordDancer(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(600f, 704f), new ArenaBoundsCircle(24f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Center, 24f);
}
