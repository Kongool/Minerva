// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex5Necron;

sealed class Wipe(ModuleBase module) : ModuleComponent(module)
{
    public bool Wiped;
    public override bool KeepOnPhaseChange => true;

    public override void OnEventDirectorUpdate(uint updateID, uint param1, uint param2, uint param3, uint param4)
    {
        if (updateID == 0x80000029)
        {
            Wiped = true;
        }
    }
}

sealed class Intermission(ModuleBase module) : ModuleComponent(module)
{
    public bool Started;

    public override void OnEventDirectorUpdate(uint updateID, uint param1, uint param2, uint param3, uint param4)
    {
        if (param2 == default && updateID == 0x8000000C)
        {
            Started = true;
        }
    }
}

sealed class BlueShockwave(ModuleBase module) : Components.TankSwap(module, (uint)AID.BlueShockwaveVisual1, (uint)AID.BlueShockwave, (uint)AID.BlueShockwave, 1.2d, 5.2d, new AOEShapeCone(100f, 50f.Degrees()));
sealed class ChokingGrasp(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.ChokingGraspAOE1, (uint)AID.ChokingGraspAOE2], Rect)
{
    public static readonly AOEShapeRect Rect = new(24f, 3f);
}

sealed class CircleOfLives(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CircleOfLives, new AOEShapeDonut(3f, 50f), 1);

[ModuleInfo(CFCID = 1062u, NameID = 14093u, PrimaryActorOID = (uint)OID.Necron, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class Ex5Necron(WorldState ws, Actor primary) : Trial.T05Necron.Necron(ws, primary);
