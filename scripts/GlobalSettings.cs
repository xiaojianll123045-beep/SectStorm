using Godot;

public partial class GlobalSettings : Node
{
    public enum WindowMode { Windowed, Borderless, Fullscreen }
    public enum Language { Chinese, English }

    public WindowMode WinMode = WindowMode.Fullscreen;
    public Language Lang = Language.Chinese;
    public int ResolutionX = 1280;
    public int ResolutionY = 720;
    public float MasterVolume = 0.8f;

    public static readonly Vector2I[] Resolutions = BuildResList();

    private static Vector2I[] BuildResList()
    {
        var list = new System.Collections.Generic.List<Vector2I>();

        void Add(int w, int h) { list.Add(new Vector2I(w, h)); }

        // 4:3
        Add(640, 480); Add(800, 600); Add(1024, 768); Add(1152, 864);
        Add(1280, 960); Add(1400, 1050); Add(1600, 1200);
        Add(1920, 1440); Add(2048, 1536); Add(2560, 1920); Add(2880, 2160);

        // 5:4
        Add(1280, 1024);

        // 16:10
        Add(640, 400); Add(800, 500); Add(960, 600); Add(1024, 640);
        Add(1152, 720); Add(1280, 800); Add(1440, 900);
        Add(1600, 1000); Add(1680, 1050); Add(1920, 1200);
        Add(2048, 1280); Add(2560, 1600); Add(2880, 1800); Add(3840, 2400);

        // 16:9
        Add(640, 360); Add(800, 450); Add(854, 480); Add(960, 540);
        Add(1024, 576); Add(1152, 648); Add(1280, 720); Add(1360, 768);
        Add(1366, 768); Add(1440, 810); Add(1536, 864); Add(1600, 900);
        Add(1680, 945); Add(1768, 992); Add(1920, 1080);
        Add(2048, 1152); Add(2160, 1215); Add(2280, 1282);
        Add(2400, 1350); Add(2560, 1440); Add(2732, 1536);
        Add(2880, 1620); Add(3072, 1728); Add(3200, 1800);
        Add(3440, 1935); Add(3840, 2160); Add(5120, 2880); Add(7680, 4320);

        // 21:9
        Add(2560, 1080); Add(3440, 1440); Add(3840, 1600); Add(5120, 2160);

        // 3:2
        Add(720, 480); Add(960, 640); Add(1152, 768); Add(1440, 960);
        Add(1536, 1024); Add(1920, 1280); Add(2400, 1600); Add(2880, 1920); Add(3840, 2560);

        list.Sort((a, b) => a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

        // deduplicate
        var dedup = new System.Collections.Generic.List<Vector2I>();
        foreach (var r in list)
        {
            if (dedup.Count == 0 || dedup[^1] != r)
                dedup.Add(r);
        }
        return dedup.ToArray();
    }

    public void AutoDetect()
    {
        var screen = DisplayServer.ScreenGetSize();
        int best = 0;
        int bestDist = int.MaxValue;
        for (int i = 0; i < Resolutions.Length; i++)
        {
            int dx = Resolutions[i].X - screen.X;
            int dy = Resolutions[i].Y - screen.Y;
            int d = dx * dx + dy * dy;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        ResolutionX = Resolutions[best].X;
        ResolutionY = Resolutions[best].Y;
    }

    public void Apply()
    {
        DisplayServer.WindowSetSize(new Vector2I(ResolutionX, ResolutionY));
        switch (WinMode)
        {
            case WindowMode.Windowed:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                break;
            case WindowMode.Borderless:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                break;
            case WindowMode.Fullscreen:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
                break;
        }
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Mathf.LinearToDb(MasterVolume));
    }
}
