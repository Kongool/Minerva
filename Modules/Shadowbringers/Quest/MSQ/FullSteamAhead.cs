// Ported from Veyn's bossmod (awgil/ffxiv_bossmod, BSD-3; see THIRD-PARTY-NOTICES.txt). BossmodReborn has no
// module for this solo duty. The original also drives Thancred's rotation for the duty (RotationModule +
// UnmanagedRotation); that is dropped, since Minerva executes no actions.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.FullSteamAhead;

public enum OID : uint
{
    Boss = 0x295D, // Ranjit
    BossP1 = 0x295C,
    LightningVoidzone = 0x1E9685
}

public enum AID : uint
{
    ShatteredSky = 16405, // Boss->self, 5.0s cast, single-target
    ShatteredSky1 = 16429, // 233C->self, 6.0s cast, range 45 circle
    HotPursuit = 16406, // Boss->self, 3.0s cast, single-target
    HotPursuit1 = 16430, // 233C->location, 3.0s cast, range 5 circle
    NexusOfThunder = 16404, // Boss->self, 3.0s cast, single-target
    NexusOfThunder1 = 16427, // 233C->self, 7.0s cast, range 60+R width 5 rect
    Wrath = 16425, // 295E->self, no cast, range 100 circle
    CoiledLevin = 16424, // 295E->self, 3.0s cast, single-target
    CoiledLevin1 = 16428, // 233C->self, 7.0s cast, range 6 circle
    UnbridledWrath = 16426, // 295E->self, no cast, range 100 circle
    HiddenCurrent = 16403, // Boss->location, no cast, ???
    VeilOfGukumatz = 16423, // 2998->self, no cast, single-target
    VeilOfGukumatz1 = 16422, // 295D->self, no cast, single-target
    VeilOfGukumatz2 = 16402, // Boss->self, no cast, single-target
    UnceremoniousBeheading = 16412, // 295D->self, 3.5s cast, range 10 circle
    HiddenCurrent1 = 16411, // 295D->location, no cast, ???
    MercilessLeft = 16415, // 295D->self, 4.0s cast, single-target
    MercilessLeft1 = 33202, // 233C->self, 4.0s cast, range 40 120-degree cone
    MercilessRight = 16431, // 233C->self, 4.0s cast, range 40 120-degree cone
    KatunCycle = 16413, // 295D->self, 5.5s cast, range 5-40 donut
    HotPursuit2 = 16410, // 295D->self, 3.0s cast, single-target
    AgelessSerpent = 16417, // 295D->self, no cast, single-target
    SerpentRising = 16433, // 295F->self, no cast, single-target
    Evisceration = 16419, // 295D->self, 2.0s cast, range 40 120-degree cone
    Spiritcall = 16420, // 295D->self, no cast, range 100 circle
    SnakingFlame = 16432, // 295F->player, 40.0s cast, width 4 rect charge
}

class KatunCycle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.KatunCycle, new AOEShapeDonut(5f, 40f));
class MercilessLeft(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MercilessLeft1, new AOEShapeCone(40f, 60.Degrees()));
class MercilessRight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MercilessRight, new AOEShapeCone(40f, 60.Degrees()));
class UnceremoniousBeheading(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.UnceremoniousBeheading, new AOEShapeCircle(10f));
class Evisceration(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Evisceration, new AOEShapeCone(40f, 60.Degrees()));

class HotPursuit(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HotPursuit1, new AOEShapeCircle(5f));
class NexusOfThunder(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.NexusOfThunder1, new AOEShapeRect(60f, 2.5f));
class CoiledLevin(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CoiledLevin1, new AOEShapeCircle(6f));
class LightningVoidzone(ModuleBase module) : Components.Voidzone(module, 6f, m => m.Enemies((uint)OID.LightningVoidzone).Where(x => x.EventState != 7));

class FullSteamAheadStates : StateMachineBuilder
{
    public FullSteamAheadStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<HotPursuit>()
            .ActivateOnEnter<NexusOfThunder>()
            .ActivateOnEnter<CoiledLevin>()
            .ActivateOnEnter<LightningVoidzone>()
            .ActivateOnEnter<KatunCycle>()
            .ActivateOnEnter<MercilessLeft>()
            .ActivateOnEnter<MercilessRight>()
            .ActivateOnEnter<UnceremoniousBeheading>()
            .ActivateOnEnter<Evisceration>();
    }
}

// keyed like Minerva's other MSQ ports: the quest id sits in CFCID (see HANDOFF.md, quest activation)
[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69155u, NameID = 8374u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from Veyn's bossmod (awgil)")]
public class FullSteamAhead(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-203f, 395f), new ArenaBoundsCircle(19.5f))
{
    protected override bool CheckPull() => this.PrimaryActor.InCombat;

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        base.DrawEnemies(pcSlot, pc);
        this.Arena.Actors(this.Enemies((uint)OID.BossP1), Colors.Enemy);
    }
}
