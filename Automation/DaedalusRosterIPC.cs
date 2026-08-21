using System;
using Dalamud.Plugin.Ipc;

namespace Minerva.Automation;

/// <summary>
/// Fills <see cref="PartyRolesConfig"/> from Daedalus's LAN roster, so role-based mechanics resolve
/// without anyone assigning roles by hand.
/// <para>
/// Minerva inherited BossmodReborn's role-CONSUMING half — every component's <c>AddAIHints</c> takes an
/// <see cref="PartyRolesConfig.Assignment"/> — but none of its producing half: no auto-assign, no
/// priority tables, no config UI. Nothing wrote <c>Assignments</c>, so every member resolved to
/// <see cref="PartyRolesConfig.Assignment.Unassigned"/> and anything keyed on a role (tower soaks,
/// tether pairs, "H2 takes the west tower") silently did nothing.
/// </para>
/// <para>
/// Daedalus is the one plugin that can answer it: it runs the rotation for every toon, so it knows each
/// job, and its roster spans machines, so a multibox fleet resolves as one party. It publishes the eight
/// standard slots already ordered by BossMod's own rules — Warrior over Paladin for main tank, White Mage
/// over Sage for H1 — so a box also running BossMod agrees with us rather than fighting us.
/// </para>
/// <para>
/// Everything is guarded: the gate throws when Daedalus is absent, so presence is probed on a timer and
/// a missing plugin simply leaves every member unassigned — exactly the behaviour before this existed.
/// </para>
/// </summary>
internal sealed class DaedalusRosterIPC
{
    /// <summary>Daedalus heartbeats the roster about every 2s, so re-reading faster only burns JSON.</summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly ICallGateSubscriber<string> getRosterJson;
    private DateTime lastPoll;
    private bool everLogged;
    private int lastAssignedCount = -1;

    /// <summary>The roles as last read. Empty until Daedalus answers, which is the safe default.</summary>
    public PartyRolesConfig Roles { get; } = new();

    public DaedalusRosterIPC()
        => this.getRosterJson = Service.PluginInterface.GetIpcSubscriber<string>("Daedalus.Party.GetRosterJson");

    /// <summary>Re-read the roster if it is time. Cheap and safe to call every frame.</summary>
    public void Update(DateTime now)
    {
        if (now - this.lastPoll < PollInterval)
            return;

        this.lastPoll = now;

        string json;
        try
        {
            json = this.getRosterJson.InvokeFunc();
        }
        catch
        {
            // Daedalus not loaded. Keep whatever we had rather than clearing: a roster that blinks
            // out for one poll must not drop the party's roles mid-pull.
            return;
        }

        try
        {
            this.Parse(json);
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, "Daedalus roster: could not parse, leaving roles as they were.");
        }
    }

    private void Parse(string json)
    {
        var parsed = PartyRoleRoster.Parse(json);

        this.Roles.Assignments.Clear();
        foreach (var (contentId, assignment) in parsed)
            this.Roles.Assignments[contentId] = assignment;

        if (this.Roles.Assignments.Count != this.lastAssignedCount || !this.everLogged)
        {
            this.lastAssignedCount = this.Roles.Assignments.Count;
            this.everLogged = true;
            Service.Log.Information($"Daedalus roster: {this.Roles.Assignments.Count} party role(s) assigned.");
        }
    }

    /// <summary>
    /// The role of the actor the hints are being built for. Resolved through the PARTY SLOT rather than
    /// the actor, because the assignment is keyed on content id and only the party list carries it.
    /// </summary>
    public PartyRolesConfig.Assignment AssignmentFor(PartyState party, Actor? actor)
    {
        if (actor == null || this.Roles.Assignments.Count == 0)
            return PartyRolesConfig.Assignment.Unassigned;

        for (var i = 0; i < PartyState.MaxSlots; ++i)
        {
            if (party.Slots[i].InstanceID == actor.InstanceID && party.Slots[i].ContentID != 0)
                return this.Roles[party.Slots[i].ContentID];
        }

        return PartyRolesConfig.Assignment.Unassigned;
    }
}
