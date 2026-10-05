using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Plugin.Ipc;

namespace Minerva.Automation;

/// <summary>
/// The navmesh's pathfinder, for quest battle scripts walking a solo duty (<see cref="QuestBattle.QuestBattle"/>):
/// Ariadne's <c>Nav.Pathfind</c>, else vnavmesh's. Separate from <see cref="NavmeshIPC"/>, which only ever hands a
/// follower points we already chose; this is the one place Minerva asks the mesh for a route.
/// </summary>
public sealed class QuestNavigation : QuestBattle.IQuestNavigation
{
    private readonly (string Name, ICallGateSubscriber<bool> Ready, ICallGateSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>?> Pathfind)[] backends;

    private readonly Func<NavmeshChoice> choice;

    public QuestNavigation(Func<NavmeshChoice> choice)
    {
        this.choice = choice;
        var pi = Service.PluginInterface;
        this.backends =
        [
            ("Ariadne", pi.GetIpcSubscriber<bool>("Ariadne.Nav.IsReady"), pi.GetIpcSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>?>("Ariadne.Nav.Pathfind")),
            ("vnavmesh", pi.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady"), pi.GetIpcSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>?>("vnavmesh.Nav.Pathfind")),
        ];
    }

    public bool IsReady => this.Ready() != null;

    public Task<List<Vector3>>? Pathfind(Vector3 from, Vector3 to)
    {
        var b = this.Ready();
        if (b == null)
            return null;
        try
        {
            return b.Value.Pathfind.InvokeFunc(from, to, false);
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, $"Minerva: {b.Value.Name} pathfind failed.");
            return null;
        }
    }

    private (string Name, ICallGateSubscriber<bool> Ready, ICallGateSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>?> Pathfind)? Ready()
    {
        var wanted = this.choice();
        foreach (var b in this.backends)
        {
            if (!NavmeshIPC.Allowed(b.Name, wanted))
                continue;
            try
            {
                if (b.Ready.InvokeFunc())
                    return b;
            }
            catch
            {
                // that plugin is not loaded -- try the next
            }
        }
        return null;
    }
}
