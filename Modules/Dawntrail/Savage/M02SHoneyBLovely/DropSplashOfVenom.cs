// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M02SHoneyBLovely;

sealed class DropSplashOfVenom(ModuleBase module) : Components.UniformStackSpread(module, 6f, 6f, 2, 2)
{
    public enum Mechanic { None, Pairs, Spread }

    public Mechanic NextMechanic;
    public DateTime Activation;

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (NextMechanic != Mechanic.None)
            hints.Add(NextMechanic.ToString());
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.SplashOfVenom:
            case (uint)AID.SpreadLove:
                NextMechanic = Mechanic.Spread;
                break;
            case (uint)AID.DropOfVenom:
            case (uint)AID.DropOfLove:
                NextMechanic = Mechanic.Pairs;
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.TemptingTwistAOE:
            case (uint)AID.HoneyBeelineAOE:
            case (uint)AID.TemptingTwistBeatAOE:
            case (uint)AID.HoneyBeelineBeatAOE:
                switch (NextMechanic)
                {
                    case Mechanic.Pairs:
                        // note: it's random whether dd or supports are hit, select supports arbitrarily
                        Activation = World.FutureTime(4.5f);
                        AddStacks(Raid.WithoutSlot(true, true, true).Where(p => p.Class.IsSupport()), Activation);
                        break;
                    case Mechanic.Spread:
                        Activation = World.FutureTime(4.5f);
                        AddSpreads(Raid.WithoutSlot(true, true, true), Activation);
                        break;
                }
                break;
            case (uint)AID.SplashOfVenomAOE:
            case (uint)AID.DropOfVenomAOE:
            case (uint)AID.SpreadLoveAOE:
            case (uint)AID.DropOfLoveAOE:
                Spreads.Clear();
                Stacks.Clear();
                NextMechanic = Mechanic.None;
                break;
        }
    }
}

abstract class Twist(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeDonut(7f, 30f));
sealed class TemptingTwist(ModuleBase module) : Twist(module, (uint)AID.TemptingTwistAOE);
sealed class TemptingTwistBeat(ModuleBase module) : Twist(module, (uint)AID.TemptingTwistBeatAOE);

abstract class Beeline(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(60f, 7f));
sealed class HoneyBeeline(ModuleBase module) : Beeline(module, (uint)AID.HoneyBeelineAOE);
sealed class HoneyBeelineBeat(ModuleBase module) : Beeline(module, (uint)AID.HoneyBeelineBeatAOE);

abstract class Splinter(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 8f);
sealed class PoisonCloudSplinter(ModuleBase module) : Splinter(module, (uint)AID.PoisonCloudSplinter);
sealed class SweetheartSplinter(ModuleBase module) : Splinter(module, (uint)AID.SweetheartSplinter);
