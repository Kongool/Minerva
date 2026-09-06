// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P4S2Hesperos;

// state related to nearsight & farsight mechanics
class NearFarSight : ModuleComponent
{
    public enum State { Near, Far, Done }

    public State CurState { get; private set; }
    private BitMask _targets;
    private BitMask _inAOE;

    private const float _aoeRadius = 5;

    public NearFarSight(ModuleBase module) : base(module)
    {
        CurState = (Module.PrimaryActor.CastInfo?.Action.ID ?? 0) switch
        {
            (uint)AID.Nearsight => State.Near,
            (uint)AID.Farsight => State.Far,
            _ => State.Done
        };
        if (CurState == State.Done)
            ReportError($"Failed to initialize near/far sight, unexpected cast {Module.PrimaryActor.CastInfo?.Action}");
    }

    public override void Update()
    {
        _targets = _inAOE = default;
        if (CurState == State.Done)
            return;

        var playersByRange = Raid.WithSlot(false, true, true).SortedByRange(Module.PrimaryActor.Position);
        foreach ((var i, var player) in CurState == State.Near ? playersByRange.Take(2) : playersByRange.TakeLast(2))
        {
            _targets.Set(i);
            _inAOE |= Raid.WithSlot(false, true, true).InRadiusExcluding(player, _aoeRadius).Mask();
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_targets.None())
            return;

        var isTarget = _targets[slot];
        var shouldBeTarget = actor.Role == Role.Tank;
        var isFailing = isTarget != shouldBeTarget;
        var shouldBeNear = CurState == State.Near ? shouldBeTarget : !shouldBeTarget;
        hints.Add(shouldBeNear ? "Stay near boss" : "Stay on max melee", isFailing);
        if (_inAOE[slot])
        {
            hints.Add("GTFO from tanks!");
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (_targets.None())
            return;

        foreach ((var i, var player) in Raid.WithSlot(false, true, true))
        {
            if (_targets[i])
            {
                Arena.Actor(player, Colors.Danger);
                Arena.ZoneCircleOutline(player.Position, _aoeRadius, Colors.Danger);
            }
            else
            {
                Arena.Actor(player, _inAOE[i] ? Colors.PlayerInteresting : Colors.PlayerGeneric);
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.NearsightAOE or (uint)AID.FarsightAOE)
            CurState = State.Done;
    }
}
