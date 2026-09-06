// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P5SProtoCarbuncle;

// this includes venom pools and raging claw/searing ray aoes
class RubyGlow4(ModuleBase module) : RubyGlowRecolor(module, 5)
{
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var poison = ActivePoisonAOEs();
        var len = poison.Length;
        var aoes = new List<AOEInstance>(len + 2);
        if (CurRecolorState != RecolorState.BeforeStones && MagicStones.Count != 0)
            aoes.Add(new(ShapeHalf, Center.Quantized(), Angle.FromDirection(QuadrantDir(AOEQuadrant))));
        aoes.AddRange(poison);

        if (Module.PrimaryActor.CastInfo?.IsSpell(AID.RagingClaw) ?? false)
            aoes.Add(new(ShapeHalf, Module.PrimaryActor.Position, Module.PrimaryActor.CastInfo.Rotation, Module.CastFinishAt(Module.PrimaryActor.CastInfo)));
        if (Module.PrimaryActor.CastInfo?.IsSpell(AID.SearingRay) ?? false)
            aoes.Add(new(ShapeHalf, Center.Quantized(), Module.PrimaryActor.CastInfo.Rotation + 180f.Degrees(), Module.CastFinishAt(Module.PrimaryActor.CastInfo)));
        return CollectionsMarshal.AsSpan(aoes);
    }
}
