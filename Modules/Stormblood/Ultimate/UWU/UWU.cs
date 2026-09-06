// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

sealed class P1Slipstream(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Slipstream, new AOEShapeCone(11.7f, 45f.Degrees()));
sealed class P1Downburst(ModuleBase module) : Components.Cleave(module, (uint)AID.Downburst, new AOEShapeCone(11.7f, 45f.Degrees()));
sealed class P1EyeOfTheStorm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EyeOfTheStorm, new AOEShapeDonut(12f, 25f));
sealed class P1Gigastorm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Gigastorm, 6.5f);
sealed class P2RadiantPlume(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RadiantPlumeAOE, 8f);
sealed class P2Incinerate(ModuleBase module) : Components.Cleave(module, (uint)AID.Incinerate, new AOEShapeCone(15f, 60f.Degrees()), [(uint)OID.Ifrit]);
sealed class P3RockBuster(ModuleBase module) : Components.Cleave(module, (uint)AID.RockBuster, new AOEShapeCone(10.55f, 60f.Degrees()), [(uint)OID.Titan]); // TODO: verify angle
sealed class P3MountainBuster(ModuleBase module) : Components.Cleave(module, (uint)AID.MountainBuster, new AOEShapeCone(15.55f, 45f.Degrees()), [(uint)OID.Titan]); // TODO: verify angle
sealed class P3WeightOfTheLand(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WeightOfTheLandAOE, 6f);
sealed class P3Upheaval(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Upheaval, 24f, true);
sealed class P3Tumult(ModuleBase module) : Components.CastCounter(module, (uint)AID.Tumult);
sealed class P4Blight(ModuleBase module) : Components.CastCounter(module, (uint)AID.Blight);
sealed class P4HomingLasers(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.HomingLasers, 4f);
sealed class P4DiffractiveLaser(ModuleBase module) : Components.Cleave(module, (uint)AID.DiffractiveLaser, new AOEShapeCone(18f, 45f.Degrees()), [(uint)OID.UltimaWeapon]); // TODO: verify angle
sealed class P5MistralSongCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MistralSongCone, new AOEShapeCone(21.7f, 75f.Degrees()));

sealed class P5AetherochemicalLaser(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.AetherochemicalLaserCenter, (uint)AID.AetherochemicalLaserRight, (uint)AID.AetherochemicalLaserLeft], new AOEShapeRect(46f, 4f));

sealed class P5LightPillar(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightPillarAOE, 3); // TODO: consider showing circle around baiter
sealed class P5AethericBoom(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.AethericBoom, 10);

[ModuleInfo(CFCID = 539u, NameID = 0u, PrimaryActorOID = (uint)OID.Garuda, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class UWU : ModuleBase
{
    public static readonly WPos ArenaCenter = new(100f, 100f);
    private readonly List<Actor> _titan;
    private readonly List<Actor> _lahabrea;
    private readonly List<Actor> _ultima;
    private Actor? _mainIfrit;

    public List<Actor> Ifrits;

    public Actor? Garuda() => PrimaryActor.IsDestroyed ? null : PrimaryActor;
    public Actor? Ifrit() => _mainIfrit;
    public Actor? Titan() => _titan.Count != 0 ? _titan[0] : null;
    public Actor? Lahabrea() => _lahabrea.Count != 0 ? _lahabrea[0] : null;
    public Actor? Ultima() => _ultima.Count != 0 ? _ultima[0] : null;

    public UWU(WorldState ws, Actor primary) : base(ws, primary, ArenaCenter, new ArenaBoundsCustom([new Polygon(ArenaCenter, 20f, 64)]))
    {
        Ifrits = Enemies((uint)OID.Ifrit);
        _titan = Enemies((uint)OID.Titan);
        _lahabrea = Enemies((uint)OID.Lahabrea);
        _ultima = Enemies((uint)OID.UltimaWeapon);
    }

    public override bool ShouldPrioritizeAllEnemies => true;

    protected override void UpdateModule()
    {
        if (_mainIfrit == null && StateMachine.ActivePhaseIndex == 1)
        {
            var count = Ifrits.Count;
            for (var i = 0; i < count; ++i)
            {
                var ifrit = Ifrits[i];
                if (ifrit.IsTargetable)
                {
                    _mainIfrit = ifrit;
                    return;
                }
            }
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(Garuda());
        Arena.Actor(Ifrit());
        Arena.Actor(Titan());
        Arena.Actor(Lahabrea());
        Arena.Actor(Ultima());
    }
}
