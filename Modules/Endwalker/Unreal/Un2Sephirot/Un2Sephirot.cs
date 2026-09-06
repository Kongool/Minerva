// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un2Sephirot;

class P1TripleTrial(ModuleBase module) : Components.Cleave(module, (uint)AID.TripleTrial, new AOEShapeCone(18.5f, 30.Degrees())); // TODO: verify angle
class P1Ein(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Ein, new AOEShapeRect(50, 22.5f));
class P2GenesisCochma(ModuleBase module) : Components.CastCounter(module, (uint)AID.GenesisCochma);
class P2GenesisBinah(ModuleBase module) : Components.CastCounter(module, (uint)AID.GenesisBinah);
class P3EinSofOhr(ModuleBase module) : Components.CastCounter(module, (uint)AID.EinSofOhrAOE);
class P3Yesod(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Yesod, 4);
class P3PillarOfMercyAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PillarOfMercyAOE, 5);
class P3PillarOfMercyKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.PillarOfMercyAOE, 17);
class P3Malkuth(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Malkuth, 25);
class P3Ascension(ModuleBase module) : Components.CastCounter(module, (uint)AID.Ascension); // TODO: show safe spot?..
class P3PillarOfSeverity(ModuleBase module) : Components.CastCounter(module, (uint)AID.PillarOfSeverityAOE);

[ModuleInfo(Group = ModuleGroup.RemovedUnreal, CFCID = 875u, NameID = 4776u, PrimaryActorOID = (uint)OID.BossP1, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Un2Sephirot(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, new ArenaBoundsCircle(20f))
{
    public Actor? BossP1() => PrimaryActor.IsDestroyed ? null : PrimaryActor;

    private Actor? _bossP3;
    public Actor? BossP3() => _bossP3;

    protected override void UpdateModule()
    {
        _bossP3 ??= GetActor((uint)OID.BossP3);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        if (StateMachine.ActivePhaseIndex <= 0)
            Arena.Actor(PrimaryActor);
        else if (StateMachine.ActivePhaseIndex == 2)
            Arena.Actor(_bossP3);
    }
}
