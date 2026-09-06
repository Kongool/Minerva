// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;


namespace Minerva.Dawntrail.Quantum.Q1FinalVerse;

sealed class AutoPotionModule(RotationModuleManager manager, Actor player) : RotationModule(manager, player)
{
    public enum Track { Potion }
    public enum PotionStrategy { None, Use }

    public static RotationModuleDefinition Definition()
    {
        var res = new RotationModuleDefinition("Auto-Potion", "Automatically use Pilgrim's Potions", "Encounter AI", "xan", RotationModuleQuality.WIP, new(~1ul), 100, 1, RotationModuleOrder.Actions, typeof(Q1FinalVerse));

        res.Define(Track.Potion).As<PotionStrategy>("Potion", "Potion")
            .AddOption(PotionStrategy.None, "Don't use")
            .AddOption(PotionStrategy.Use, "Use potion ASAP");

        return res;
    }

    public override void Execute(StrategyValues strategy, Actor? primaryTarget, float estimatedAnimLockDelay, bool isMoving)
    {
        var opt = strategy.Option(Track.Potion);
        if (opt.As<PotionStrategy>() == PotionStrategy.Use && RegenLeft() < 2)
            Hints.ActionsToExecute.Push(ActionDefinitions.IDPotionPilgrim, Player, opt.Priority(ActionQueue.Priority.Medium));
    }

    private float RegenLeft() => Player.FindStatus(648u) is ActorStatus stat ? StatusDuration(stat.ExpireAt) : default;
}
