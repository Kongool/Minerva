// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C01ASS.C011Silkie;

abstract class PuffTethers(ModuleBase module, bool originAtBoss) : ModuleComponent(module)
{
    private readonly bool _originAtBoss = originAtBoss;
    private readonly PuffTracker? _tracker = module.FindComponent<PuffTracker>();
    private SlipperySoap.Color _bossColor;
    private readonly AOEShapeCross shapeBlue = new(60f, 5f);
    private readonly AOEShapeDonut shapeGreen = new(5f, 60f);
    private readonly AOEShapeCone shapeYellow = new(60f, 22.5f.Degrees());

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (_tracker == null)
            return;
        DrawTetherHints(pc, _tracker.ChillingPuffs, false);
        DrawTetherHints(pc, _tracker.FizzlingPuffs, true);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (_tracker == null)
            return;
        DrawTether(pc, _tracker.ChillingPuffs);
        DrawTether(pc, _tracker.FizzlingPuffs);
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (actor != Module.PrimaryActor)
            return;
        var color = SlipperySoap.ColorForStatus(status.ID);
        if (color != SlipperySoap.Color.None)
            _bossColor = color;
    }

    private void DrawTetherHints(Actor player, List<Actor> puffs, bool yellow)
    {
        var source = puffs.Find(p => p.Tether.Target == player.InstanceID);
        if (source == null)
            return;

        var moveDir = (player.Position - source.Position).Normalized();
        var movePos = source.Position + 10f * moveDir;
        var moveAngle = Angle.FromDirection(moveDir);
        if (yellow)
        {
            shapeYellow.Draw(Arena, movePos, moveAngle + 45f.Degrees(), Colors.Other6);
            shapeYellow.Draw(Arena, movePos, moveAngle + 135f.Degrees(), Colors.Other6);
            shapeYellow.Draw(Arena, movePos, moveAngle - 135f.Degrees(), Colors.Other6);
            shapeYellow.Draw(Arena, movePos, moveAngle - 45f.Degrees(), Colors.Other6);
        }
        else
        {
            shapeBlue.Draw(Arena, movePos, moveAngle, Colors.Other6);
        }

        var bossOrigin = _originAtBoss ? Module.PrimaryActor.Position : Center;
        switch (_bossColor)
        {
            case SlipperySoap.Color.Green:
                shapeGreen.Draw(Arena, bossOrigin, new(), Colors.Other6);
                break;
            case SlipperySoap.Color.Blue:
                shapeBlue.Draw(Arena, bossOrigin, new(), Colors.Other6);
                break;
        }
    }

    private void DrawTether(Actor player, List<Actor> puffs)
    {
        var source = puffs.Find(p => p.Tether.Target == player.InstanceID);
        if (source != null)
        {
            Arena.AddLine(source.Position, player.Position);
        }
    }
}
sealed class PuffTethers1(ModuleBase module) : PuffTethers(module, false);
sealed class PuffTethers2(ModuleBase module) : PuffTethers(module, true);
