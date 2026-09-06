// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UCOB;

class P1Plummet(ModuleBase module) : Components.Cleave(module, (uint)AID.Plummet, new AOEShapeCone(12f, 60f.Degrees()), [(uint)OID.Twintania]);
class P1Fireball(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.Fireball, (uint)AID.Fireball, 4f, 5.3f, 4, 4);
class P2BahamutsClaw(ModuleBase module) : Components.CastCounter(module, (uint)AID.BahamutsClaw);
class P3FlareBreath(ModuleBase module) : Components.Cleave(module, (uint)AID.FlareBreath, new AOEShapeCone(29.2f, 45f.Degrees()), [(uint)OID.BahamutPrime]); // TODO: verify angle
class P5MornAfah(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.MornAfah, 4f, 8, 8); // TODO: verify radius

[ModuleInfo(CFCID = 280u, NameID = 0u, PrimaryActorOID = (uint)OID.Twintania, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class UCOB(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, new ArenaBoundsCircle(21f))
{
    private Actor? _nael;
    private Actor? _bahamutPrime;

    public Actor? Twintania() => PrimaryActor.IsDestroyed ? null : PrimaryActor;
    public Actor? Nael() => _nael;
    public Actor? BahamutPrime() => _bahamutPrime;

    public override bool ShouldPrioritizeAllEnemies => true;

    protected override void UpdateModule()
    {
        _nael ??= GetActor((uint)OID.NaelDeusDarnus);
        _bahamutPrime ??= GetActor((uint)OID.BahamutPrime);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(Twintania());
        Arena.Actor(Nael());
        Arena.Actor(BahamutPrime());
    }
}
