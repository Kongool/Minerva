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
            .ActivateOnEnter<StormUnbound>()
            .ActivateOnEnter<Adds>()
            .ActivateOnEnter<UnmovingTroikaSecond>()
            .ActivateOnEnter<UnmovingTroikaLast>()
            .ActivateOnEnter<TheSwordUnbound>()
            .ActivateOnEnter<Concentrativity>()
            .ActivateOnEnter<HienAI>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, PrimaryActorOID = (uint)OID.BossP1, CFCID = 68721u, NameID = 6039u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class ZenosP1(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(233f, -93.25f), new ArenaBoundsCircle(20f));
