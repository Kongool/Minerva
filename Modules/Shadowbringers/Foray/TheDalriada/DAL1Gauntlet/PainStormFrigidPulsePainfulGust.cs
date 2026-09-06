// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL1Gauntlet;

sealed class PainStormFrigidPulsePainfulGust(ModuleBase module) : Components.GenericAOEs(module)
{
    public readonly List<AOEInstance> AOEs = [];
    private static readonly AOEShapeCircle circle = new(20f);
    private static readonly AOEShapeDonut donut = new(8f, 25f);
    private static readonly AOEShapeCone cone = new(35f, 65f.Degrees());
    private readonly NorthSouthwind _kb = module.FindComponent<NorthSouthwind>()!;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.FrigidPulse or (uint)AID.FrigidPulseShadow => donut,
            (uint)AID.PainStorm or (uint)AID.PainStormShadow => cone,
            (uint)AID.PainfulGust or (uint)AID.PainfulGustShadow => circle,
            _ => null
        };
        if (shape != null)
        {
            AOEs.Add(new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell), actorID: caster.InstanceID));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        var count = AOEs.Count;
        var id = caster.InstanceID;
        var aoes = CollectionsMarshal.AsSpan(AOEs);
        for (var i = 0; i < count; ++i)
        {
            if (aoes[i].ActorID == id)
            {
                AOEs.RemoveAt(i);
                return;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var kbs = _kb.ActiveKnockbacks(slot, actor);
        if (kbs.Length != 0 && !_kb.IsImmune(slot, kbs[0].Activation))
        { }
        else
        {
            base.AddAIHints(slot, actor, assignment, hints);
        }
    }
}
