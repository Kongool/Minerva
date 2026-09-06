// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Quest.MSQ.OneLifeForOneWorld;

public enum OID : uint
{
    Boss = 0x17CD,
    KnightOfDarkness = 0x17CE, // R0.500, x1
    BladeOfLight = 0x1EA19E,
}

public enum AID : uint
{
    Overpower = 6683, // Boss->self, 2.5s cast, range 6+R 90-degree cone
    UnlitCyclone = 6684, // Boss->self, 4.0s cast, range 5+R circle
    UnlitCycloneAdds = 6685, // 18D6->location, 4.0s cast, range 9 circle
    Skydrive = 6686, // Boss->player, 5.0s cast, single-target
    UtterDestruction = 6690, // FirstWard->self, 3.0s cast, range 20+R circle
    RollingBladeCircle = 6691, // Boss->self, 3.0s cast, range 7 circle
    RollingBladeCone = 6692 // FirstWard->self, 3.0s cast, range 60+R 30-degree cone
}

public enum SID : uint
{
    Invincibility = 325 // KnightOfDarkness->Boss/_Gen_FirstWard, extra=0x0
}

class Overpower(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Overpower, new AOEShapeCone(7f, 45f.Degrees()));
class UnlitCyclone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.UnlitCyclone, 6f);
class UnlitCycloneAdds(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.UnlitCycloneAdds, 9f);

class Skydrive(ModuleBase module) : Components.BaitAwayIcon(module, 5f, 23u, (uint)AID.Skydrive);
class SkydrivePuddle(ModuleBase module) : Components.Voidzone(module, 5f, m => m.Enemies(0x1EA19Cu).Where(x => x.EventState != 7));
class RollingBlade(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RollingBladeCircle, 7f);
class RollingBladeCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RollingBladeCone, new AOEShapeCone(60f, 15f.Degrees()));

class BladeOfLight(ModuleBase module) : ModuleComponent(module)
{
    public Actor? Blade => World.Actors.FirstOrDefault(x => x.OID == 0x1EA19Eu && x.IsTargetable);

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (Blade != null)
            Arena.Actor(Blade, Colors.Vulnerable);
    }
}

class Adds(ModuleBase module) : Components.AddsMulti(module, [0x17CEu, 0x17CFu, 0x17D0u, 0x17D1u]);
class TargetPriorityHandler(ModuleBase module) : ModuleComponent(module)
{
    private Actor? Knight => Module.Enemies((uint)OID.KnightOfDarkness).FirstOrDefault();
    private Actor? Covered => World.Actors.FirstOrDefault(s => s.OID != 0x18D6u && s.FindStatus((uint)SID.Invincibility) != null);
    private Actor? BladeOfLight => World.Actors.FirstOrDefault(s => s.OID == (uint)OID.BladeOfLight && s.IsTargetable);

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (Knight != null && Covered != null)
            Arena.AddLine(Knight.Position, Covered.Position, Colors.Danger, 1);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (BladeOfLight != null)
        {
            var playerIsAttacked = false;

            for (var i = 0; i < hints.PotentialTargets.Count; ++i)
            {
                var e = hints.PotentialTargets[i];
                if (e.Actor.TargetID == actor.InstanceID)
                {
                    playerIsAttacked = true;
                    e.Priority = 0;
                }
                else
                {
                    e.Priority = AIHints.Enemy.PriorityUndesirable;
                }
            }

            if (!playerIsAttacked)
            {
                if (actor.DistanceToHitbox(BladeOfLight) > 5.5f)
                    hints.ForcedMovement = BladeOfLight!.Position - actor.Position;
                else
                    hints.InteractWithTarget = BladeOfLight;
            }
        }
        else
        {
            for (var i = 0; i < hints.PotentialTargets.Count; ++i)
            {
                var e = hints.PotentialTargets[i];
                if (e.Actor == Knight)
                    e.Priority = 2;
                else if (e.Actor == Covered)
                    e.Priority = 0;
                else
                    e.Priority = 1;
            }
        }
    }
}

class UtterDestruction(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.UtterDestruction, new AOEShapeDonut(10f, 20f));

class WarriorOfDarknessStates : StateMachineBuilder
{
    public WarriorOfDarknessStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Overpower>()
            .ActivateOnEnter<Adds>()
            .ActivateOnEnter<TargetPriorityHandler>()
            .ActivateOnEnter<UnlitCyclone>()
            .ActivateOnEnter<UnlitCycloneAdds>()
            .ActivateOnEnter<Skydrive>()
            .ActivateOnEnter<SkydrivePuddle>()
            .ActivateOnEnter<BladeOfLight>()
            .ActivateOnEnter<RollingBlade>()
            .ActivateOnEnter<RollingBladeCone>()
            .ActivateOnEnter<UtterDestruction>();
    }
}

[ModuleInfo(CFCID = 67885u, NameID = 5240u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class WarriorOfDarkness(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(default, 19.5f, 36)]);
}
