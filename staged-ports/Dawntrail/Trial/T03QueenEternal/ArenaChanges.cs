// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T03QueenEternal;

sealed class ArenaChanges(ModuleBase module) : ModuleComponent(module)
{
    private readonly WPos SplitArenaCenter = new(100f, 94f);
    private BitMask gravity;

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x01 && state == 0x00040004u)
        {
            SetArena(new ArenaBoundsSquare(20f), new(100f, 100f));
        }
        else if (state == 0x00020001u)
        {
            switch (index)
            {
                case 0x00: // x arena
                    var arenaX = T03QueenEternal.GetXArena();
                    SetArena(arenaX, arenaX.Center);
                    break;
                case 0x02: // disjointed rect arena
                    var arenaSplit = T03QueenEternal.GetSplitArena();
                    SetArena(arenaSplit, arenaSplit.Center);
                    break;
            }
        }
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id == 0x2BFE) // final phase arena
        {
            SetArena(new ArenaBoundsRect(20f, 15f), new(100f, 105f));
        }
    }

    private void SetArena(ArenaBounds bounds, WPos center)
    {
        Bounds = bounds;
        Center = center;
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.GravitationalAnomaly)
        {
            gravity.Set(Raid.FindSlot(actor.InstanceID));
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.GravitationalAnomaly)
        {
            gravity.Clear(Raid.FindSlot(actor.InstanceID));
        }
    }

    public override void Update()
    {
        if (Center != SplitArenaCenter)
        {
            return;
        }
        var raid = Raid.WithSlot(false, true, true);
        var len = raid.Length;

        var countGravityAnomaly = 0;
        for (var i = 0; i < len; ++i)
        {
            if (gravity[raid[i].Item1])
            {
                ++countGravityAnomaly;
            }
        }
        var isRect = Bounds is ArenaBoundsRect;
        if (countGravityAnomaly != 0 && !isRect)
        {
            SetArena(new ArenaBoundsRect(12f, 8f), SplitArenaCenter);
        }
        else if (countGravityAnomaly == 0 && isRect)
        {
            SetArena(T03QueenEternal.GetSplitArena(), SplitArenaCenter);
        }
    }
}
