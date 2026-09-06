// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A12IrminsulSawtooth;

class WhiteBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WhiteBreath, new AOEShapeCone(28f, 60f.Degrees()));
class MeanThrash(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MeanThrash, new AOEShapeCone(12f, 60f.Degrees()));
class MeanThrashKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.MeanThrash, 10f, stopAtWall: true);
class MucusBomb(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.MucusBomb, 10f);
class MucusSpray(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MucusSpray2, new AOEShapeDonut(6f, 20f));
class Rootstorm(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Rootstorm);
class Ambush(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Ambush, 9f);
class AmbushKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Ambush, 30f, stopAtWall: true, kind: Kind.TowardsOrigin);

class ShockwaveStomp(ModuleBase module) : Components.CastLineOfSightAOE(module, (uint)AID.ShockwaveStomp, 70f)
{
    public override ReadOnlySpan<Actor> BlockerActors()
    {
        var boulders = Module.Enemies((uint)OID.Irminsul);
        var count = boulders.Count;
        if (count == 0)
            return [];
        var actors = new List<Actor>();
        for (var i = 0; i < count; ++i)
        {
            var b = boulders[i];
            if (!b.IsDead)
                actors.Add(b);
        }
        return CollectionsMarshal.AsSpan(actors);
    }
}

[ModuleInfo(CFCID = 120u, NameID = 4623u, PrimaryActorOID = (uint)OID.Irminsul, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A12IrminsulSawtooth(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, 130), arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Donut(new(default, 130f), 8, 35)]);
    private Actor? _sawtooth;

    public Actor? Irminsul() => PrimaryActor;
    public Actor? Sawtooth() => _sawtooth;

    protected override void UpdateModule()
    {
        _sawtooth ??= GetActor((uint)OID.Sawtooth);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_sawtooth);
        Arena.Actors(Enemies((uint)OID.ArkKed));
    }
}
