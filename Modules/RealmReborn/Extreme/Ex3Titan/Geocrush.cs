// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex3Titan;

class Geocrush(ModuleBase module, float radius) : Components.CastCounter(module, (uint)AID.Geocrush)
{
    private readonly float _radius = radius;
    private const float _ringWidth = 2f;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!actor.Position.InCircle(Center, _radius))
            hints.Add("Move closer to center!");
        else if (actor.Position.InCircle(Center, _radius - _ringWidth))
            hints.Add("Move closer to the edge!");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        hints.AddForbiddenZone(new SDInvertedDonut(Center, _radius - _ringWidth, _radius));
        hints.AddPredictedDamage(Raid.WithSlot(false, true, true).Mask(), default);
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        Arena.ZoneDonut(Center, _radius, 25f, Colors.AOE);
        Arena.ZoneDonut(Center, _radius - _ringWidth, _radius, Colors.SafeFromAOE);
    }
}

class Geocrush1(ModuleBase module) : Geocrush(module, Radius)
{
    public const float Radius = 15f;
}

class Geocrush2(ModuleBase module) : Geocrush(module, Radius)
{
    public const float Radius = 12f;
}
