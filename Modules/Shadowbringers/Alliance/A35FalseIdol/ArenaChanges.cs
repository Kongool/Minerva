// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A35FalseIdol;

sealed class ArenaChanges(ModuleBase module) : Components.GenericAOEs(module)
{
    public readonly List<AOEInstance> AOEs = [];
    private static readonly AOEShapeCircle circleDistortion = new(6f), circleShockWave = new(7f);
    private static readonly AOEShapeRect rect = new(3f, 25f);
    public readonly Polygon[] Towers = new Polygon[2];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.PlaceOfPower => circleDistortion,
            (uint)AID.ShockwaveAOE => circleShockWave,
            _ => null
        };
        if (shape != null)
        {
            AOEs.Add(new(shape, spell.LocXZ, default, Module.CastFinishAt(spell)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        var id = spell.Action.ID;
        if (spell.Action.ID == (uint)AID.ShockwaveAOE)
        {
            if (Towers[0] == default)
            {
                Towers[0] = new Polygon(caster.Position, 7.5f, 20);
                Bounds = new ArenaBoundsCustom(A35FalseIdol.BaseSquare, [Towers[0]]);
            }
            else
            {
                Towers[1] = new Polygon(caster.Position, 7.5f, 20);
                Bounds = new ArenaBoundsCustom(A35FalseIdol.BaseSquare, Towers);
                Array.Clear(Towers);
            }
            if (AOEs.Count != 0)
            {
                AOEs.RemoveAt(0);
            }
        }
        else if (id == (uint)AID.Towerfall)
        {
            Bounds = A35FalseIdol.ArenaP2;
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x01)
        {
            switch (state)
            {
                case 0x02000100u:
                    AOEs.Clear();
                    if (Bounds == A35FalseIdol.ArenaP2)
                    {
                        Bounds = A35FalseIdol.DistortionArena;
                    }
                    else
                    {
                        Bounds = A35FalseIdol.RedGirlsDistortionArena;
                    }
                    break;
                case 0x00800001u:
                    if (Bounds == A35FalseIdol.DistortionArena)
                    {
                        Bounds = A35FalseIdol.ArenaP2;
                    }
                    else
                    {
                        Bounds = A35FalseIdol.RedGirlsArena;
                    }
                    break;
                case 0x08000001u:
                    Bounds = A35FalseIdol.ArenaP2;
                    Center = A35FalseIdol.ArenaCenter;
                    break;
                case 0x80004000u:
                    AOEs.Add(new(rect, new(-700f, -725f), default, World.FutureTime(10d)));
                    break;
                case 0x20001000u:
                    AOEs.Clear();
                    Bounds = A35FalseIdol.RedGirlsArena;
                    Center = A35FalseIdol.RedGirlsArena.Center;
                    break;
            }
        }
    }
}
