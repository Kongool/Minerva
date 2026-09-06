// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex2Garuda;

class MistralSong : Components.GenericLineOfSightAOE
{
    private readonly WPos _predictedPosition;

    public MistralSong(ModuleBase module, WPos predictedPosition) : base(module, (uint)AID.MistralSong, 31.7f, true)
    {
        _predictedPosition = predictedPosition;
        Modify(_predictedPosition, ActiveBlockers());
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            Modify(caster.Position, ActiveBlockers());
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            Modify(null, ActiveBlockers());
    }

    private IEnumerable<(WPos, float)> ActiveBlockers() => Module.Enemies((uint)OID.Monolith).Where(a => !a.IsDead).Select(a => (a.Position, a.HitboxRadius - 0.5f));
}
class MistralSong1(ModuleBase module) : MistralSong(module, new(0, -13));
class MistralSong2(ModuleBase module) : MistralSong(module, new(13, 0));
