// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P2SHippokampos;

sealed class DoubledImpact(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.DoubledImpact, 6f);
sealed class SewageEruption(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SewageEruptionAOE, 6f);
// state related to sewage deluge mechanic
sealed class SewageDeluge(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private readonly Shape[] shapes = [new Square(new(90.5f, 90.5f), 4.5f), new Square(new(109.5f, 90.5f), 4.5f), new Square(new(90.5f, 109.5f), 4.5f), new Square(new(109.5f, 109.5f), 4.5f),
    new Rectangle(new(100f, 90.5f), 5f, 2f), new Rectangle(new(100f, 109.5f), 5f, 2f), new Rectangle(new(90.5f, 100f), 2f, 5f), new Rectangle(new(109.5f, 100f), 2f, 5f)];
    private int seenEvents;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnMapEffect(byte index, uint state)
    {
        // 800375A2: we typically get two events for index=0 (global) and index=N (corner)
        // state 00200010 - "prepare" (show aoe that is still harmless)
        // state 00020001 - "active" (dot in center/borders, oneshot in corner)
        // state 00080004 - "finish" (reset)

        switch (state)
        {
            case 0x00200010u:
                if (index is > 0 and < 5)
                {
                    if (shapes[index - 1] is Square sq)
                    {
                        sq.HalfHeight = sq.HalfWidth = default;
                        ++seenEvents;
                    }
                }
                else if (index == 0)
                {
                    ++seenEvents;
                }
                if (seenEvents > 1)
                {
                    var center = Center;
                    var shape = new AOEShapeCustom(center, [new Rectangle(new(100f, 100f), 17.5f, 22.5f)], shapes);
                    _aoe = [new(shape, center, activation: World.FutureTime(7.9d), shapeDistance: shape.Distance(center, default))];
                }
                break;
            case 0x0020001u:
                _aoe = [];
                Bounds = new ArenaBoundsCustom(shapes);
                break;
            case 0x00080004u:
                Bounds = new ArenaBoundsRect(17.5f, 22.5f);
                if (index is > 0 and < 5)
                {
                    if (shapes[index - 1] is Square sq)
                    {
                        seenEvents = 0;
                        sq.HalfHeight = sq.HalfWidth = 4.5f;
                    }
                }
                break;
        }
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 811u, CFCID = 811u, NameID = 10348u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class P2S(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsRect(17.5f, 22.5f));
