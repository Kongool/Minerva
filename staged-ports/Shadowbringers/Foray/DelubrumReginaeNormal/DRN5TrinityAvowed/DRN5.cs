// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN5TrinityAvowed;

sealed class WrathOfBozja(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.WrathOfBozja, new AOEShapeCone(60f, 45f.Degrees()), tankbuster: true);
sealed class GleamingArrow(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GleamingArrow, new AOEShapeRect(60f, 5f));
sealed class GloryOfBozja(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.GloryOfBozja);

[ModuleInfo(CFCID = 760u, NameID = 9853u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class DRN5TrinityAvowed(WorldState ws, Actor primary) : TrinityAvowed(ws, primary);
