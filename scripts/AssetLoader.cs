using Godot;
using System.Collections.Generic;

public static class AssetLoader
{
    private static Dictionary<string, Texture2D> _cache = new();

    public static Texture2D Load(string path, Texture2D fallback = null)
    {
        if (_cache.TryGetValue(path, out var tex))
            return tex;
        var loaded = ResourceLoader.Load<Texture2D>(path);
        if (loaded != null)
        {
            _cache[path] = loaded;
            return loaded;
        }
        GD.PrintErr($"AssetLoader: failed to load {path}");
        if (fallback != null) _cache[path] = fallback;
        return fallback;
    }

    // --- fallback textures ---
    private static Texture2D _mkCircle(int r, Color c)
    {
        int d = r * 2;
        var img = Image.CreateEmpty(d, d, false, Image.Format.Rgba8);
        Color b = c.Lerp(Colors.White, 0.3f);
        for (int y = 0; y < d; y++)
            for (int x = 0; x < d; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= r) img.SetPixel(x, y, dist > r - 2f ? b : c);
            }
        return ImageTexture.CreateFromImage(img);
    }

    private static Texture2D _mkSect(int r, Color c)
    {
        int d = r * 2;
        var img = Image.CreateEmpty(d, d, false, Image.Format.Rgba8);
        Color b = c.Lerp(Colors.White, 0.3f);
        for (int y = 0; y < d; y++)
            for (int x = 0; x < d; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= r) img.SetPixel(x, y, dist > r - 2f ? b : c);
            }
        for (int y = 0; y < d; y++)
        {
            int cx = r;
            if (y >= r - 3 && y <= r + 3)
            {
                img.SetPixel(cx, y, Colors.White);
                img.SetPixel(cx - 1, y, Colors.White);
                img.SetPixel(cx + 1, y, Colors.White);
            }
            if (y >= r - 5 && y <= r + 5)
            {
                int ox = r - 5 + (y - (r - 5));
                if (ox >= 0 && ox < d) img.SetPixel(ox, y, Colors.White.Lerp(c, 0.3f));
                int ox2 = r + 5 - (y - (r - 5));
                if (ox2 >= 0 && ox2 < d) img.SetPixel(ox2, y, Colors.White.Lerp(c, 0.3f));
            }
        }
        return ImageTexture.CreateFromImage(img);
    }

    private static Texture2D _fallback(int type)
    {
        return type switch
        {
            0 => _mkCircle(14, new Color(0.75f, 0.23f, 0.17f)),  // city
            1 => _mkCircle(7, new Color(0.54f, 0.27f, 0.07f)),   // village
            2 => _mkSect(11, new Color(0.42f, 0.36f, 0.91f)),    // sect
            _ => _mkCircle(8, Colors.Gray)
        };
    }

    // --- public accessors with fallback ---
    public static Texture2D MarkerCity => Load("res://assets/icons/城市.svg", _fallback(0));
    public static Texture2D MarkerVillage => Load("res://assets/icons/村庄.svg", _fallback(1));
    public static Texture2D MarkerSect => Load("res://assets/icons/宗门.svg", _fallback(2));
    public static Texture2D MarkerSectRed => Load("res://assets/icons/宗门_红.svg", _fallback(2));
    public static Texture2D MarkerSectGreen => Load("res://assets/icons/宗门_绿.svg", _fallback(2));
    public static Texture2D MarkerSectPurple => Load("res://assets/icons/宗门_紫.svg", _fallback(2));
}
