using Godot;
using System;
using System.Collections.Generic;

public static class NameDB
{
    private static string[] _cityPre, _citySuf, _villagePre, _villageSuf, _sectPre, _sectSuf;
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        var file = FileAccess.Open("res://assets/data/名字库.json", FileAccess.ModeFlags.Read);
        if (file == null) { GD.PrintErr("NameDB: 名字库.json not found"); return; }
        var jsonStr = file.GetAsText();
        file.Close();

        var json = Json.ParseString(jsonStr);
        if (json.VariantType != Variant.Type.Dictionary) { GD.PrintErr("NameDB: bad json"); return; }
        var dict = json.AsGodotDictionary();

        _cityPre = ToArray(dict["城池_前缀"]);
        _citySuf = ToArray(dict["城池_后缀"]);
        _villagePre = ToArray(dict["村庄_前缀"]);
        _villageSuf = ToArray(dict["村庄_后缀"]);
        _sectPre = ToArray(dict["宗门_前缀"]);
        _sectSuf = ToArray(dict["宗门_后缀"]);

        GD.Print($"NameDB loaded: city {_cityPre.Length}x{_citySuf.Length}={_cityPre.Length*_citySuf.Length}, village {_villagePre.Length}x{_villageSuf.Length}={_villagePre.Length*_villageSuf.Length}, sect {_sectPre.Length}x{_sectSuf.Length}={_sectPre.Length*_sectSuf.Length}");
    }

    private static string[] ToArray(Variant v)
    {
        var arr = v.AsGodotArray();
        var result = new string[arr.Count];
        for (int i = 0; i < arr.Count; i++) result[i] = arr[i].AsString();
        return result;
    }

    public static string GenerateName(LocationType type, Random rng, HashSet<string> used)
    {
        Load();
        string[] pre, suf;
        string fallback;

        switch (type)
        {
            case LocationType.City: pre = _cityPre; suf = _citySuf; fallback = "城"; break;
            case LocationType.Village: pre = _villagePre; suf = _villageSuf; fallback = "村"; break;
            case LocationType.Sect: pre = _sectPre; suf = _sectSuf; fallback = "宗"; break;
            default: return "无名";
        }

        if (pre == null || suf == null || pre.Length == 0 || suf.Length == 0)
            return fallback + used.Count;

        for (int i = 0; i < 100; i++)
        {
            string name = pre[rng.Next(pre.Length)] + suf[rng.Next(suf.Length)];
            if (!used.Contains(name)) { used.Add(name); return name; }
        }
        return fallback + used.Count;
    }
}
