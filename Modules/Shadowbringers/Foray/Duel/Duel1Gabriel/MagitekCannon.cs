// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.Duel.Duel1Gabriel;

sealed class MagitekCannonVoidzone(ModuleBase module) : Components.VoidzoneAtCastTargetGroup(module, 3f, [(uint)AID.MagitekCannonFirst, (uint)AID.MagitekCannonRest], GetVoidzones, 0.9f)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.MagitekCannonVoidzone);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

sealed class MagitekCannonChase(ModuleBase module) : Components.StandardChasingAOEs(module, 3f, (uint)AID.MagitekCannonFirst, (uint)AID.MagitekCannonRest, 30f, 3.2d, 5, true, (uint)IconID.MagitekCannon)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell) { }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        base.OnCastStarted(caster, spell);
        if (spell.Action.ID is var id && id == ActionFirst || id == ActionRest)
        {
            Advance(spell.LocXZ, MoveDistance, World.CurrentTime);
            if (Chasers.Count == 0)
            {
                Targets.Clear();
                NumCasts = 0;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (TargetsMask[slot])
        {
            hints.AddForbiddenZone(new SDCircle(Center, 13.5f));
        }
    }
}
