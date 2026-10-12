// Written from five recordings of five kills (Korha and Saar, 2026-10-11; 18s to 48s); BossmodReborn has no module
// for this dungeon. The boss dies fast at this item level, so little of the fight has been seen.
//
// What the recordings show: Attractant marks a player as Prey (status 420, tether 17). Three to eight seconds later
// (a Moldy Phlegm can come first) the boss Inhales that player (a stun) and two seconds after that Devours it (status
// 421: no actions, damage over time), and a Voracious Maw appears on the boss. Prey passes to any party member the marked player comes near, and whoever
// passed it on cannot be marked again (Slippery Prey, 475). Spore Cloud then poisons everyone still outside.
// Killing the Maw frees whoever is inside: in the fifth kill it died 4.7s after it appeared and the boss Spat Korha
// out in the same moment.
//
// Not handled:
//  - Not being eaten at all. The game's text for Prey says only that it can be passed on, and nobody in these pulls
//    tried anything with the Moldy Phlegm puddle.
//  - The puddle Moldy Phlegm leaves (0x1E8FC7). Nobody stood in one, so what it does is unknown and it is not drawn.
//  - The room. The rectangle is a guess from the door, where the boss stands and where people walked, with nothing
//    known about the walls.
//  - Dirty Sneeze (1847), cast twice, each time at one player. And on the sheet beside these but never cast:
//    Beatdown (1848), Spore Burst (1851, a 3s cast), Purulent Ooze (1852).
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D20LostCityOfAmdapor.D201DecayingGourmand;

public enum OID : uint
{
    DecayingGourmand = 0xA80, // R2.85
    VoraciousMaw = 0xA81, // R2.85, 2,873 HP, on top of the boss while someone is inside it
    MoldyPhlegm = 0x1E8FC7 // R0.5 event object, where Moldy Phlegm landed
}

public enum AID : uint
{
    AutoAttack = 870, // Decaying Gourmand->tank, no cast, single-target
    MoldyPhlegm = 941, // Decaying Gourmand->location, 2.2s cast, range 6 circle
    Attractant = 1845, // Decaying Gourmand->player, no cast: Prey
    Devour = 1846, // Decaying Gourmand->the prey, no cast
    DirtySneeze = 1847, // Decaying Gourmand->player, no cast, single-target
    SporeCloud = 1849, // Decaying Gourmand->self, no cast, everyone not devoured: Poison
    Spit = 1850, // Decaying Gourmand->self, no cast, when the Maw dies: whoever was inside is out
    Inhale = 1854 // Decaying Gourmand->the prey, no cast: Stun
}

public enum SID : uint
{
    Prey = 420,
    Devoured = 421,
    SlipperyPrey = 475
}

sealed class MoldyPhlegm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MoldyPhlegm, 6f);

sealed class VoraciousMaw(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        var maws = Module.Enemies((uint)OID.VoraciousMaw);
        for (var i = 0; i < maws.Count; ++i)
        {
            if (!maws[i].IsDeadOrDestroyed)
            {
                hints.Add("Kill the Voracious Maw!");
                return;
            }
        }
    }
}

sealed class D201DecayingGourmandStates : StateMachineBuilder
{
    public D201DecayingGourmandStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MoldyPhlegm>()
            .ActivateOnEnter<VoraciousMaw>();
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 22u, CFCID = 22u, NameID = 2556u, PrimaryActorOID = (uint)OID.DecayingGourmand, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva (from recordings)")]
// the door is at (98, 0), the boss waits at (98, -39.4), the coffer appears at (98.7, -48.5), and the party has walked
// x 83 to 107 and z -43 to -3
public sealed class D201DecayingGourmand(WorldState ws, Actor primary) : ModuleBase(ws, primary, new WPos(98f, -28f), new ArenaBoundsRect(17f, 28f))
{
    // Someone inside the Maw can do nothing, takes damage, and stays there until it dies. A rotation left to itself
    // stays on the boss: the third kill, 2026-10-11, Korha was inside for 12.8s, and with the Maw targeted by hand
    // only auto-attacks went into it (346 of its 2,873 HP) while every weaponskill went to the boss.
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.VoraciousMaw => 2,
                (uint)OID.DecayingGourmand => 1,
                _ => 0
            };
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.VoraciousMaw));
    }
}
