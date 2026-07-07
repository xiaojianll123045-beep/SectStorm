using Godot;
using System.Linq;

public partial class SectPanel : Control
{
    private GameManager _gm;
    private RichTextLabel _info;
    private bool _open;

    public override void _Ready()
    {
        Visible = false;
        SetAnchorsPreset(LayoutPreset.FullRect);

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.5f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.Size = new Vector2(500, 400);
        panel.Position = new Vector2(-250, -200);
        var ps = new StyleBoxFlat();
        ps.BgColor = new Color(0.10f, 0.10f, 0.12f);
        ps.CornerRadiusTopLeft = 8; ps.CornerRadiusTopRight = 8;
        ps.CornerRadiusBottomLeft = 8; ps.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", ps);
        AddChild(panel);

        _info = new RichTextLabel();
        _info.Position = new Vector2(16, 16);
        _info.Size = new Vector2(468, 340);
        _info.BbcodeEnabled = true;
        _info.AddThemeFontSizeOverride("normal_font_size", 13);
        _info.AddThemeColorOverride("default_color", new Color(0.85f, 0.85f, 0.90f));
        panel.AddChild(_info);

        var closeBtn = new Button();
        closeBtn.Text = "关闭";
        closeBtn.Position = new Vector2(400, 360);
        closeBtn.CustomMinimumSize = new Vector2(80, 28);
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        closeBtn.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
        closeBtn.Pressed += () => { Visible = false; _open = false; };
        panel.AddChild(closeBtn);
    }

    public void Toggle()
    {
        if (!_open) Refresh();
        _open = !_open;
        Visible = _open;
    }

    private void Refresh()
    {
        if (_gm == null) _gm = GetNodeOrNull<GameManager>("../GameManager");
        if (_gm == null || _gm.State == null) return;
        var sect = _gm.State.PlayerSect;
        if (sect == null) return;

        string txt = $"[b]{sect.Name} 宗门概况[/b]\n\n";
        txt += $"灵石: {(int)sect.Lingshi} | 声望: {(int)sect.Prestige} | 灵脉: {(int)sect.SpiritVein}\n\n";

        txt += "[b]建筑[/b]\n";
        txt += $"  议事堂 Lv.{sect.MeetingHall}\n";
        txt += $"  修炼室 Lv.{sect.CultivationRoom}  (突破加成 +{sect.BreakthroughBonus() * 100 - 100:F0}%)\n";
        txt += $"  藏经阁 Lv.{sect.Library}\n";
        txt += $"  丹房 Lv.{sect.AlchemyRoom}  (炼丹加成 +{sect.AlchemyBonus() * 100 - 100:F0}%)\n";
        txt += $"  灵田 Lv.{sect.SpiritField}\n";
        txt += $"  护山大阵 Lv.{sect.ProtectionArray}  (防御加成 +{sect.DefenseBonus() * 100 - 100:F0}%)\n\n";

        int total = _gm.State.Disciples.Count(d => d.SectId == sect.Id);
        txt += $"[b]弟子 ({total}/{sect.MaxDisciples()})[/b]\n";
        var disciples = _gm.State.Disciples.Where(d => d.SectId == sect.Id).OrderByDescending(d => d.Realm).ThenBy(d => d.Combat).ToList();
        int showCount = Mathf.Min(10, disciples.Count);
        for (int i = 0; i < showCount; i++)
        {
            var d = disciples[i];
            txt += $"  {d.Name} ({d.Realm}) 战力{d.Combat} 寿元{d.Lifespan}旬 心境{d.Mood} 忠诚{d.Loyalty} 状态:{d.State}\n";
        }
        if (disciples.Count > showCount)
            txt += $"  ... 还有 {disciples.Count - showCount} 名弟子\n\n";

        txt += $"\n[color=#808080]快捷键: ESC=暂停  F=迷雾  L=日志[/color]";
        _info.Text = txt;
    }
}
