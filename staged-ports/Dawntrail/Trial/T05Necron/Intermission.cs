// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T05Necron;

sealed class Prisons(ModuleBase module) : ModuleComponent(module)
{
    private BitMask activeTeleporters;

    private static readonly WPos[] prisonPositions = [new(100f, -100f), new(300f, -100f), new(300f, 100f), new(300f, 300f),
    new(100f, 300f), new(-100f, 300f), new(-100f, 100f), new(-100f, -100f)];

    public override void OnMapEffect(byte index, uint state)
    {
        if (index <= 0x07)
        {
            switch (state)
            {
                case 0x00020001u:
                    activeTeleporters.Set(index);
                    break;
                case 0x00080004u:
                    activeTeleporters.Clear(index);
                    break;
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (pc.PosRot.Y < -100f)
        {
            var playerPos = pc.Position;
            for (var i = 0; i < 8; ++i)
            {
                var pos = prisonPositions[i];
                if (playerPos.InSquare(pos, 50f))
                {
                    if (activeTeleporters[i])
                    {
                        var color = Colors.SafeFromAOE;
                        Arena.ZoneCircle(pos + new WDir(default, -7.4f), 2f, color);
                        Arena.ZoneCircle(pos + new WDir(-2.5f, -20f), 2f, color);
                        Arena.ZoneCircle(pos + new WDir(15f, -11.5f), 2f, color);
                        Arena.ZoneCircle(pos + new WDir(20f, default), 1.5f, color);
                    }
                    return;
                }
            }
        }
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (pc.PosRot.Y < -100f)
        {
            var playerPos = pc.Position;
            if (Bounds.Radius == 17.5f && playerPos.InSquare(Center, 50f)) // grand cross could happen while player is in prison
            {
                return;
            }
            for (var i = 0; i < 8; ++i)
            {
                var pos = prisonPositions[i];
                if (playerPos.InSquare(pos, 50f))
                {
                    var arena = new ArenaBoundsCustom([new Polygon(pos, 9.5f, 32), new Polygon(pos + new WDir(-5f, -21f), 4.5f, 32),
                        new Polygon(pos + new WDir(14f, -14f), 4.5f, 32), new Polygon(pos + new WDir(20f, default), 3.25f, 32)]);
                    Bounds = arena;
                    Center = arena.Center;
                    return;
                }
            }
        }
        else if (Bounds.Radius == 17.5f)
        {
            Bounds = new ArenaBoundsRect(18f, 15f);
            Center = Necron.ArenaCenter;
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (actor.PosRot.Y < -100f)
        {
            var playerPos = actor.Position;
            for (var i = 0; i < 8; ++i)
            {
                var pos = prisonPositions[i];
                if (playerPos.InSquare(pos, 50f))
                {
                    if (activeTeleporters[i])
                    {
                        hints.Teleporters.Add(new(pos + new WDir(default, -7.4f), pos + new WDir(-6f, -18f), 2f, false));
                        hints.Teleporters.Add(new(pos + new WDir(-2.5f, -20f), pos + new WDir(10f, -15f), 2f, false));
                        hints.Teleporters.Add(new(pos + new WDir(15f, -11.5f), pos + new WDir(19f, -2f), 2f, false));
                        hints.GoalZones.Add(AIHints.GoalSingleTarget(pos + new WDir(20f, default), 1f, 9f));
                    }
                    return;
                }
            }
        }
    }
}
