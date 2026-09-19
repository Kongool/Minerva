// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A31Mustadio;

class EnergyBurst(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.EnergyBurst);
class ArmShot(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.ArmShot);
class LegShot(ModuleBase module) : Components.Voidzone(module, 3f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.LegShotVoidzone);
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

abstract class Handgonne(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(30f, 105f.Degrees()));
class LeftHandgonne(ModuleBase module) : Handgonne(module, (uint)AID.LeftHandgonne);
class RightHandgonne(ModuleBase module) : Handgonne(module, (uint)AID.RightHandgonne);

class SatelliteBeam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SatelliteBeam, new AOEShapeRect(30, 15)); // Satellite Beam and Compress can both be shown earleir through tether
class Compress(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Compress, new AOEShapeRect(100, 7.5f));

class BallisticSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.BallisticImpact1, 6);

// Analysis marks every player with the side Mustadio must not see -- the border drawn round your feet with one side
// missing -- and Last Testament then kills whoever has that side turned toward him. He fires it from off the arena
// (2026-09-15: he moved to (642,248) nine seconds before the cast), so the direction to turn changes with him.
//
// Measured from that pull, both casts, boss-relative facing at the resolve (+ is to your right):
//   BackUnseen  hit at +175, safe at 0      LeftUnseen hit at -99, safe at -91, -54, -4, +7
//   RightUnseen hit at +88, +90, +95, +98, +136, safe at +82 and -56
// So the marked side is forbidden to face him, roughly a quadrant wide. The arc below is a little wider than that,
// since turning further away costs nothing. Neither BossmodReborn nor the port had anything for this.
sealed class Analysis(ModuleBase module) : ModuleComponent(module)
{
    /// <summary>Where the boss may not be, relative to the character's own facing.</summary>
    private static readonly (uint Status, float Deg, string Side)[] Marks =
    [
        ((uint)SID.RightUnseen, 90f, "right"),
        ((uint)SID.LeftUnseen, -90f, "left"),
        ((uint)SID.BackUnseen, 180f, "back"),
    ];

    private const float ForbiddenHalfWidthDeg = 60f;

    private readonly Dictionary<ulong, int> marked = [];
    private DateTime resolveAt;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        for (var i = 0; i < Marks.Length; ++i)
            if (status.ID == Marks[i].Status)
                this.marked[actor.InstanceID] = i;
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        for (var i = 0; i < Marks.Length; ++i)
            if (status.ID == Marks[i].Status)
                this.marked.Remove(actor.InstanceID);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.LastTestament)
            this.resolveAt = this.Module.CastFinishAt(spell);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.LastTestament)
            this.resolveAt = default;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.marked.TryGetValue(actor.InstanceID, out var i))
            hints.Add($"Keep your {Marks[i].Side} away from Mustadio", this.resolveAt != default);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.resolveAt == default || !this.marked.TryGetValue(actor.InstanceID, out var i))
            return;
        var toBoss = Angle.FromDirection(this.Module.PrimaryActor.Position - actor.Position);
        hints.ForbiddenDirections.Add((toBoss + Marks[i].Deg.Degrees(), ForbiddenHalfWidthDeg.Degrees(), this.resolveAt));
    }
}

[ModuleInfo(CFCID = 636u, NameID = 7915u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")] // 7919 
public class A31Mustadio(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(600, 290), new ArenaBoundsSquare(30, 45.Degrees()));
