// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace BossMod;

public sealed class DemoModule : ModuleBase
{
    private sealed class DemoComponent(ModuleBase module) : ModuleComponent(module)
    {
        public override void AddHints(int slot, Actor actor, TextHints hints)
        {
            hints.Add("Hint", false);
            hints.Add("Risk");
        }

        public override void AddMovementHints(int slot, Actor actor, MovementHints movementHints)
        {
            movementHints.Add(actor.Position, actor.Position + new WDir(10f, 10f), Colors.Danger);
        }

        public override void AddGlobalHints(GlobalHints hints)
        {
            hints.Add("Global");
        }

        public override void DrawArenaBackground(int pcSlot, Actor pc)
        {
            Arena.ZoneCircle(Module.Center, 10f, Colors.AOE);
        }

        public override void DrawArenaForeground(int pcSlot, Actor pc)
        {
            Arena.Actor(Module.Center, default, Colors.PC);
        }
    }

    public DemoModule(WorldState ws, Actor primary) : base(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f))
    {
        ActivateComponent<DemoComponent>();
    }
}
