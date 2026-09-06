// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A11Prishe;

sealed class NullifyingDropkick(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.NullifyingDropkick, 6f);
sealed class Holy(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Holy, 6f);
sealed class BanishgaIV(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.BanishgaIV);
sealed class Banishga(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Banishga);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1015u, CFCID = 1015u, NameID = 13351u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class A11Prishe(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(800f, 400f), new ArenaBoundsSquare(35f));
