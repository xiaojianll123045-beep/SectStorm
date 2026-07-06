using Godot;
using System.Collections.Generic;

public partial class ModBridge : Node
{
    public override void _Ready()
    {
        Name = "ModBridge";
        GD.Print("[ModBridge] ready");
    }

    // GDScript-facing helpers
    public void print_mod(string msg) => GD.Print($"[Mod] {msg}");
    public void print_err(string msg) => GD.PrintErr($"[Mod] {msg}");

    public void register_hook(string hookName, Callable handler)
    {
        if (System.Enum.TryParse<ModAPI.GameHook>(hookName, out var hook))
            ModAPI.RegisterHook(hook, () => handler.Call());
    }

    public void fire_event(string eventName)
    {
        ModEventBus.FireCustom(eventName);
    }

    public void listen_event(string eventName, Callable handler)
    {
        ModEventBus.ListenCustom(eventName, (_) => handler.Call());
    }

    public string get_mod_path(string modId, string relativePath)
    {
        return ModSandbox.ResolveModPath(modId, relativePath);
    }

    public bool is_feature_enabled(string key) => ModAPI.IsFeatureEnabled(key);
    public void disable_feature(string key) => ModAPI.DisableFeature(key);
    public void enable_feature(string key) => ModAPI.EnableFeature(key);
}
