// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.DeathUntoDawn.P2;

public enum OID : uint
{
    Boss = 0x3200,
    Fetters = 0x3218
}

public enum AID : uint
{
    LunarGungnir = 24025, // LunarOdin->31EC, 12.0s cast, range 6 circle
    LunarGungnir1 = 24026, // LunarOdin->2E2E, 25.0s cast, range 6 circle
    GungnirAOE = 24698, // 233C->self, 10.0s cast, range 10 circle
    Gagnrath = 24030, // 321C->self, 3.0s cast, range 50 width 4 rect
    GungnirSpread = 24029, // 321C->self, no cast, range 10 circle
    LeftZantetsuken = 24034, // LunarOdin->self, 4.0s cast, range 70 width 39 rect
    RightZantetsuken = 24032, // LunarOdin->self, 4.0s cast, range 70 width 39 rect
}

class Fetters(ModuleBase module) : Components.Adds(module, (uint)OID.Fetters);

class GunmetalSoul(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(4f, 100f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var enemies = Module.Enemies(0x1EB1D5);
        var count = 0;
        var countE = enemies.Count;
        for (var i = 0; i < countE; ++i)
        {
            if (enemies[i].EventState != 7)
                ++count;
        }

        if (count == 0)
            return [];

        var aoes = new AOEInstance[count];
        var index = 0;

        for (var i = 0; i < countE; ++i)
        {
            var e = enemies[i];
            if (e.EventState != 7)
                aoes[index++] = new(donut, e.Position);
        }
        return aoes;
    }
}
class LunarGungnir1(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.LunarGungnir, 6f);
class LunarGungnir2(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.LunarGungnir1, 6f);
class Gungnir(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GungnirAOE, 10f);
class Gagnrath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Gagnrath, new AOEShapeRect(50f, 2f));
class GungnirSpread(ModuleBase module) : Components.BaitAwayIcon(module, 10f, 189u, (uint)AID.GungnirSpread, 5.3f);
class Zantetsuken(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.RightZantetsuken, (uint)AID.LeftZantetsuken], new AOEShapeRect(70f, 19.5f));

public class LunarOdinStates : StateMachineBuilder
{
    public LunarOdinStates(ModuleBase module) : base(module)
    {
        TrivialPhase()

            .ActivateOnEnter<Fetters>()
            .ActivateOnEnter<Gungnir>()
            .ActivateOnEnter<Gagnrath>()
            .ActivateOnEnter<GungnirSpread>()
            .ActivateOnEnter<GunmetalSoul>()
            .ActivateOnEnter<LunarGungnir1>()
            .ActivateOnEnter<LunarGungnir2>()
            .ActivateOnEnter<Zantetsuken>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69602u, NameID = 10034u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class LunarOdin(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(146.5f, 84.5f), new ArenaBoundsCircle(20f))
{
    protected override bool CheckPull() => Raid.Player()!.InCombat;
}
