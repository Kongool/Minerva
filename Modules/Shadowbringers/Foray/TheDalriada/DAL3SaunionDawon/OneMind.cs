// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL3SaunionDawon;

sealed class OneMind(ModuleBase module) : ModuleComponent(module)
{
    private readonly DAL3SaunionDawon bossmod = (DAL3SaunionDawon)module;
    private bool oneMind;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.OneMind)
        {
            oneMind = true;
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.OneMind)
        {
            oneMind = false;
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (oneMind)
        {
            hints.Add($"Pull {Module.PrimaryActor.Name} & {bossmod.BossDawon?.Name} further apart!");
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (oneMind && bossmod.BossDawon is Actor dawon)
        {
            var primary = Module.PrimaryActor;
            var primaryID = primary.TargetID;
            var actorID = actor.InstanceID;
            var isPrimaryTarget = actorID == primaryID;
            if (isPrimaryTarget ^ actor.InstanceID == dawon.TargetID) // if actor got aggro from both bosses we can do nothing
            {
                var pos = isPrimaryTarget ? dawon.Position : primary.Position;
                hints.AddForbiddenZone(new SDCircle(pos, (pos - actor.Position).Length() + 2f), DateTime.MaxValue);
            }
        }
    }
}
