using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Minerva.Validate;

/// <summary>
/// Loads BossmodReborn out of game so its boss-module logic can be driven against a Minerva recording
/// (see docs/dual-viewer-plan.md). Everything here is reflection: BMR is loaded by path and never
/// referenced, so this project has no compile-time dependency on it or on Dalamud.
/// <para>
/// Three things make headless loading work:
/// <list type="number">
/// <item>Dalamud's assemblies are resolved from the launcher's dev folder, so BMR's types load even
/// though no game is running.</item>
/// <item><c>BossMod.Service.Config</c> is a plain <c>ConfigRoot</c> (no Dalamud), but it must be
/// <c>Initialize()</c>d before any module is built — <c>MiniArena</c> has a static initializer that
/// reads <c>Service.Config.Get&lt;BossModuleConfig&gt;()</c>, and every <c>BossModule</c> ctor builds a
/// <c>MiniArena</c>.</item>
/// <item>We only ever read component *data* (<c>ActiveAOEs</c>), never call BMR's <c>Draw*</c>, so ImGui
/// is never touched.</item>
/// </list>
/// </para>
/// </summary>
internal sealed class BmrHost
{
    private readonly Assembly asm;
    private readonly Type registryType;

    private BmrHost(Assembly asm, Type registryType)
    {
        this.asm = asm;
        this.registryType = registryType;
    }

    public string Location => this.asm.Location;

    /// <summary>The loaded BossmodReborn assembly, for the op bridge.</summary>
    public Assembly Assembly => this.asm;

    /// <summary>
    /// Build the module BMR registers for this actor's OID, bound to a live WorldState (unlike
    /// <see cref="CreateModuleForOID"/>, which uses a throwaway one). Minimum maturity is the lowest
    /// BMR defines, so WIP modules are included — we want to compare against those too.
    /// </summary>
    public object? CreateModuleForActor(object worldState, object primaryActor)
    {
        var maturityType = this.asm.GetType("BossMod.BossModuleInfo+Maturity")!;
        var minMaturity = Enum.GetValues(maturityType).Cast<object>().First(); // lowest declared value
        return this.registryType.GetMethod("CreateModuleForActor", BindingFlags.Public | BindingFlags.Static)!
                   .Invoke(null, [worldState, primaryActor, minMaturity]);
    }

    /// <summary>Boss modules BMR has registered (keyed by primary actor OID).</summary>
    public int ModuleCount => ((IDictionary)this.registryType
        .GetField("RegisteredModules", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!).Count;

    /// <summary>
    /// Build a BMR module for a primary-actor OID, using BMR's own timeline factory — it supplies a
    /// throwaway WorldState and dummy primary actor, which is exactly what a construction probe needs.
    /// Returns the module instance, or null when BMR has no module for that OID.
    /// </summary>
    public object? CreateModuleForOID(uint oid)
        => this.registryType.GetMethod("CreateModuleForTimeline", BindingFlags.Public | BindingFlags.Static)!
               .Invoke(null, [oid]);

    /// <summary>Registry metadata for an OID: (module type name, group id, name id), or null.</summary>
    public (string Type, uint GroupID, uint NameID)? InfoForOID(uint oid)
    {
        var info = this.registryType.GetMethod("FindByOID", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [oid]);
        if (info == null)
            return null;
        var t = info.GetType();
        var moduleType = (Type)t.GetField("ModuleType")!.GetValue(info)!;
        return (moduleType.FullName ?? moduleType.Name,
                (uint)t.GetField("GroupID")!.GetValue(info)!,
                (uint)t.GetField("NameID")!.GetValue(info)!);
    }

    /// <summary>All registered primary-actor OIDs, ascending.</summary>
    public uint[] RegisteredOIDs()
    {
        var dict = (IDictionary)this.registryType
            .GetField("RegisteredModules", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        return dict.Keys.Cast<uint>().OrderBy(o => o).ToArray();
    }

    /// <summary>
    /// Load BMR and prepare it for headless use. <paramref name="bmrPath"/> and
    /// <paramref name="dalamudDir"/> default to the XIVLauncher install locations.
    /// </summary>
    public static BmrHost Load(string? bmrPath = null, string? dalamudDir = null, string? gameDataDir = null)
    {
        bmrPath = bmrPath ?? FindBmrAssembly() ?? throw new FileNotFoundException(
            "could not find BossModReborn.dll — pass --bmr <path>. Looked under " +
            "%AppData%\\XIVLauncher\\installedPlugins\\BossModReborn.");
        dalamudDir ??= FindDalamudDir();

        var probeDirs = new[] { Path.GetDirectoryName(Path.GetFullPath(bmrPath))!, dalamudDir }
            .Where(d => d != null && Directory.Exists(d)).Select(d => d!).ToArray();

        // BMR's types reference Dalamud/Lumina/FFXIVClientStructs; satisfy them from the launcher's
        // dev folder. Returning null lets the runtime fall through to its normal failure for anything
        // we can't find, which surfaces as a readable exception rather than a silent misload.
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var name = new AssemblyName(args.Name).Name;
            if (name == null)
                return null;
            foreach (var dir in probeDirs)
            {
                var candidate = Path.Combine(dir, name + ".dll");
                if (File.Exists(candidate))
                {
                    try { return Assembly.LoadFrom(candidate); }
                    catch { /* keep probing */ }
                }
            }
            return null;
        };

        var asm = Assembly.LoadFrom(Path.GetFullPath(bmrPath));

        // MUST run before any BossModule is constructed (see class remarks).
        var service = asm.GetType("BossMod.Service")
            ?? throw new InvalidOperationException($"{Path.GetFileName(bmrPath)} has no BossMod.Service — is this really BossmodReborn?");
        var config = service.GetField("Config", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? throw new InvalidOperationException("BossMod.Service.Config was null; BMR's layout has changed.");
        config.GetType().GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)!.Invoke(config, null);

        // BMR's ActionDefinitions reads Excel sheets in its type initializer, and BossModule construction
        // touches it — so Lumina must be live before any module is built, or the ctor dies with an NRE
        // inside a TypeInitializationException.
        InitLumina(asm, service, gameDataDir);

        var registry = asm.GetType("BossMod.BossModuleRegistry")
            ?? throw new InvalidOperationException("BossMod.BossModuleRegistry not found; BMR's layout has changed.");
        RuntimeHelpersRunStaticCtor(registry); // populates RegisteredModules

        return new BmrHost(asm, registry);
    }

    /// <summary>
    /// Point BMR's <c>Service.LuminaGameData</c> at the game's sqpack. Lumina is loaded out of the same
    /// Dalamud dev folder as everything else, so this needs no compile-time reference.
    /// </summary>
    private static void InitLumina(Assembly bmr, Type service, string? gameDataDir)
    {
        var field = service.GetField("LuminaGameData", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("BossMod.Service.LuminaGameData not found; BMR's layout has changed.");

        gameDataDir ??= FindGameDataDir()
            ?? throw new DirectoryNotFoundException(
                @"could not find the game's sqpack folder — pass --game <path to ...\game\sqpack>. " +
                "BMR needs it because ActionDefinitions reads Excel sheets when a module is constructed.");

        var luminaType = field.FieldType; // Lumina.GameData

        // Lumina's ctor has picked up optional parameters over the years (options, logger...). Bind to
        // whichever overload starts with the data path and fill the rest with their defaults, so a Lumina
        // update doesn't break the host.
        var ctor = luminaType.GetConstructors()
            .Where(c => c.GetParameters() is { Length: > 0 } ps && ps[0].ParameterType == typeof(string))
            .OrderBy(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"no Lumina.GameData ctor takes a data path. Available: " +
                string.Join(" | ", luminaType.GetConstructors().Select(c =>
                    "(" + string.Join(", ", c.GetParameters().Select(pp => pp.ParameterType.Name)) + ")")));

        var ps = ctor.GetParameters();
        var argv = new object?[ps.Length];
        argv[0] = gameDataDir;
        for (var i = 1; i < ps.Length; ++i)
            argv[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue
                : ps[i].ParameterType.IsValueType ? Activator.CreateInstance(ps[i].ParameterType)
                : null;

        field.SetValue(null, ctor.Invoke(argv));
    }

    /// <summary>The game's sqpack directory, from the XIVLauncher config or the usual install spots.</summary>
    private static string? FindGameDataDir()
    {
        foreach (var candidate in new[]
        {
            @"C:\game\sqpack",
            @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack",
            @"C:\Program Files\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack",
        })
        {
            if (Directory.Exists(candidate))
                return candidate;
        }
        return null;
    }

    private static void RuntimeHelpersRunStaticCtor(Type t)
        => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);

    /// <summary>Newest installed BossModReborn build, or null.</summary>
    private static string? FindBmrAssembly()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher", "installedPlugins", "BossModReborn");
        if (!Directory.Exists(root))
            return null;
        return Directory.GetDirectories(root)
            .OrderByDescending(d => d) // version-named folders; newest last alphabetically
            .Select(d => Path.Combine(d, "BossModReborn.dll"))
            .FirstOrDefault(File.Exists);
    }

    private static string? FindDalamudDir()
    {
        var dev = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher", "addon", "Hooks", "dev");
        return Directory.Exists(dev) ? dev : null;
    }
}
