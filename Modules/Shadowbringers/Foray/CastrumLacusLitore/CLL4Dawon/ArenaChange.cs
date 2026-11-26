// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;
using static Minerva.Shadowbringers.Foray.CastrumLacusLitore.CLL4Dawon.CLL4Dawon;


namespace Minerva.Shadowbringers.Foray.CastrumLacusLitore.CLL4Dawon;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donutDawon = new(30f, 35f);
    private static readonly AOEShapeDonut donutLyon = new(20f, 25f);
    private AOEInstance[] _aoe = [];
    private bool lyonDeathwall;
    private bool dawonDeathwall;
    public bool IsDawonArena = true;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.MoltingPlumage && !dawonDeathwall)
        {
            AddAOE(donutDawon, 0.2d);
        }
        else if (!IsDawonArena && spell.Action.ID == (uint)AID.RagingWindsVisual1 && !lyonDeathwall)
        {
            AddAOE(donutLyon, 1.2d);
        }
        void AddAOE(AOEShapeDonut shape, double delay) => _aoe = [new(shape, Center, default, Module.CastFinishAt(spell, delay))];
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.DeathwallDawon)
        {
            Bounds = DawonDefaultArena;
            Center = DawonCenter;
            _aoe = [];
            dawonDeathwall = true;
        }
        else if (actor.OID == (uint)OID.DeathwallLyon)
        {
            lyonDeathwall = true;
            _aoe = [];
            if (!IsDawonArena)
                Bounds = LyonDefaultArena;
        }
    }

    // Whether the player is in Dawon's arena or Lyon's. Decided on the game tick: the drawing callback this was ported
    // in runs only while the radar is on screen, and a toon sent to Lyon with the radar closed kept Dawon's floor.
    public override void Update()
    {
        base.Update();
        if (Raid.Player() is { } player)
        {
            Decide(player.Position);
        }
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        base.DrawArenaBackground(pcSlot, pc);
        Decide(pc.Position);
    }

    private void Decide(WPos position)
    {
        if (!IsDawonArena && DawonStartingArena.Contains(position - DawonStartingArena.Center))
        {
            IsDawonArena = true;
            Center = DawonCenter;
            Bounds = DawonDefaultArena;
        }
        else if (IsDawonArena && LyonStartingArena.Contains(position - LyonStartingArena.Center))
        {
            IsDawonArena = false;
            Center = LyonCenter;
            Bounds = lyonDeathwall ? LyonDefaultArena : LyonStartingArena;
        }
    }
}
