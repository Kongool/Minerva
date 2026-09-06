// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C02AMR.C022Gorai;

sealed class Thundercall(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<Actor> _orbs = [];
    private Actor? _safeOrb;
    private Actor? _miniTarget;
    private readonly List<AOEInstance> _aoes = [];

    private readonly AOEShapeCircle _shapeSmall = new(8f);
    private readonly AOEShapeCircle _shapeLarge = new(18f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.Actors(_orbs, Colors.Object, true);
        if (_miniTarget != null)
            Arena.ZoneCircleOutline(_miniTarget.Position, 3f);
        if (_safeOrb != null)
            Arena.ZoneCircleOutline(_safeOrb.Position, 1f, Colors.Safe);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.NHumbleHammerAOE or (uint)AID.SHumbleHammerAOE)
        {
            _orbs.AddRange(Module.Enemies((uint)OID.NBallOfLevin));
            _orbs.AddRange(Module.Enemies((uint)OID.SBallOfLevin));
            WDir center = default;
            var count = _orbs.Count;
            for (var i = 0; i < count; ++i)
                center += _orbs[i].Position - Center;
            _safeOrb = _orbs.Farthest(Center + center);
            _miniTarget = World.Actors.Find(spell.TargetID);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.NHumbleHammerAOE:
            case (uint)AID.SHumbleHammerAOE:
                _miniTarget = null;
                _safeOrb = null;
                foreach (var o in _orbs)
                    _aoes.Add(new(spell.Targets.Any(t => t.ID == o.InstanceID) ? _shapeSmall : _shapeLarge, o.Position, default, World.FutureTime(4.2d)));
                break;
            case (uint)AID.NShockSmall:
            case (uint)AID.NShockLarge:
            case (uint)AID.SShockSmall:
            case (uint)AID.SShockLarge:
                _aoes.RemoveAll(aoe => aoe.Origin.AlmostEqual(caster.Position, 1f));
                ++NumCasts;
                break;
        }
    }
}

sealed class Flintlock(ModuleBase module) : Components.GenericWildCharge(module, 4f)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.NHumbleHammerAOE:
            case (uint)AID.SHumbleHammerAOE:
                Source = Module.PrimaryActor;
                foreach (var (slot, player) in Raid.WithSlot(true, true, true))
                    PlayerRoles[slot] = spell.MainTargetID == player.InstanceID ? PlayerRole.Target : player.Role == Role.Tank ? PlayerRole.Share : PlayerRole.ShareNotFirst; // TODO: or should it be 'avoid'?
                break;
            case (uint)AID.NFlintlockAOE:
            case (uint)AID.SFlintlockAOE:
                ++NumCasts;
                Source = null;
                break;
        }
    }
}
