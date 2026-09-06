// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Modules.Dawntrail.Extreme.Ex8Enuo;

sealed class GazeOfTheVoidAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GazeOfTheVoid2, new AOEShapeCone(40f, 22.5f.Degrees()), 7); // This is the easy part!

sealed class GazeOfTheVoidSoaks(ModuleBase module) : ModuleComponent(module)
{
    //TODO: We could probably filter these by distance to the boss?
    public static List<Actor> GetSmallOrbs(ModuleBase module)
    {
        var orbs = module.Enemies((uint)OID.VoidSoakSmall);
        var count = orbs.Count;
        return count == 0 ? [] : orbs;
    }
    public static List<Actor> GetBigOrbs(ModuleBase module)
    {
        var orbs = module.Enemies((uint)OID.VoidSoakLarge);
        var count = orbs.Count;
        return count == 0 ? [] : orbs;
    }
    public override void AddGlobalHints(GlobalHints hints)
    {
        if (GetSmallOrbs(Module).Count != 0 || GetBigOrbs(Module).Count != 0)
            hints.Add("Soak the orbs in pairs!");
    }

    //TODO: These AI hints are pretty basic -- They'll point us in the correct direction but we're not waiting for pairs or prioritizing the closest to the boss.
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var orbs = GetSmallOrbs(Module);
        var count = orbs.Count;
        var bigorbs = GetBigOrbs(Module);
        var bigcount = bigorbs.Count;
        if (actor.Role != Role.Tank)
        {
            if (count != 0)
            {
                var orbz = new ShapeDistance[count];
                for (var i = 0; i < count; ++i)
                {
                    var o = orbs[i];
                    orbz[i] = new SDInvertedRect(o.Position + 0.5f * o.Rotation.ToDirection(), new WDir(default, 1f), 0.5f, 0.5f, 0.5f);
                }
                hints.AddForbiddenZone(new SDIntersection(orbz), DateTime.MaxValue);
            }
        }
        else
        {
            if (bigcount != 0)
            {
                var orbz = new ShapeDistance[bigcount];
                for (var i = 0; i < bigcount; ++i)
                {
                    var o = bigorbs[i];
                    orbz[i] = new SDInvertedRect(o.Position + 0.5f * o.Rotation.ToDirection(), new WDir(default, 1f), 0.5f, 0.5f, 0.5f);
                }
                hints.AddForbiddenZone(new SDIntersection(orbz), DateTime.MaxValue);
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var orbs = GetSmallOrbs(Module);
        var count = orbs.Count;
        var bigorbs = GetBigOrbs(Module);
        var bigcount = bigorbs.Count;
        var isTank = pc.Role != Role.Tank;
        var colorSmall = isTank ? Colors.Danger : Colors.Safe;
        var colorBig = isTank ? Colors.Safe : Colors.Danger;
        for (var i = 0; i < count; ++i)
        {
            Arena.ZoneCircleOutline(orbs[i].Position, 1f, colorSmall);
        }
        for (var i = 0; i < bigcount; ++i)
        {
            Arena.ZoneCircleOutline(bigorbs[i].Position, 1f, colorBig);
        }
    }
}
