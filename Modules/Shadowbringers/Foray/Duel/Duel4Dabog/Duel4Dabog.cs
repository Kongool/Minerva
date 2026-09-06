// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.Duel.Duel4Dabog;

abstract class RightArmBlaster(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(100, 3));
sealed class RightArmBlasterFragment(ModuleBase module) : RightArmBlaster(module, (uint)AID.RightArmBlasterFragment);
sealed class RightArmBlasterBoss(ModuleBase module) : RightArmBlaster(module, (uint)AID.RightArmBlasterBoss);

sealed class LeftArmSlash(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LeftArmSlash, new AOEShapeCone(10, 90.Degrees())); // TODO: verify angle
sealed class LeftArmWave(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LeftArmWaveAOE, 24);

[ModuleInfo(Group = ModuleGroup.BozjaDuel, CFCID = 778u, NameID = 19u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")] // bnpcname=9958
public sealed class Duel4Dabog(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(250f, 710f), new ArenaBoundsCircle(20f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Center, 20f);
}
