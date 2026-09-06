// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.Job.Monk.ThePowerToProtect;

public enum OID : uint
{
    Boss = 0x1BCB, // R5.400, x1
    CorpseBrigadeKnuckledancer = 0x1C0C, // R0.500, x2 (spawn during fight)
    CorpseBrigadeBowdancer = 0x1C0D, // R0.500, x2 (spawn during fight)
    HeweraldIronaxe = 0x1C01, // R0.500, x1
    CorpseBrigadeFiredancer = 0x1C00, // R0.500, x0 (spawn during fight)
    CorpseBrigadeBowdancer1 = 0x1BFF, // R0.500, x0 (spawn during fight)
    CorpseBrigadeKnuckledancer1 = 0x1BFE, // R0.500, x0 (spawn during fight)
    CorpseBrigadeBarber = 0x1BFD, // R0.500, x0 (spawn during fight)
    SalvagedSlasher = 0x1C1F, // R1.050, x0 (spawn during fight)
    CorpseBrigadeVanguard = 0x1C02, // R2.000, x0 (spawn during fight)
    FireII = 0x1EA4C6,
}

public enum AID : uint
{
    IronTempest = 1003, // HeweraldIronaxe->self, 3.5s cast, range 5+R circle
    FireII = 2175, // CorpseBrigadeFiredancer->location, 2.5s cast, range 5 circle
    Overpower = 720, // HeweraldIronaxe->self, 2.5s cast, range 6+R 90-degree cone
    Rive = 1135, // HeweraldIronaxe->self, 2.5s cast, range 30+R width 2 rect
    DiffractiveLaser = 8348, // Boss->location, 4.0s cast, range 5 circle
}

public enum SID : uint
{
    ExtremeCaution = 1269 // Boss->player, extra=0x0

}

class ExtremeCaution(ModuleBase module) : Components.StayMove(module)
{
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.ExtremeCaution && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            PlayerStates[slot] = new(Requirement.Stay, status.ExpireAt);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.ExtremeCaution && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            PlayerStates[slot] = default;
    }
}
class IronTempest(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IronTempest, 5.5f);
class FireII(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 5, (uint)AID.FireII, m => m.Enemies((uint)OID.FireII).Where(x => x.EventState != 7), 0);
class Overpower(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Overpower, new AOEShapeCone(6.5f, 45.Degrees()));
class Rive(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Rive, new AOEShapeRect(30.5f, 1));
class DiffractiveLaser(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DiffractiveLaser, 5);

class IoStates : StateMachineBuilder
{
    public IoStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<IronTempest>()
            .ActivateOnEnter<FireII>()
            .ActivateOnEnter<Overpower>()
            .ActivateOnEnter<Rive>()
            .ActivateOnEnter<DiffractiveLaser>()
            .ActivateOnEnter<ExtremeCaution>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 67966u, NameID = 5667u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Io(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    public static readonly ArenaBoundsCustom arena = new([new PolygonCustom([new(101.93f, -666.63f), new(94.49f, -639.63f), new(50.64f, -652.38f), new(57.58f, -679.32f)])]);

    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}

