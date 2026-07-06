using Godot;
using System;
using System.Collections.Generic;

public static class ModManager
{
    public static List<ModManifest> LoadedMods = new();
    public static List<ModManifest> EnabledMods = new();
    public static List<string> ModLog = new();
    public static List<string> LoadErrors = new();

    private static HashSet<string> _enabledIds = new();

    public static void Init()
    {
        ModSandbox.Init();
        LoadEnabledConfig();
        ScanMods();
    }

    public static void ApplyAll()
    {
        LoadErrors.Clear();
        ModLog.Clear();

        EnabledMods.Clear();
        foreach (var m in LoadedMods)
            if (_enabledIds.Contains(m.Id)) EnabledMods.Add(m);

        // version
        EnabledMods.RemoveAll(m =>
        {
            if (VersionCompare(m.MinGameVersion, "0.1") > 0)
            { LoadErrors.Add($"[{m.Id}] 需要游戏版本 {m.MinGameVersion}"); return true; }
            return false;
        });

        // dependencies
        bool changed = true;
        while (changed)
        {
            changed = false;
            EnabledMods.RemoveAll(m =>
            {
                foreach (var dep in m.Dependencies)
                    if (!_enabledIds.Contains(dep))
                    { LoadErrors.Add($"[{m.Id}] 缺少依赖 {dep}"); return true; }
                return false;
            });
        }

        // conflicts
        foreach (var m in EnabledMods)
            foreach (var c in m.Conflicts)
                if (_enabledIds.Contains(c))
                    LoadErrors.Add($"冲突: {m.Id} 与 {c}");

        // apply
        foreach (var m in EnabledMods)
        {
            try { ApplyMod(m); ModLog.Add($"[OK] {m.Name} v{m.Version}"); }
            catch (Exception e)
            { LoadErrors.Add($"[{m.Id}] 加载失败: {e.Message}"); GD.PrintErr($"[ModManager] FAIL {m.Id}: {e.Message}"); }
        }

        GD.Print($"[ModManager] loaded {EnabledMods.Count}/{LoadedMods.Count} mods");
    }

    private static void ApplyMod(ModManifest m)
    {
        if (m.Type == "language") ApplyLanguageMod(m);
        else if (m.Type == "data") ApplyDataMod(m);
        else if (m.Type == "script") ApplyScriptMod(m);
    }

    private static void ApplyLanguageMod(ModManifest m)
    {
        var dir = m.Folder + "/locale";
        if (!DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(dir))) return;
        var da = DirAccess.Open(dir);
        if (da == null) return;
        da.ListDirBegin();
        while (true)
        {
            var f = da.GetNext();
            if (string.IsNullOrEmpty(f) || !f.EndsWith(".json")) continue;
            GD.Print($"[ModManager] language: {dir}/{f}");
        }
        da.ListDirEnd();
    }

    private static void ApplyDataMod(ModManifest m)
    {
        var dir = m.Folder + "/data";
        if (!DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(dir))) return;
        var da = DirAccess.Open(dir);
        if (da == null) return;
        da.ListDirBegin();
        while (true)
        {
            var f = da.GetNext();
            if (string.IsNullOrEmpty(f) || !f.EndsWith(".json")) continue;
            GD.Print($"[ModManager] data: {dir}/{f}");
        }
        da.ListDirEnd();
    }

    private static void ApplyScriptMod(ModManifest m)
    {
        var dir = m.Folder + "/scripts";
        if (!DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(dir))) return;
        var da = DirAccess.Open(dir);
        if (da == null) return;
        da.ListDirBegin();
        while (true)
        {
            var f = da.GetNext();
            if (string.IsNullOrEmpty(f) || !f.EndsWith(".gd")) continue;
            GD.Print($"[ModManager] script: {dir}/{f}");
        }
        da.ListDirEnd();
    }

    public static void SetEnabled(string modId, bool enabled)
    {
        if (enabled) _enabledIds.Add(modId);
        else _enabledIds.Remove(modId);
        SaveEnabledConfig();
    }

    public static bool IsEnabled(string modId) => _enabledIds.Contains(modId);

    private static void LoadEnabledConfig()
    {
        _enabledIds.Clear();
        var path = "user://mods_enabled.json";
        if (!FileAccess.FileExists(path)) return;
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (f == null) return;
        var json = Json.ParseString(f.GetAsText());
        if (json.VariantType == Variant.Type.Array)
            foreach (var v in json.AsGodotArray())
                _enabledIds.Add(v.AsString());
    }

    private static void SaveEnabledConfig()
    {
        var arr = new Godot.Collections.Array();
        foreach (var id in _enabledIds) arr.Add(id);
        using var f = FileAccess.Open("user://mods_enabled.json", FileAccess.ModeFlags.Write);
        if (f != null) f.StoreString(Json.Stringify(arr));
    }

    private static void ScanMods()
    {
        LoadedMods.Clear();
        foreach (var folder in ModSandbox.ScanModFolders())
        {
            var m = ModManifest.Load(folder);
            LoadedMods.Add(m);
            GD.Print($"[ModManager] found: {m.Id} ({m.Type})");
        }
    }

    private static int VersionCompare(string a, string b)
    {
        var pa = a.Split('.');
        var pb = b.Split('.');
        for (int i = 0; i < Math.Max(pa.Length, pb.Length); i++)
        {
            int va = i < pa.Length && int.TryParse(pa[i], out var x) ? x : 0;
            int vb = i < pb.Length && int.TryParse(pb[i], out var y) ? y : 0;
            if (va != vb) return va - vb;
        }
        return 0;
    }
}
