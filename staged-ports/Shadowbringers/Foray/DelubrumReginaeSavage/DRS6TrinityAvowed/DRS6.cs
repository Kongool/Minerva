// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS6TrinityAvowed;

sealed class WrathOfBozja(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.WrathOfBozja, new AOEShapeCone(60f, 45f.Degrees()));
sealed class WrathOfBozjaBow(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.WrathOfBozjaBow, new AOEShapeCone(60f, 45f.Degrees()));

// note: it is combined with different AOEs (bow1, bow2, staff1)
class QuickMarch(ModuleBase module) : Components.StatusDrivenForcedMarch(module, 3f, (uint)SID.ForwardMarch, (uint)SID.AboutFace, (uint)SID.LeftFace, (uint)SID.RightFace);

sealed class GleamingArrow(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GleamingArrow, new AOEShapeRect(60f, 5f));

[ModuleInfo(CFCID = 761u, NameID = 9853u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class DRS6TrinityAvowed(WorldState ws, Actor primary) : TrinityAvowed(ws, primary);
