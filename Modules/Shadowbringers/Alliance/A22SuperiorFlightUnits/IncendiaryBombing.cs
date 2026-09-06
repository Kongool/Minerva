// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A22SuperiorFlightUnits;

sealed class IncendiaryBombing(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 8f, (uint)AID.IncendiaryBombing, GetVoidzones, 0.6d)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.FireVoidzoneSmall);
        var count = enemies.Count;
        if (count == 0)
        {
            return [];
        }

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
            {
                voidzones[index++] = z;
            }
        }
        return voidzones[..index];
    }
}

sealed class IncendiaryBarrage(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 27f, (uint)AID.IncendiaryBarrage, GetVoidzones, 0.9d)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.FireVoidzoneBig);
        var count = enemies.Count;
        if (count == 0)
        {
            return [];
        }

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
            {
                voidzones[index++] = z;
            }
        }
        return voidzones[..index];
    }
}

sealed class IncendiaryBombingBait : Components.GenericBaitAway
{
    private static readonly AOEShapeCircle circle = new(8f);
    private RelSimplifiedComplexPolygon polygon;
    private bool polygonInit;

    public IncendiaryBombingBait(ModuleBase module) : base(module, centerAtTarget: true)
    {
        IgnoreOtherBaits = true;
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.IncendiaryBombing)
        {
            CurrentBaits.Add(new(Module.PrimaryActor, actor, circle, World.FutureTime(6.1d)));
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.IncendiaryBombing)
        {
            CurrentBaits.Clear();
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (IsBaitTarget(actor))
        {
            if (!polygonInit)
            {
                polygon = A22SuperiorFlightUnits.ArenaShape.Polygon.Offset(-1.5f);
                polygonInit = true;
            }

            hints.AddForbiddenZone(new SDComplexPolygonInvertedContains(polygon, Center), CurrentBaits.Ref(0).Activation);
        }
    }
}
