// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS2StygimolochWarrior;

sealed class ViciousSwipe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ViciousSwipe, 15f);
sealed class CrazedRampage(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.CrazedRampage, 13f);
sealed class Coerce(ModuleBase module) : Components.StatusDrivenForcedMarch(module, 4f, (uint)SID.ForwardMarch, (uint)SID.AboutFace, (uint)SID.LeftFace, (uint)SID.RightFace);

[ModuleInfo(CFCID = 761u, NameID = 9754u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class DRS2StygimolochWarrior(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-160f, 78f), new ArenaBoundsSquare(17.5f));
