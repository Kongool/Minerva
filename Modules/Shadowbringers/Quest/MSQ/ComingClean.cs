// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.ComingClean; // quest name: Full Steam Ahead

public enum OID : uint
{
    Boss = 0x295C, // R1.0
    Ranjit = 0x295D, // R1.0
    SerpentHead = 0x295F, // R1.0
    LightningVoidzone = 0x1E9685, // R0.5
    Helper = 0x233C, // R0.5
}

public enum AID : uint
{
    ShatteredSky = 16405, // Boss->self, 5.0s cast, single-target
    ShatteredSky1 = 16429, // 233C->self, 6.0s cast, range 45 circle
    HotPursuit = 16406, // Boss->self, 3.0s cast, single-target
    HotPursuit1 = 16430, // 233C->location, 3.0s cast, range 5 circle
    NexusOfThunderVisual = 16404, // Boss->self, 3.0s cast, single-target
    NexusOfThunder = 16427, // 233C->self, 7.0s cast, range 60+R width 5 rect
    Wrath = 16425, // 295E->self, no cast, range 100 circle
    CoiledLevin = 16424, // 295E->self, 3.0s cast, single-target
    CoiledLevin1 = 16428, // 233C->self, 7.0s cast, range 6 circle
    UnbridledWrath = 16426, // 295E->self, no cast, range 100 circle
    HiddenCurrent = 16403, // Boss->location, no cast, ???
    VeilOfGukumatz1 = 16423, // 2998->self, no cast, single-target
    VeilOfGukumatz2 = 16422, // 295D->self, no cast, single-target
    VeilOfGukumatz3 = 16402, // Boss->self, no cast, single-target
    UnceremoniousBeheading = 16412, // 295D->self, 3.5s cast, range 10 circle
    HiddenCurrent1 = 16411, // 295D->location, no cast, ???
    MercilessLeftVisual = 16415, // 295D->self, 4.0s cast, single-target
    MercilessLeft = 33202, // 233C->self, 4.0s cast, range 40 120-degree cone
    MercilessRight = 16431, // 233C->self, 4.0s cast, range 40 120-degree cone
    KatunCycle = 16413, // 295D->self, 5.5s cast, range 5-40 donut
    HotPursuit2 = 16410, // 295D->self, 3.0s cast, single-target
    AgelessSerpent = 16417, // 295D->self, no cast, single-target
    SerpentRising = 16433, // 295F->self, no cast, single-target
    Evisceration = 16419, // 295D->self, 2.0s cast, range 40 120-degree cone
    Spiritcall = 16420, // 295D->self, no cast, range 100 circle
    SnakingFlame = 16432, // 295F->player, 40.0s cast, width 4 rect charge
}

public enum SID : uint
{
    Smackdown = 2068,
    DownForTheCount = 783 // 295E->player, extra=0xEC7
}

class KatunCycle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.KatunCycle, new AOEShapeDonut(5f, 40f));

class MercilessEvisceration(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.MercilessLeft, (uint)AID.MercilessRight, (uint)AID.Evisceration], new AOEShapeCone(40f, 60f.Degrees()));
class UnceremoniousBeheading(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.UnceremoniousBeheading, 10f);
class HotPursuit(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HotPursuit1, 5f);
class NexusOfThunder(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.NexusOfThunder, new AOEShapeRect(60f, 2.5f));
class CoiledLevin(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CoiledLevin1, 6f);
class LightningVoidzone(ModuleBase module) : Components.Voidzone(module, 6f, m => m.Enemies((uint)OID.LightningVoidzone).Where(x => x.EventState != 7));

class RanjitStates : StateMachineBuilder
{
    public RanjitStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HotPursuit>()

            .ActivateOnEnter<NexusOfThunder>()
            .ActivateOnEnter<CoiledLevin>()
            .ActivateOnEnter<LightningVoidzone>()
            .ActivateOnEnter<KatunCycle>()
            .ActivateOnEnter<MercilessEvisceration>()
            .ActivateOnEnter<UnceremoniousBeheading>()
            .Raw.Update = () => module.Enemies((uint)OID.Ranjit) is var boss && boss.Count != 0 && boss[0].FindStatus((uint)SID.DownForTheCount) != null || module.World.CurrentCFCID != 680;
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69155u, NameID = 8374u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ranjit(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-203f, 395f), 19.5f, 20)]);

    protected override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Ranjit));
        Arena.Actor(PrimaryActor);
    }
}
