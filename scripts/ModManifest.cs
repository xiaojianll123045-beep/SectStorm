using Godot;
using System.Collections.Generic;

public class ModManifest
{
    public string Id;
    public string Name;
    public string Version = "1.0";
    public string Author = "";
    public string Description = "";
    public string Type = "data";
    public List<string> Dependencies = new();
    public List<string> Conflicts = new();
    public string MinGameVersion = "0.1";
    public string Folder;

    public static ModManifest Load(string folderPath)
    {
        var m = new ModManifest();
        m.Folder = folderPath;
        m.Id = folderPath.GetFile();

        var jsonPath = folderPath + "/mod.json";
        if (!FileAccess.FileExists(jsonPath)) return m;

        using var f = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
        if (f == null) return m;
        var jsonStr = f.GetAsText();
        var json = Json.ParseString(jsonStr);
        if (json.VariantType != Variant.Type.Dictionary) return m;
        var dict = json.AsGodotDictionary();

        if (dict.TryGetValue("version", out var v)) m.Version = v.AsString();
        if (dict.TryGetValue("author", out var a)) m.Author = a.AsString();
        if (dict.TryGetValue("type", out var t)) m.Type = t.AsString();
        if (dict.TryGetValue("min_game_version", out var mg)) m.MinGameVersion = mg.AsString();
        if (dict.TryGetValue("name", out var n)) m.Name = n.AsString();
        if (dict.TryGetValue("dependencies", out var deps))
            foreach (var d in deps.AsGodotArray()) m.Dependencies.Add(d.AsString());
        if (dict.TryGetValue("conflicts", out var confs))
            foreach (var c in confs.AsGodotArray()) m.Conflicts.Add(c.AsString());

        // locale name override
        var localePath = folderPath + $"/mod_{TranslationServer.GetLocale()}.json";
        if (FileAccess.FileExists(localePath))
        {
            using var lf = FileAccess.Open(localePath, FileAccess.ModeFlags.Read);
            if (lf != null)
            {
                var lj = Json.ParseString(lf.GetAsText());
                if (lj.VariantType == Variant.Type.Dictionary)
                {
                    var ld = lj.AsGodotDictionary();
                    if (ld.TryGetValue("name", out var ln)) m.Name = ln.AsString();
                    if (ld.TryGetValue("description", out var ld2)) m.Description = ld2.AsString();
                }
            }
        }
        if (string.IsNullOrEmpty(m.Name)) m.Name = m.Id;

        return m;
    }
}
