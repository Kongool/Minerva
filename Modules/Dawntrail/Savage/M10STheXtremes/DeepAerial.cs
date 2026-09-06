// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M10STheXtremes;

sealed class DeepAerial(ModuleBase module) : Components.CastTowers(module, (uint)AID.DeepAerial, 6f, 2, 2);
sealed class WateryGrave(ModuleBase module) : Components.Adds(module, (uint)OID.WateryGrave)
{
    // show circle size to avoid moving tethers over it
    // add has radius of 4
    // do charges count if very edge of rect clips radius, or closer towards middle of rect?
    // yes it does; rect has to be completely clear of add radius
    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        base.DrawArenaBackground(pcSlot, pc);

        if (ActiveActors.Count > 0)
            Arena.ZoneCircleOutline(Center, 4f, default);
    }
}

// can't use BaitTether since tethers are coming out from helpers, not bosses
// both tethers are same IDs; need to use icons for blue/red specific hints (blue avoid orb, red go through orb)
// tether length roughly 30f until safe
// could merge into 1 component? reduce duplicate code
sealed class RedTether(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeRect(60f, 4f), (uint)IconID.TetherRed, (uint)AID.XtremeWaveRed)
{
    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        // no hints for people inside bubble
        if (actor.FindStatus((uint)SID.WateryGrave) != null)
            return;

        if (!IsBaitTarget(actor))
        {
            base.AddHints(slot, actor, hints);
            return;
        }

        var source = BaitSource(actor);
        if (source == null)
            return;

        if (actor.DistanceToPoint(source.Position) <= 30f)
        {
            hints.Add("Stretch tether!");
        }

        var orbs = Module.Enemies((uint)OID.WateryGrave);
        if (orbs.Count == 0)
            return;

        var orb = orbs[0];
        var pos = Module.PrimaryActor.Position;
        var rotation = (actor.Position - pos).ToAngle();
        // bigger halfwidth since orb has radius of 4
        // should be fine with 8 halfwidth but keep at 7 for visual clarity + safety
        var inside = orb.Position.InRect(pos, rotation, 60f, 0f, 7f);
        if (!inside)
        {
            hints.Add("Bait through orb!");
        }
    }
}
//sealed class BlueTether(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeRect(60f, 4f), (uint)IconID.TetherBlue, (uint)AID.XtremeWaveBlue, source: module.Enemies((uint)OID.DeepBlue)[0])
sealed class BlueTether(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeRect(60f, 4f), (uint)IconID.TetherBlue, (uint)AID.XtremeWaveBlue)
{
    public override Actor? BaitSource(Actor target)
    {
        var enemies = Module.Enemies((uint)OID.DeepBlue);
        if (enemies.Count == 0)
            return default;

        return enemies[0];
    }
    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        // no hints for people inside bubble
        if (actor.FindStatus((uint)SID.WateryGrave) != null)
            return;

        if (!IsBaitTarget(actor))
        {
            base.AddHints(slot, actor, hints);
            return;
        }

        var source = BaitSource(actor);
        if (source == null)
            return;

        if (actor.DistanceToPoint(source.Position) <= 30f)
        {
            hints.Add("Stretch tether!");
        }

        var orbs = Module.Enemies((uint)OID.WateryGrave);
        if (orbs.Count == 0)
            return;

        var enemies = Module.Enemies((uint)OID.DeepBlue);
        if (enemies.Count == 0)
            return;

        var orb = orbs[0];
        var pos = enemies[0].Position;
        var rotation = (actor.Position - pos).ToAngle();
        // bigger halfwidth since orb has radius of 4
        // counts even if it clips the edge; make halfwidth slightly bigger for safety
        var inside = orb.Position.InRect(pos, rotation, 60f, 0f, 9f);
        if (inside)
        {
            hints.Add("Bait away from orb!");
        }
    }
}

sealed class RedBlueTether(ModuleBase module) : Components.GenericBaitAway(module)
{
    private Actor? _blue;
    private readonly AOEShapeRect _rect = new(60f, 4f);

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.TetherRed && actor.OID == (uint)OID.RedHot)
        {
            CurrentBaits.Add(new(Module.PrimaryActor, World.Actors.Find(targetID) ?? actor, _rect));
        }
        if (iconID == (uint)IconID.TetherBlue && actor.OID == (uint)OID.DeepBlue)
        {
            var enemies = Module.Enemies((uint)OID.DeepBlue);
            if (enemies.Count == 0)
                return;

            _blue = enemies[0];
            CurrentBaits.Add(new(_blue, World.Actors.Find(targetID) ?? actor, _rect));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (CurrentBaits.Count != 0 && spell.Action.ID is (uint)AID.XtremeWaveRed or (uint)AID.XtremeWaveBlue)
        {
            CurrentBaits.RemoveAt(0);
        }
    }

    public override void Update()
    {
        var count = CurrentBaits.Count - 1;
        for (var i = count; i >= 0; --i)
        {
            ref var b = ref CurrentBaits.Ref(i);
            if (b.Target.IsDead)
            {
                CurrentBaits.RemoveAt(i);
            }
        }
    }
}
