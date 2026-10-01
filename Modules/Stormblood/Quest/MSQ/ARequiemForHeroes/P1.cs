// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.MSQ.ARequiemForHeroes;

// Hien, whom you play against Zenos, ported from BossmodReborn's AutoHien: the Kyokufu > Gofu > Yagetsu combo, Ajisai
// kept on the target, Second Wind low, Hissatsu: Gyoten on cooldown. Stripped in the port; restored 2026-09-30 ("the hien
// fight daedalus didnt do any rotations").
class AutoHien(WorldState ws) : QuestBattle.UnmanagedRotation(ws, 3f)
{
    protected override void Exec(Actor? primaryTarget)
    {
        if (primaryTarget == null)
            return;

        Hints.GoalZones.Add(AIHints.GoalSingleTarget(primaryTarget, 3f));

        var ajisai = StatusDetails(primaryTarget, Roleplay.SID.Ajisai, Player.InstanceID);

        switch (ComboAction)
        {
            case Roleplay.AID.Gofu:
                UseAction(Roleplay.AID.Yagetsu, primaryTarget);
                break;

            case Roleplay.AID.Kyokufu:
                UseAction(Roleplay.AID.Gofu, primaryTarget);
                break;

            default:
                if (ajisai.Left < 5)
                    UseAction(Roleplay.AID.Ajisai, primaryTarget);
                UseAction(Roleplay.AID.Kyokufu, primaryTarget);
                break;
        }

        if (Player.HPMP.CurHP < 5000)
            UseAction(Roleplay.AID.SecondWind, Player, -10f);

        UseAction(Roleplay.AID.HissatsuGyoten, primaryTarget, -10f);
    }
}

class HienAI(ModuleBase module) : QuestBattle.RotationModule<AutoHien>(module);

// P1's own mechanics. Its ids differ from P2's throughout, so the P2 components borrowed here answered to none of them:
// nothing was drawn or dodged while you played Hien (2026-09-30, "hien fight no ai dodging").
class VeinSplitterP1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VeinSplitterP1, 10f);
class ThunderousForce(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThunderousForce, 8f);
class UnmovingTroikaP1Second(ModuleBase module) : UnmovingTroika(module, (uint)AID.UnmovingTroikaP1Second);
class UnmovingTroikaP1Last(ModuleBase module) : UnmovingTroika(module, (uint)AID.UnmovingTroikaP1Last);

// four Specters of Zenos, north, south, east and west, each a 6-yalm line through the middle
class ArtOfTheSwordP1A(ModuleBase module) : ArtOfTheSword(module, (uint)AID.ArtOfTheSwordP1A);
class ArtOfTheSwordP1B(ModuleBase module) : ArtOfTheSword(module, (uint)AID.ArtOfTheSwordP1B);

// the same 8-yalm shove as P2's: stand within 8 yalms of the middle so it keeps you on the floor
class SwellP1(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.ArtOfTheSwellP1, 8f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
            hints.AddForbiddenZone(new SDInvertedCircle(Center, 8f));
    }
}

class ConcentrativityP1(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ConcentrativityP1);
class DarknessP1(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DarknessP1);

public class ZenosP1States : StateMachineBuilder
{
    public ZenosP1States(ModuleBase module) : base(module)
    {
        // BossmodReborn's P1 carries only the kit; its mechanics are P2's components, which answer to their own casts,
        // so they are safe here and draw what P1 casts (2026-09-30: Lightless Spark's cone went undrawn and undodged).
        TrivialPhase()
            .ActivateOnEnter<FloodOfDarkness>()
            .ActivateOnEnter<VeinSplitter>()
            .ActivateOnEnter<LightlessSpark>()
            .ActivateOnEnter<LightlessSpark2>()
            .ActivateOnEnter<SwellUnbound>()
            .ActivateOnEnter<Swell>()
            .ActivateOnEnter<ArtOfTheSword1>()
            .ActivateOnEnter<ArtOfTheSword2>()
            .ActivateOnEnter<ArtOfTheSword3>()
            .ActivateOnEnter<ArtOfTheStorm>()
            .ActivateOnEnter<EntropicFlame>()
            .ActivateOnEnter<DarkAether>()
            .ActivateOnEnter<DarkAetherSpawn>()
            .ActivateOnEnter<StormUnbound>()
            .ActivateOnEnter<Adds>()
            .ActivateOnEnter<UnmovingTroikaSecond>()
            .ActivateOnEnter<UnmovingTroikaLast>()
            .ActivateOnEnter<TheSwordUnbound>()
            .ActivateOnEnter<Concentrativity>()
            .ActivateOnEnter<VeinSplitterP1>()
            .ActivateOnEnter<ThunderousForce>()
            .ActivateOnEnter<UnmovingTroikaP1Second>()
            .ActivateOnEnter<UnmovingTroikaP1Last>()
            .ActivateOnEnter<ArtOfTheSwordP1A>()
            .ActivateOnEnter<ArtOfTheSwordP1B>()
            .ActivateOnEnter<SwellP1>()
            .ActivateOnEnter<ConcentrativityP1>()
            .ActivateOnEnter<DarknessP1>()
            .ActivateOnEnter<HienAI>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, PrimaryActorOID = (uint)OID.BossP1, CFCID = 68721u, NameID = 6039u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class ZenosP1(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(233f, -93.25f), new ArenaBoundsCircle(20f));
