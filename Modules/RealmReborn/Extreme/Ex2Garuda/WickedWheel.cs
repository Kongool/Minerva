// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex2Garuda;

class WickedWheel(ModuleBase module) : Components.CastCounter(module, (uint)AID.WickedWheel)
{
    private DateTime _expectedNext = module.World.FutureTime(25d);
    private const float _radius = 8.7f;

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (_expectedNext != default)
            hints.Add($"Wicked wheel in ~{Math.Max((_expectedNext - World.CurrentTime).TotalSeconds, 0)}s");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // note: suparna also casts this, but we generally ignore it...
        if (_expectedNext != default && Module.PrimaryActor.TargetID != actor.InstanceID && (_expectedNext - World.CurrentTime).TotalSeconds < 3d)
            hints.AddForbiddenZone(new SDCircle(Module.PrimaryActor.Position, _radius), _expectedNext);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (_expectedNext != default && (_expectedNext - World.CurrentTime).TotalSeconds < 3d)
            Arena.ZoneCircleOutline(Module.PrimaryActor.Position, _radius, Colors.Danger);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == WatchedAction)
        {
            // not sure about this ...
            _expectedNext = Module.Enemies((uint)OID.Suparna).Any(a => a.IsTargetable && !a.IsDead) ? World.FutureTime(25d) : default;
        }
    }
}
