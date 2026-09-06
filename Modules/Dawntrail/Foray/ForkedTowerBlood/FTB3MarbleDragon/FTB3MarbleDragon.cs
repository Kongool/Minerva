// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerBlood.FTB3MarbleDragon;

sealed class ImitationStar(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.ImitationStarVisual, (uint)AID.ImitationStar, 1.9f);
sealed class ImitationRain(ModuleBase module) : Components.RaidwideInstant(module, (uint)AID.ImitationRain);
sealed class WitheringEternity(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.WitheringEternity, (uint)AID.ImitationRain, 2.6f);
sealed class WickedWater(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.WickedWater, (uint)AID.ImitationRain, 2.7f);
sealed class ImitationIcicle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ImitationIcicle, 8f);
sealed class DreadDeluge(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.DreadDeluge);

[SkipLocalsInit]
sealed class FrigidTwister(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<Actor> voidzones = module.Enemies((uint)OID.Icewind);
    private readonly AOEShapeCircle circle = new(5f);
    private readonly AOEShapeArcCapsule arcCW = new(5f, 25f.Degrees(), module.Center), arcCCW = new(5f, -25f.Degrees(), module.Center);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = voidzones.Count;
        if (count == 0)
        {
            return [];
        }
        var aoes = new AOEInstance[count];
        var center = Center;
        for (var i = 0; i < count; ++i)
        {
            var vz = voidzones[i];
            var pos = vz.Position;
            if (vz.LastFrameMovement == default)
            {
                aoes[i] = new(circle, pos.Quantized());
            }
            else
            {
                var dir = pos - center;
                var ccw = vz.Rotation.ToDirection().OrthoR().Dot(dir) < 0f;
                aoes[i] = new(ccw ? arcCCW : arcCW, pos.Quantized());
            }
        }
        return aoes;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = voidzones.Count;
        if (count == 0)
        {
            return;
        }
        var forbiddenNearFuture = World.FutureTime(1.1d);
        var forbiddenSoon = World.FutureTime(3d);
        var forbiddenFarFuture = DateTime.MaxValue;
        var center = Center;
        var a15 = 15f.Degrees();
        var a25 = 25f.Degrees();
        var a35 = 35f.Degrees();
        for (var i = 0; i < count; ++i)
        {
            var vz = voidzones[i];
            var pos = vz.Position;
            var dir = pos - center;
            var ccw = vz.Rotation.ToDirection().OrthoR().Dot(dir) < 0f;
            var mult = ccw ? -1f : 1f;
            var mov = vz.LastFrameMovement != default;
            if (mov)
            {
                hints.AddForbiddenZone(new SDArcCapsule(pos, center, mult * a15, 5f), forbiddenNearFuture);
                hints.AddForbiddenZone(new SDArcCapsule(pos, center, mult * a25, 5f), forbiddenSoon);
                hints.AddForbiddenZone(new SDArcCapsule(pos, center, mult * a35, 5f), forbiddenFarFuture);
            }
            hints.TemporaryObstacles.Add(new SDCircle(pos.Quantized(), mov ? 5f : 6f));
        }
    }
}
sealed class FrigidDive(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FrigidDive, new AOEShapeRect(60f, 10f));
sealed class GelidGaol(ModuleBase module) : Components.Adds(module, (uint)OID.GelidGaol, 1);

[ModuleInfo(Group = ModuleGroup.TheForkedTowerBlood, CFCID = 1018u, NameID = 13838u, PrimaryActorOID = (uint)OID.MarbleDragon, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class FTB3MarbleDragon(WorldState ws, Actor primary) : ModuleBase(ws, primary, startingArena.Center, startingArena)
{
    public static readonly WPos ArenaCenter = new(-337f, 157f);
    private static readonly ArenaBoundsCustom startingArena = new([new Polygon(ArenaCenter, 39.5f, 48)], [new Rectangle(new(-337f, 116.853f), 11f, 1.25f),
    new Rectangle(new(-337f, 197.413f), 11f, 1.25f)]);
    public static readonly ArenaBoundsCircle DefaultArena = new(30f);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        if (PrimaryActor.FindStatus((uint)SID.Invincibility) == null)
            Arena.Actor(PrimaryActor);
    }
}
