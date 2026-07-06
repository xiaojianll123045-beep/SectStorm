using Godot;
using System.Collections.Generic;

public static class ModSandbox
{
    private static string _modsRes = "res://mods";
    private static string _modsUser = "user://mods";

    public static void Init()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(_modsRes));
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(_modsUser));
    }

    public static List<string> ScanModFolders()
    {
        var folders = new List<string>();
        foreach (var baseDir in new[] { _modsRes, _modsUser })
        {
            var da = DirAccess.Open(baseDir);
            if (da == null) continue;
            da.ListDirBegin();
            while (true)
            {
                var f = da.GetNext();
                if (string.IsNullOrEmpty(f)) break;
                if (f.StartsWith(".")) continue;
                var full = baseDir + "/" + f;
                if (da.CurrentIsDir())
                    folders.Add(full);
            }
            da.ListDirEnd();
        }
        return folders;
    }

    public static string ResolveModPath(string modId, string relativePath)
    {
        var inRes = _modsRes + "/" + modId + "/" + relativePath;
        if (FileAccess.FileExists(inRes)) return inRes;
        var inUser = _modsUser + "/" + modId + "/" + relativePath;
        return inUser;
    }
}
