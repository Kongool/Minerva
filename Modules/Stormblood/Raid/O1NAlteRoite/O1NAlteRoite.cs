// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

using Minerva.Components;
namespace Minerva.Stormblood.Raid.O1NAlteRoite;

////////////////////////
// Raidwide Mechanics//
///////////////////////
sealed class Roar(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Roar);
sealed class ThinIce(ModuleBase module) : Components.ThinIce(module, 6f, true, (uint)SID.ThinIce, true);
sealed class Charybdis(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Charybdis);

////////////////////////
// AOE stuff         //
///////////////////////
sealed class ClampAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Clamp, new AOEShapeRect(9f + module.PrimaryActor.HitboxRadius, 5f));

////////////////////////
// Twinbolt stuff    //
///////////////////////
sealed class TwinBoltTetheredBuster(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.TwinBolt, hint: "Tankbuster! - Watch for Tethered Player!");
sealed class TwinBoltAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TwinBolt1, new AOEShapeCircle(5f));

////////////////////////
// Knockbacks        //
///////////////////////
sealed class ClampKB(ModuleBase module)
    : Components.SimpleKnockbacks(
        module,
        (uint)AID.Clamp,
        distance: 20f,
        kind: Components.GenericKnockback.Kind.DirForward,
        shape: new AOEShapeRect(9f + module.PrimaryActor.HitboxRadius, 5f)); // width 10 => halfwidth 5

sealed class BreathwingKB(ModuleBase module)
    : Components.SimpleKnockbacks(module, (uint)AID.BreathWing, distance: 20f, kind: Components.GenericKnockback.Kind.DirForward);

sealed class DownburstKB(ModuleBase module)
    : Components.SimpleKnockbacks(module, (uint)AID.Downburst, distance: 20f, kind: Components.GenericKnockback.Kind.AwayFromOrigin);

////////////////////////
// Tornado  Mechanics//
///////////////////////
sealed class DownburstTornado(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle _shape = new(5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        // same filter you used, but draw in danger color
        var aoes = new List<AOEInstance>();
        foreach (var a in Module.Enemies((uint)OID.Gen_AlteRoite)
                     .Where(a => a.Position.InCircle(Module.Center, 2f) && Module.PrimaryActor.CastInfo?.Action.ID == (uint)AID.Downburst))
        {
            aoes.Add(new(_shape, a.Position.Quantized(), a.Rotation, default, Colors.Danger));
        }
        return CollectionsMarshal.AsSpan(aoes);
    }
}
////////////////////////
// Stack + Spread    //
///////////////////////
sealed class BlazeLevinStackSpread(ModuleBase module)
    : Components.IconStackSpread(
        module,
        stackIcon: 62,
        spreadIcon: 108,
        stackAID: (uint)AID.Blaze,
        spreadAID: (uint)AID.Levinbolt,
        stackRadius: 6f,
        spreadRadius: 6f,
        activationDelay: 5.0);

////////////////////////
// Module Stuff       //
///////////////////////
[ModuleInfo(CFCID = 252u, NameID = 5629u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "JoeSparkx (ported from BMR)")]
public class O1NAlteRoite(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(00, 00), new ArenaBoundsCircle(20));
