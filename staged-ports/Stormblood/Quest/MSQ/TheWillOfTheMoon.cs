// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;


namespace Minerva.Stormblood.Quest.MSQ.TheWillOfTheMoon;

public enum OID : uint
{
    Boss = 0x24A0,
    Magnai = 0x24A1,
    KhunShavar = 0x252F, // R1.82
    Hien = 0x24A3,
    Daidukul = 0x24A2, // R0.5
    TheScaleOfTheFather = 0x2532, // R1.0
    Helper = 0x233C
}

public enum AID : uint
{
    DispellingWind = 13223, // Boss->self, 3.0s cast, range 40+R width 8 rect
    Epigraph = 13225, // 252D->self, 3.0s cast, range 45+R width 8 rect
    WhisperOfLivesPast = 13226, // 252E->self, 3.5s cast, range -12 donut
    AncientBlizzard = 13227, // 252F->self, 3.0s cast, range 40+R 45-degree cone
    Tornado = 13228, // 252F->location, 5.0s cast, range 6 circle
    Epigraph2 = 13222, // 2530->self, 3.0s cast, range 45+R width 8 rect
    FlatlandFury = 13244, // 2532->self, 17.0s cast, range 10 circle
    FlatlandFuryEnrage = 13329, // 249F->self, 25.0s cast, range 10 circle
    ViolentEarth = 13236, // 233C->location, 3.0s cast, range 6 circle
    WindChisel = 13518, // 233C->self, 2.0s cast, range 34+R 20-degree cone
    TranquilAnnihilation = 13233, // _Gen_DaidukulTheMirthful->24A3, 15.0s cast, single-target
}

public enum SID : uint
{
    Invincibility = 775 // none->Boss, extra=0x0
}

class DispellingWind(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DispellingWind, new AOEShapeRect(40f, 4f));
class Epigraph(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Epigraph, new AOEShapeRect(45f, 4f));
class Whisper(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WhisperOfLivesPast, new AOEShapeDonut(6f, 12f));
class Blizzard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientBlizzard, new AOEShapeCone(40f, 22.5f.Degrees()));
class Tornado(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Tornado, 6f);
class Epigraph1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Epigraph2, new AOEShapeRect(45f, 4f));

public class FlatlandFury(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlatlandFury, 10f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // if all 9 adds are alive, instead of drawing forbidden zones (which would fill the whole arena), force AI to target nearest one to kill it
        if (ActiveCasters.Length == 9)
            hints.ForcedTarget = Module.Enemies((uint)OID.TheScaleOfTheFather).MinBy(actor.DistanceToHitbox);
        else
            base.AddAIHints(slot, actor, assignment, hints);
    }
}

public class FlatlandFuryEnrage(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlatlandFuryEnrage, 10f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (ActiveCasters.Length < 9)
            base.AddAIHints(slot, actor, assignment, hints);
    }
}

public class ViolentEarth(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ViolentEarth, 6f);
public class WindChisel(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WindChisel, new AOEShapeCone(34f, 10f.Degrees()));

public class Scales(ModuleBase module) : Components.Adds(module, (uint)OID.TheScaleOfTheFather);
class P1Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            if (e.Actor.FindStatus((uint)SID.Invincibility) != null)
                e.Priority = AIHints.Enemy.PriorityInvincible;

            // they do very little damage and sadu will raise them after a short delay, no point in attacking
            if (e.Actor.OID == (uint)OID.KhunShavar)
                e.Priority = AIHints.Enemy.PriorityPointless;
        }
    }
}

class P2Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID == (uint)OID.Magnai ? 1 : 0;
        }
    }
}

class SaduHeavensflameStates : StateMachineBuilder
{
    public SaduHeavensflameStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<P1Hints>()
            .ActivateOnEnter<DispellingWind>()
            .ActivateOnEnter<Epigraph>()
            .ActivateOnEnter<Whisper>()
            .ActivateOnEnter<Blizzard>()
            .ActivateOnEnter<Tornado>()
            .ActivateOnEnter<Epigraph1>()
            .Raw.Update = () => module.Enemies((uint)OID.Magnai).Count != 0;
        TrivialPhase(1)
            .ActivateOnEnter<P2Hints>()
            .ActivateOnEnter<Scales>()
            .ActivateOnEnter<FlatlandFury>()
            .ActivateOnEnter<FlatlandFuryEnrage>()
            .ActivateOnEnter<ViolentEarth>()
            .ActivateOnEnter<WindChisel>()
            .OnEnter(() =>
            {
                module.Arena.Center = new(-186.5f, 550.5f);
            })
            .Raw.Update = () => module.Raid.Player()?.IsDeadOrDestroyed ?? true;
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68683u, NameID = 6152u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class SaduHeavensflame(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-223f, 519f), new ArenaBoundsCircle(20u))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(World.Actors.Where(x => !x.IsAlly));
    }
}
