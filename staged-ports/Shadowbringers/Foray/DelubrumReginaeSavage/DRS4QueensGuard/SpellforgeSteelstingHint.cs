// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS4QueensGuard;

// TODO: improve hints (check player's class; for healers, hints for party members having incorrect buff)
sealed class SpellforgeSteelstingHint(ModuleBase module) : ModuleComponent(module)
{
    private string _hint = "";
    public bool Active => _hint.Length > 0;

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (Active)
            hints.Add(_hint);
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        var hint = tether.ID switch
        {
            (uint)TetherID.PhysicalVulnerabilityDown => "Spellforge",
            (uint)TetherID.MagicVulnerabilityDown => "Steelsting",
            _ => ""
        };
        if (hint.Length > 0)
            _hint = hint;
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID is (uint)SID.PhysicalVulnerabilityDown or (uint)SID.MagicVulnerabilityDown)
            _hint = "";
    }
}
