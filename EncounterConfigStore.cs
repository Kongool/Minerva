using System;
using System.IO;

namespace Minerva;

/// <summary>
/// Persists the encounter <see cref="ConfigRoot"/> to its own file beside the plugin config.
///
/// <para>Kept separate from <see cref="Configuration"/> deliberately. That one is Dalamud-managed plugin
/// settings — a fixed set of fields Minerva owns. This is a reflected, open-ended set that grows with every
/// ported fight, and a raid group will want to hand it to each other. One file per concern means a corrupt
/// or outdated encounter file costs you strategy settings, not your whole plugin config.</para>
///
/// <para>Saves are debounced by a dirty flag rather than written on every ImGui frame: dragging a slider
/// fires a change per frame, and each one would be a synchronous disk write on the render thread.</para>
/// </summary>
public sealed class EncounterConfigStore : IDisposable
{
    private const string FileName = "EncounterSettings.json";

    private readonly ConfigRoot config;
    private readonly string path;
    private bool dirty;

    public EncounterConfigStore(ConfigRoot config, string directory)
    {
        this.config = config;
        this.path = Path.Combine(directory, FileName);
        this.Load();
        this.config.Modified += this.OnModified;
    }

    private void OnModified() => this.dirty = true;

    private void Load()
    {
        if (!File.Exists(this.path))
            return;
        try
        {
            this.config.LoadFrom(File.ReadAllText(this.path), msg => Service.Log.Warning($"[EncounterSettings] {msg}"));
        }
        catch (IOException ex)
        {
            Service.Log.Error(ex, "Could not read encounter settings; keeping defaults.");
        }
        catch (UnauthorizedAccessException ex)
        {
            Service.Log.Error(ex, "Could not read encounter settings; keeping defaults.");
        }
    }

    /// <summary>Write out if anything changed since the last call. Cheap when nothing has.</summary>
    public void Flush()
    {
        if (!this.dirty)
            return;
        this.dirty = false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(this.path)!);
            File.WriteAllText(this.path, this.config.SaveToString());
        }
        catch (IOException ex)
        {
            Service.Log.Error(ex, "Could not save encounter settings.");
        }
        catch (UnauthorizedAccessException ex)
        {
            Service.Log.Error(ex, "Could not save encounter settings.");
        }
    }

    public void Dispose()
    {
        this.config.Modified -= this.OnModified;
        this.Flush();
    }
}
