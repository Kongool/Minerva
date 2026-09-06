// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T07Byakko;

class StormPulse(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.StormPulse);
class HeavenlyStrike(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.HeavenlyStrike);
class HeavenlyStrikeSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.HeavenlyStrike, 3f);
class SweepTheLeg1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SweepTheLeg1, new AOEShapeCone(28.5f, 135f.Degrees()));
class SweepTheLeg3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SweepTheLeg3, new AOEShapeDonut(5f, 30f));
class TheRoarOfThunder(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TheRoarOfThunder);
class ImperialGuard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ImperialGuard, new AOEShapeRect(44.75f, 2.5f));
class FireAndLightning(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.FireAndLightning1, (uint)AID.FireAndLightning2], new AOEShapeRect(50f, 10f));

class DistantClap(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DistantClap, new AOEShapeDonut(5f, 3f));

class HighestStakes(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.Stackmarker, (uint)AID.HighestStakes2, 6f, 5f, 7, 7);

class AratamaForce(ModuleBase module) : Components.Voidzone(module, 2f, GetVoidzones, 2)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.AratamaForce);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (!z.IsDead)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

class HundredfoldHavoc(ModuleBase module) : Components.Exaflare(module, 5f)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HundredfoldHavocFirst)
        {
            Lines.Add(new(caster.Position, 5f * caster.Rotation.ToDirection(), Module.CastFinishAt(spell), 1d, 10, 2));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.HundredfoldHavocFirst or (uint)AID.HundredfoldHavocRest)
        {
            var count = Lines.Count;
            var pos = caster.Position;
            for (var i = 0; i < count; ++i)
            {
                var line = Lines[i];
                if (line.Next.AlmostEqual(pos, 1f))
                {
                    AdvanceLine(line, pos);
                    if (line.ExplosionsLeft == 0)
                        Lines.RemoveAt(i);
                    return;
                }
            }
        }
    }
}

[ModuleInfo(CFCID = 290u, NameID = 6221u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class T07Byakko(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, new ArenaBoundsCircle(20f));
