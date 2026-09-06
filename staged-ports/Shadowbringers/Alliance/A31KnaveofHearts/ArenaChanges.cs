// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A31KnaveofHearts;

sealed class BoxSpawn(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BoxSpawn, new AOEShapeRect(8f, 4f));

sealed class ArenaChanges(ModuleBase module) : ModuleComponent(module)
{
    public readonly List<Square> Squares = [];
    private DateTime lastUpdate;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.BoxSpawn)
        {
            Squares.Add(new Square(caster.Position, 4f));
        }
    }

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (actor.OID == (uint)OID.ArenaFeatures)
        {
            if (state == 0x00010002u)
            {
                Bounds = new ArenaBoundsCustom(A31KnaveofHearts.BaseSquare, [.. Squares], AdjustForHitboxInwards: true);
                lastUpdate = World.CurrentTime;
            }
            else if (state == 0x00040008u && World.CurrentTime > lastUpdate.AddSeconds(1d)) // clearing old squares can happen in the same frame as new squares got added
            {
                Bounds = A31KnaveofHearts.DefaultArena;
            }
        }
    }
}
