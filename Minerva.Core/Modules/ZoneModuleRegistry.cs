using System.Reflection;

namespace Minerva;

/// <summary>
/// Every <see cref="ZoneModule"/> in an assembly, indexed by content-finder condition.
///
/// <para>Separate from <see cref="ModuleRegistry"/> because the lookup is different in kind: a boss module
/// is chosen by CFC <i>and</i> a present actor, so a zone can hold dozens and the right one is whichever
/// boss you are standing in front of. A zone has exactly one zone module, chosen by CFC alone, and it is
/// up for as long as you are there.</para>
/// </summary>
public sealed class ZoneModuleRegistry
{
    public sealed record class Info(Type ModuleType, ZoneModuleInfoAttribute Attr);

    private readonly Dictionary<uint, Info> byCFC = [];

    /// <summary>Modules registered.</summary>
    public int Count => this.byCFC.Count;

    /// <summary>CFCs that had more than one zone module claim them; the first registered won.</summary>
    public List<string> Collisions { get; } = [];

    public static ZoneModuleRegistry Build(params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = [typeof(ZoneModuleRegistry).Assembly];

        var reg = new ZoneModuleRegistry();
        foreach (var asm in assemblies)
            foreach (var type in ModuleRegistry.LoadableTypes(asm))
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(ZoneModule)))
                    continue;
                var attr = type.GetCustomAttribute<ZoneModuleInfoAttribute>();
                if (attr == null || attr.CFCID == 0)
                    continue;

                if (reg.byCFC.TryGetValue(attr.CFCID, out var existing))
                {
                    // Recorded rather than thrown: a duplicate is a porting mistake, and losing the whole
                    // registry over one is worse than running with the first and saying so.
                    reg.Collisions.Add($"CFC {attr.CFCID}: {type.FullName} collides with {existing.ModuleType.FullName}");
                    continue;
                }
                reg.byCFC[attr.CFCID] = new Info(type, attr);
            }
        return reg;
    }

    /// <summary>The module for this zone, or null. Never returns a WIP module unless asked for one.</summary>
    public ZoneModule? Create(WorldState ws, uint cfcID, ModuleMaturity minMaturity = ModuleMaturity.WIP)
    {
        if (cfcID == 0 || !this.byCFC.TryGetValue(cfcID, out var info) || info.Attr.Minerva < minMaturity)
            return null;
        return (ZoneModule?)Activator.CreateInstance(info.ModuleType, ws);
    }

    /// <summary>Metadata for a zone, without building the module.</summary>
    public Info? For(uint cfcID) => cfcID != 0 && this.byCFC.TryGetValue(cfcID, out var info) ? info : null;
}
