// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex2Garuda;

class SpinyShield(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _shield = module.Enemies((uint)OID.SpinyShield);
    public Actor? ActiveShield => _shield.FirstOrDefault(a => a.EventState != 7);

    private const float _radius = 6; // TODO: verify

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var shield = ActiveShield;
        if (shield != null && !actor.Position.InCircle(shield.Position, _radius))
            hints.Add("Go inside shield");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var shield = ActiveShield;
        if (shield != null)
            hints.AddForbiddenZone(new SDInvertedCircle(shield.Position, _radius));
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        var shield = ActiveShield;
        if (shield != null)
            Arena.ZoneCircle(shield.Position, _radius, Colors.SafeFromAOE);
    }
}
