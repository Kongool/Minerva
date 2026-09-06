namespace Minerva;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Every <see cref="ConfigNode"/> in the assembly, one instance each, addressed by type. Ported in spirit
/// from BossmodReborn's <c>ConfigRoot</c> (BSD-3; see THIRD-PARTY-NOTICES.txt).
///
/// <para><b>Why a registry rather than one settings class.</b> Each raid module declares its own node next
/// to the fight it configures, so the strategy choices live with the code that reads them. The root
/// discovers them by reflection, which means a newly ported module's settings appear without anything
/// central being edited.</para>
///
/// <para><b>Why <see cref="Instance"/> is always usable.</b> Modules read their config in their
/// constructors. The offline replay validator and the tests build modules with no plugin and no config
/// file, so the root must hand out defaulted nodes rather than throw or return null — otherwise every
/// configurable module becomes unloadable outside the game, which is precisely where they get checked.
/// Loading a file layers saved values over those defaults; it never replaces the instances, so a node a
/// module already holds a reference to stays the node it reads from.</para>
/// </summary>
public sealed class ConfigRoot
{
    /// <summary>The process-wide root. The plugin points its own config file at this; everything else uses defaults.</summary>
    public static ConfigRoot Instance { get; } = new();

    private readonly Dictionary<Type, ConfigNode> nodes = [];

    /// <summary>Fired when any node changes, so the owner can persist.</summary>
    public event Action? Modified;

    public ConfigRoot() => this.Discover(typeof(ConfigRoot).Assembly);

    /// <summary>
    /// Register every concrete <see cref="ConfigNode"/> in an assembly. Modules live in the plugin assembly,
    /// not this one, so the plugin calls this a second time with its own.
    /// </summary>
    public void Discover(Assembly asm)
    {
        foreach (var t in LoadableTypes(asm))
        {
            if (t.IsAbstract || !t.IsSubclassOf(typeof(ConfigNode)) || this.nodes.ContainsKey(t))
                continue;
            if (t.GetConstructor(Type.EmptyTypes) == null)
                continue;
            var node = (ConfigNode)Activator.CreateInstance(t)!;
            node.Modified += _ => this.Modified?.Invoke();
            this.nodes[t] = node;
        }
    }

    /// <summary>
    /// This node's single instance, created on demand. A module asking for a node the registry has not seen
    /// gets a defaulted one rather than an exception — the alternative is that adding a config class to a
    /// module makes it crash until the registry is rebuilt.
    /// </summary>
    public T Get<T>() where T : ConfigNode, new()
    {
        if (!this.nodes.TryGetValue(typeof(T), out var node))
        {
            node = new T();
            node.Modified += _ => this.Modified?.Invoke();
            this.nodes[typeof(T)] = node;
        }
        return (T)node;
    }

    /// <summary>Every registered node, for the settings window to render.</summary>
    public IEnumerable<ConfigNode> Nodes => this.nodes.Values;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
    };

    /// <summary>
    /// Layer saved values over the defaults, keyed by node type name. Unknown keys and unreadable nodes are
    /// skipped rather than aborting the load: a config file written by a newer build, or one naming a module
    /// that has since been renamed, should cost you that one node's settings and nothing else.
    /// </summary>
    public void LoadFrom(string json, Action<string>? onError = null)
    {
        JsonObject? root;
        try
        {
            root = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException ex)
        {
            onError?.Invoke($"config is not valid JSON, keeping defaults: {ex.Message}");
            return;
        }
        if (root == null)
            return;

        var byName = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var t in this.nodes.Keys)
            byName[t.FullName ?? t.Name] = t;

        foreach (var (key, value) in root)
        {
            if (value == null || !byName.TryGetValue(key, out var type))
                continue;
            try
            {
                if (JsonSerializer.Deserialize(value.ToJsonString(), type, SerializerOptions) is ConfigNode loaded)
                    CopyFields(loaded, this.nodes[type]);
            }
            catch (JsonException ex)
            {
                onError?.Invoke($"config node '{key}' could not be read, keeping its defaults: {ex.Message}");
            }
        }
    }

    public string SaveToString()
    {
        var root = new JsonObject();
        foreach (var (type, node) in this.nodes)
            root[type.FullName ?? type.Name] = JsonNode.Parse(JsonSerializer.Serialize(node, type, SerializerOptions));
        return root.ToJsonString(SerializerOptions);
    }

    // Values are copied into the existing instance rather than swapping it in: modules capture their node in
    // their constructor, so replacing the instance would leave them reading a detached object forever.
    private static void CopyFields(ConfigNode from, ConfigNode to)
    {
        foreach (var f in to.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            if (!f.IsInitOnly)
                f.SetValue(to, f.GetValue(from));
    }

    // The plugin assembly also holds Dalamud-dependent types that fail to load headlessly, which makes
    // GetTypes() throw; the survivors are what we can use. Same reasoning as ModuleRegistry.LoadableTypes.
    private static IEnumerable<Type> LoadableTypes(Assembly asm)
    {
        try
        {
            return asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
