// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P2SHippokampos;

// state related to cataract mechanic
class Cataract(ModuleBase module) : ModuleComponent(module)
{
    private readonly AOEShapeRect _aoeBoss = new(50, 7.5f, 50);
    private readonly AOEShapeRect _aoeHead = new(50, 50, 0, module.PrimaryActor.CastInfo?.IsSpell(AID.WingedCataract) ?? false ? 180.Degrees() : default);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_aoeBoss.Check(actor.Position, Module.PrimaryActor) || _aoeHead.Check(actor.Position, Module.Enemies((uint)OID.CataractHead).FirstOrDefault()))
            hints.Add("GTFO from cataract!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        _aoeBoss.Draw(Arena, Module.PrimaryActor);
        _aoeHead.Draw(Arena, Module.Enemies((uint)OID.CataractHead).FirstOrDefault());
    }
}
