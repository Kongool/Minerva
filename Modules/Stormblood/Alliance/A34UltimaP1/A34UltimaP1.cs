// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A34UltimaP1;

class HolyIVBait(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HolyIVBait, 6f);
class HolyIVSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.HolyIVSpread, 6f);
class AuralightAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AuralightAOE, 20f);
class AuralightRect(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AuralightRect, new AOEShapeRect(70f, 5f));
class GrandCrossAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandCrossAOE, new AOEShapeCross(60f, 7.5f));
class TimeEruption(ModuleBase module) : Components.SimpleAOEGroupsByTimewindow(module, [(uint)AID.TimeEruptionAOEFirst, (uint)AID.TimeEruptionAOESecond], new AOEShapeRect(20f, 10f), expectedNumCasters: 9);

class Eruption2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Eruption2, 8f);
class ControlTower2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ControlTower2, 6f);

abstract class ExtremeEdge(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(60f, 18f));
class ExtremeEdge1(ModuleBase module) : ExtremeEdge(module, (uint)AID.ExtremeEdge1);
class ExtremeEdge2(ModuleBase module) : ExtremeEdge(module, (uint)AID.ExtremeEdge2);

class CrushWeapon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CrushWeapon, 6f);
class Searchlight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Searchlight, 6f);
class HallowedBolt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HallowedBolt, 6f);

// Ultimate Illusion: a 22-second cast, then the arena is struck about once a second for twenty seconds (a range-80
// circle, so there is no edge to walk out of). The survivors are the ones standing in the shelter -- always the same
// spot, and on 2026-09-15 all eight living players were packed into a four-yalm patch there, taking about 950 of
// 28230 HP a tick and being healed straight back. Confirmed by the user: always that spot, every Ultimate Illusion.
// Nothing in BossmodReborn points at it.
sealed class UltimateIllusion(ModuleBase module) : ModuleComponent(module)
{
    /// <summary>The shelter. An event object (0x1EA1A1, radius 2) sits here; three more of its kind sit elsewhere in
    /// the arena and do nothing, so the position is taken rather than the object.</summary>
    private static readonly WPos Shelter = new(600f, -620f);

    /// <summary>Kept to the object's own radius. Players as far as 3.2 yalms from its centre survived, so this asks
    /// for less room than the shelter really gives.</summary>
    private const float Radius = 2f;

    /// <summary>How long after the last strike the shelter stops being the only safe ground.</summary>
    private const double TickGrace = 3d;

    private DateTime firstStrike;
    private DateTime until;

    private bool Active => this.until != default && this.World.CurrentTime < this.until;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.UltimateIllusion1)
        {
            this.firstStrike = this.Module.CastFinishAt(spell);
            this.until = this.firstStrike.AddSeconds(TickGrace);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.UltimateIllusion2 or (uint)AID.UltimateIllusion3)
            this.until = this.World.FutureTime(TickGrace);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.Active)
            hints.Add("Stand in the shelter!", !actor.Position.InCircle(Shelter, Radius));
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (this.Active)
            this.Arena.ZoneCircleOutline(Shelter, Radius, Colors.Safe, 2f);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.Active)
            hints.AddForbiddenZone(new SDInvertedCircle(Shelter, Radius), this.firstStrike);
    }
}

[ModuleInfo(CFCID = 636u, NameID = 7909u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A34UltimaP1(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(600f, -600f), new ArenaBoundsSquare(30f));
