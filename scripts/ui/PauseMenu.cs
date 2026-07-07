using Godot;
using System.Linq;

public partial class PauseMenu : Control
{
    private bool _open;

    public override void _Ready()
    {
        Visible = false;
        SetAnchorsPreset(LayoutPreset.FullRect);

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.6f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.Size = new Vector2(300, 280);
        panel.Position = new Vector2(-150, -140);
        var ps = new StyleBoxFlat();
        ps.BgColor = new Color(0.10f, 0.10f, 0.12f);
        ps.CornerRadiusTopLeft = 8; ps.CornerRadiusTopRight = 8;
        ps.CornerRadiusBottomLeft = 8; ps.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", ps);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.Size = new Vector2(260, 240);
        vbox.Position = new Vector2(20, 20);
        vbox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(vbox);

        var title = new Label();
        title.Text = "暂停";
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.2f));
        vbox.AddChild(title);

        AddBtn(vbox, "保存游戏", () => { ShowSlots(true); });
        AddBtn(vbox, "加载游戏", () => { ShowSlots(false); });
        AddBtn(vbox, "宗门面板", () => { Visible = false; _open = false; EmitSignal(nameof(OpenSectPanel)); });
        AddBtn(vbox, "外交 (D)", () => { Visible = false; _open = false; EmitSignal(nameof(OpenDiplomacy)); });
        AddBtn(vbox, "编组部队 (A)", () => { Visible = false; _open = false; EmitSignal(nameof(OpenArmyCreator)); });
        AddBtn(vbox, "返回主菜单", () => { GetTree().ChangeSceneToFile("res://scenes/menu.tscn"); });
        AddBtn(vbox, "退出游戏", () => GetTree().Quit());
        AddBtn(vbox, "继续游戏", () => { Visible = false; _open = false; });
    }

    [Signal] public delegate void OpenSectPanelEventHandler();
    [Signal] public delegate void OpenDiplomacyEventHandler();
    [Signal] public delegate void OpenArmyCreatorEventHandler();

    private Control _slotPanel;
    private VBoxContainer _slotBox;

    private void ShowSlots(bool saving)
    {
        if (_slotPanel == null) BuildSlotPanel();
        _slotBox.GetChildren().ToList().ForEach(c => c.QueueFree());

        var saves = SaveLoadManager.ListSaves();
        var title = new Label();
        title.Text = saving ? "保存到：" : "读取存档：";
        title.AddThemeFontSizeOverride("font_size", 12);
        title.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.2f));
        _slotBox.AddChild(title);

        for (int i = 0; i < 5; i++)
        {
            string slotName = $"slot{i + 1}";
            bool exists = saves.Contains(slotName);
            var hbox = new HBoxContainer();
            hbox.CustomMinimumSize = new Vector2(0, 24);

            var label = new Label();
            label.Text = $"{slotName} {(exists ? "💾" : "空")}";
            label.CustomMinimumSize = new Vector2(100, 0);
            label.AddThemeFontSizeOverride("font_size", 12);
            label.AddThemeColorOverride("font_color", exists ? new Color(0.85f, 0.85f, 0.90f) : new Color(0.5f, 0.5f, 0.55f));
            hbox.AddChild(label);

            if (exists)
            {
                var loadBtn = new Button();
                loadBtn.Text = saving ? "覆盖" : "读取";
                loadBtn.CustomMinimumSize = new Vector2(50, 22);
                loadBtn.AddThemeFontSizeOverride("font_size", 11);
                loadBtn.Pressed += () =>
                {
                    var gm = GetNodeOrNull<GameManager>("../../GameManager");
                    if (gm == null) return;
                    if (saving) SaveLoadManager.SaveGame(gm, slotName);
                    else
                    {
                        SaveLoadManager.LoadGame(gm, slotName);
                        _slotPanel.Visible = false;
                        Visible = false; _open = false;
                        // signal MapView to regenerate visuals
                        var mv = GetNodeOrNull<MapView>("../..");
                        mv?.Call("OnLoadGame");
                    }
                };
                hbox.AddChild(loadBtn);
            }
            else if (saving)
            {
                var saveBtn = new Button();
                saveBtn.Text = "保存";
                saveBtn.CustomMinimumSize = new Vector2(50, 22);
                saveBtn.AddThemeFontSizeOverride("font_size", 11);
                saveBtn.Pressed += () =>
                {
                    var gm = GetNodeOrNull<GameManager>("../GameManager");
                    if (gm != null) SaveLoadManager.SaveGame(gm, slotName);
                };
                hbox.AddChild(saveBtn);
            }

            _slotBox.AddChild(hbox);
        }

        var closeBtn = new Button();
        closeBtn.Text = "关闭";
        closeBtn.Pressed += () => _slotPanel.Visible = false;
        _slotBox.AddChild(closeBtn);

        _slotPanel.Visible = true;
    }

    private void BuildSlotPanel()
    {
        _slotPanel = new Control();
        _slotPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        _slotPanel.Visible = false;
        AddChild(_slotPanel);

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.5f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Pass;
        _slotPanel.AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.Size = new Vector2(250, 300);
        panel.Position = new Vector2(-125, -150);
        var ps = new StyleBoxFlat();
        ps.BgColor = new Color(0.10f, 0.10f, 0.12f);
        ps.CornerRadiusTopLeft = 8; ps.CornerRadiusTopRight = 8;
        ps.CornerRadiusBottomLeft = 8; ps.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", ps);
        _slotPanel.AddChild(panel);

        _slotBox = new VBoxContainer();
        _slotBox.Position = new Vector2(16, 16);
        _slotBox.Size = new Vector2(220, 270);
        _slotBox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(_slotBox);
    }

    private void AddBtn(Container parent, string text, System.Action action)
    {
        var btn = new Button();
        btn.Text = text;
        btn.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
        btn.CustomMinimumSize = new Vector2(0, 30);
        btn.AddThemeFontSizeOverride("font_size", 14);
        btn.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
        btn.AddThemeColorOverride("font_hover_color", new Color(1f, 0.85f, 0.4f));
        btn.Pressed += action;
        parent.AddChild(btn);
    }

    private void SaveGame()
    {
        var gm = GetNodeOrNull<GameManager>("../GameManager");
        if (gm == null) return;
        using var f = FileAccess.Open("user://save.json", FileAccess.ModeFlags.Write);
        if (f == null) { GD.PrintErr("Save failed"); return; }
        var data = new Godot.Collections.Dictionary();
        data["seed"] = gm.State.Seed;
        data["year"] = gm.State.Year;
        data["xun"] = gm.State.Xun;
        data["lingshi"] = gm.State.PlayerSect?.Lingshi ?? 0;
        f.StoreString(Json.Stringify(data));
        GD.Print("Game saved");
    }

    private void LoadGame()
    {
        if (!FileAccess.FileExists("user://save.json")) { GD.Print("No save found"); return; }
        using var f = FileAccess.Open("user://save.json", FileAccess.ModeFlags.Read);
        if (f == null) return;
        var json = Json.ParseString(f.GetAsText());
        GD.Print("Save loaded: ", json);
    }

    public void Toggle()
    {
        _open = !_open;
        Visible = _open;
    }
}
