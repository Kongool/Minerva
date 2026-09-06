// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T05Yojimbo;

class Enchain(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _targets = [];

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if ((tether.ID == 1 || tether.ID == 57) && source.Type == ActorType.Player)
            _targets.Add(source);
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if ((tether.ID == 1 || tether.ID == 57) && source.Type == ActorType.Player)
            _targets.Remove(source);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_targets.Count > 0)
            hints.Add(_targets.Contains(actor) ? "You're about to be bound" : "Prepare to free bound players");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_targets.Contains(actor))
            hints.AddForbiddenZone(new AOEShapeCircle(20f).Distance(Module.PrimaryActor.Position, default), DateTime.MaxValue);

        foreach (var ironChain in Module.Enemies((uint)OID.IronChain))
            hints.SetPriority(ironChain, 1);
    }
}
