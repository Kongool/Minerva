// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;
using Minerva.QuestBattle.Stormblood.MSQ;

namespace Minerva.Stormblood.Quest.MSQ.EmissaryOfTheDawn;

public enum OID : uint
{
    Boss = 0x234B,
    Helper = 0x233C
}

public enum AID : uint
{
    TerminusEst = 11538, // Menacing Mercenary (0x234E)->self, 2.2s cast, range 40 width 4 rect
}

// The Menacing Mercenaries line up and each fires Terminus Est down its own facing: fifteen casts in five volleys in
// the one recording (newtoon1, 2026-09-28 21:15), every one of which hit an ally and none the player. BossmodReborn's
// module has no component for it, and with a module in charge nothing else draws it.
class TerminusEst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TerminusEst, new AOEShapeRect(40f, 2f));

class LB(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (World.Actors.Any(x => x.OID == 0x2340 && x.FindStatus(1497) != null))
            hints.ActionsToExecute.Push(ActionID.MakeSpell(Roleplay.AID.Starstorm), null, ActionQueue.Priority.VeryHigh, targetPos: new Vector3(Center.X, 0, Center.Z));
    }
}

class AlphinaudAI(ModuleBase module) : QuestBattle.RotationModule<AutoAlphi>(module);

class HostileSkyArmorStates : StateMachineBuilder
{
    public HostileSkyArmorStates(ModuleBase module) : base(module)
    {
        TrivialPhase()

            .ActivateOnEnter<AlphinaudAI>()
            .ActivateOnEnter<LB>()
            .ActivateOnEnter<TerminusEst>()
            .Raw.Update = () => module.World.CurrentCFCID != 582;
    }
}

[ModuleInfo(CFCID = 68612u, NameID = 7257u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class HostileSkyArmor(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, new ArenaBoundsCircle(20f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}

