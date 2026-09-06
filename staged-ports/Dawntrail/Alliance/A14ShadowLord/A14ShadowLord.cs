// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A14ShadowLord;

sealed class Teleport(ModuleBase module) : Components.CastCounter(module, (uint)AID.Teleport);
sealed class TeraSlash(ModuleBase module) : Components.CastCounter(module, (uint)AID.TeraSlash);
sealed class DoomArc(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DoomArc);
sealed class UnbridledRage(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeRect(100f, 4f), (uint)IconID.UnbridledRage, (uint)AID.UnbridledRageAOE, 5.9d);
sealed class DarkNova(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.DarkNova, 6f);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1015u, CFCID = 1015u, NameID = 13653u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class A14ShadowLord(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(150f, 800f), new ArenaBoundsCircle(30f));