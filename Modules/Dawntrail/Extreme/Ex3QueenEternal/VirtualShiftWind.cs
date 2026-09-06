// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex3QueenEternal;

sealed class Aeroquell(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Aeroquell, 5f, 4);
sealed class AeroquellTwister(ModuleBase module) : Components.Voidzone(module, 5f, GetTwister)
{
    private static List<Actor> GetTwister(ModuleBase module) => module.Enemies((uint)OID.Twister);
}
sealed class MissingLink(ModuleBase module) : Components.Chains(module, (uint)TetherID.MissingLink, default, 25f);

sealed class WindOfChange(ModuleBase module) : Components.GenericKnockback(module, (uint)AID.WindOfChange)
{
    private readonly Angle[] _directions = new Angle[PartyState.MaxPartySize];
    private DateTime _activation;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_directions[slot] != default)
        {
            return new Knockback[1] { new(actor.Position, 20f, _activation, null, _directions[slot], Kind.DirForward, ignoreImmunes: true) };
        }
        return [];
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        var dir = status.ID switch
        {
            (uint)SID.WestWindOfChange => 90f.Degrees(),
            (uint)SID.EastWindOfChange => -90f.Degrees(),
            _ => default
        };
        if (dir != default && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
        {
            _directions[slot] = dir;
            _activation = status.ExpireAt;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            ++NumCasts;
            if (Raid.FindSlot(spell.MainTargetID) is var slot && slot >= 0)
            {
                _directions[slot] = default;
            }
        }
    }
}
