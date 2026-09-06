// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
sealed class P3Inception3Sacrament(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.SacramentInception)
{
    private AOEInstance[] _aoe = [];
    private readonly Actor? boss = ((TEA)module).AlexPrime();
    private readonly Actor? heart = ((TEA)module).TrueHeart();
    private readonly AOEShapeCross cross = new(100f, 8f);
    private WPos initialPos;
    private DateTime activation;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_aoe.Length == 0 && initialPos != default && initialPos != boss!.Position) // only relevant if one or more crystals didn't get destroyed
        {
            return new AOEInstance[1] { new(cross, boss.Position, boss.Rotation, activation) };
        }
        return _aoe;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Inception) // only relevant if one or more crystals didn't get destroyed
        {
            initialPos = caster.Position;
            activation = Module.CastFinishAt(spell, 8.3d);
        }
    }

    public override void Update()
    {
        if (heart != null && _aoe.Length == 0)
        {
            var last = heart.LastFrameMovement;
            if (last != default)
            {
                ref var f = ref World.Frame;
                if (last.Length() / f.Duration > 7f && heart.FindStatus((uint)SID.EnigmaCodexHeart) != null)
                {
                    _ = Intersect.RayCircleAnglesDeg(Center, 20f, heart.Position, last, out _, out var exit);
                    var a = new Angle(MathF.Round(exit / 22.5f) * 22.5f * Angle.DegToRad); // alexander will appear at one of the 8 arena slices
                    var pos = (Center + 21.5f * a.ToDirection()).Quantized();
                    var angle = a + 180f.Degrees();
                    _aoe = [new(cross, pos, angle, World.FutureTime(6.9d), shapeDistance: cross.Distance(pos, angle))];
                }
            }
        }
    }
}

[SkipLocalsInit]
sealed class P3Inception3Debuffs(ModuleBase module) : Components.GenericStackSpread(module)
{
    private Actor? _sharedSentence;
    private BitMask _avoid;
    private BitMask _tethered;
    private bool _inited; // we init stuff on first update, since component is activated when statuses are already applied

    public override void Update()
    {
        if (!_inited)
        {
            _inited = true;
            if (_sharedSentence != null)
                Stacks.Add(new(_sharedSentence, 4f, 3, int.MaxValue, World.FutureTime(4d), _avoid));
        }
        base.Update();
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        base.AddHints(slot, actor, hints);
        if (_tethered[slot] && FindPartner(slot) is var partner && partner != null && (partner.Position - actor.Position).LengthSq() < 900f)
            hints.Add("Stay farther from partner!");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);

        if (_tethered[pcSlot] && FindPartner(pcSlot) is var partner && partner != null)
            Arena.AddLine(pc.Position, partner.Position, (partner.Position - pc.Position).LengthSq() < 900f ? default : Colors.Safe);
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        switch (status.ID)
        {
            case (uint)SID.AggravatedAssault:
                _avoid.Set(Raid.FindSlot(actor.InstanceID));
                break;
            case (uint)SID.SharedSentence:
                _sharedSentence = actor;
                break;
            case (uint)SID.RestrainingOrder:
                _tethered.Set(Raid.FindSlot(actor.InstanceID));
                break;
        }
    }

    private Actor? FindPartner(int slot)
    {
        var remaining = _tethered;
        remaining[slot] = false;
        return remaining.Any() ? Raid[remaining.LowestSetBit()] : null;
    }
}

[SkipLocalsInit]
sealed class P3TrueHeart(ModuleBase module) : Components.Adds(module, (uint)OID.TrueHeart, AIHints.Enemy.PriorityPointless);

[SkipLocalsInit]
sealed class P3Inception3EarlyHints(ModuleBase module) : ModuleComponent(module)
{
    private WPos[]? _safespots;
    private readonly TEA bossmod = (TEA)module;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (_safespots != null)
            Arena.ZoneCircleOutline(_safespots[pcSlot], 1f, Colors.Safe);
    }

    public override void AddMovementHints(int slot, Actor actor, MovementHints movementHints)
    {
        if (_safespots != null)
            movementHints.Add((actor.Position, _safespots[slot], Colors.Safe));
    }

    public override void Update()
    {
        if (_safespots != null)
            return;

        var h = bossmod.TrueHeart();
        if (h != null && h.LastFrameMovement != default)
        {
            var dir = h.LastFrameMovement.Normalized();
            var edgeDist = Intersect.RayCircle(h.Position, dir, Center, 21.5f);
            if (edgeDist is > 0 and < float.MaxValue)
            {
                Init(h.Position + dir * edgeDist);
            }
        }
    }

    private void Init(WPos heartPosition)
    {
        _safespots = new WPos[8];
        var center = Center;
        var relNorth = (heartPosition - center).ToAngle();

        foreach (var (slot, actor) in Raid.WithSlot(true, true, true))
        {
            var safeSide = actor.Role is Role.Tank || actor.FindStatus((uint)SID.SharedSentence) != null ? -90f.Degrees() : 90f.Degrees();
            _safespots[slot] = center + (relNorth + safeSide).ToDirection() * 18f;
        }
    }
}
