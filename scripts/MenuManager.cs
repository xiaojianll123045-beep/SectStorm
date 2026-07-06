using Godot;

public partial class MenuManager : Control
{
    private Control _settingsPanel;
    private Control _aboutPanel;
    private GlobalSettings _settings;

    private Color BgColor = new Color(0.06f, 0.06f, 0.08f);
    private Color PanelBg = new Color(0.10f, 0.10f, 0.12f);
    private Color Gold = new Color(0.70f, 0.50f, 0.20f);
    private Color GoldBright = new Color(0.90f, 0.75f, 0.40f);
    private Color TextColor = new Color(0.85f, 0.85f, 0.90f);

    public override void _Ready()
    {
        _settings = new GlobalSettings();
        _settings.AutoDetect();
        _settings.Apply();
        AddChild(_settings);

        BuildUI();
    }

    private void BuildUI()
    {
        // background
        var bg = new ColorRect();
        bg.Color = BgColor;
        bg.Size = GetViewportRect().Size;
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(bg);

        // title
        var title = new Label();
        title.Text = "宗门风云";
        title.AddThemeFontSizeOverride("font_size", 48);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        title.Position = new Vector2(0, 80);
        title.AddThemeColorOverride("font_color", Gold);
        AddChild(title);

        // subtitle
        var sub = new Label();
        sub.Text = "SectStorm";
        sub.AddThemeFontSizeOverride("font_size", 16);
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        sub.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        sub.Position = new Vector2(0, 130);
        sub.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        AddChild(sub);

        // buttons
        var btnBox = new VBoxContainer();
        btnBox.SetAnchorsPreset(Control.LayoutPreset.Center);
        btnBox.Position = new Vector2(-80, -60);
        btnBox.Size = new Vector2(160, 180);
        btnBox.AddThemeConstantOverride("separation", 8);
        AddChild(btnBox);

        AddMenuBtn(btnBox, "开始游戏", () => OnStartGame());
        AddMenuBtn(btnBox, "模组管理", () => OnMods());
        AddMenuBtn(btnBox, "DLC", () => OnDLC());
        AddMenuBtn(btnBox, "设置", () => OnSettings());
        AddMenuBtn(btnBox, "关于", () => OnAbout());
        AddMenuBtn(btnBox, "退出", () => OnQuit());

        // version
        var ver = new Label();
        ver.Text = "v0.1";
        ver.AddThemeFontSizeOverride("font_size", 11);
        ver.Position = new Vector2(10, GetViewportRect().Size.Y - 25);
        ver.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
        AddChild(ver);

        // settings panel (hidden)
        BuildSettingsPanel();
    }

    private Button AddMenuBtn(Container parent, string text, System.Action action)
    {
        var btn = new Button();
        btn.Text = text;
        btn.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
        btn.CustomMinimumSize = new Vector2(160, 36);
        btn.AddThemeFontSizeOverride("font_size", 16);
        btn.AddThemeColorOverride("font_color", TextColor);
        btn.AddThemeColorOverride("font_hover_color", GoldBright);
        btn.AddThemeColorOverride("font_pressed_color", Gold);
        btn.AddThemeStyleboxOverride("normal", MakeBtnStyle(new Color(0.15f, 0.15f, 0.18f)));
        btn.AddThemeStyleboxOverride("hover", MakeBtnStyle(new Color(0.20f, 0.20f, 0.25f)));
        btn.AddThemeStyleboxOverride("pressed", MakeBtnStyle(new Color(0.12f, 0.12f, 0.14f)));
        btn.Pressed += action;
        parent.AddChild(btn);
        return btn;
    }

    private StyleBoxFlat MakeBtnStyle(Color bg)
    {
        var s = new StyleBoxFlat();
        s.BgColor = bg;
        s.CornerRadiusTopLeft = 4;
        s.CornerRadiusTopRight = 4;
        s.CornerRadiusBottomLeft = 4;
        s.CornerRadiusBottomRight = 4;
        s.ContentMarginLeft = 12;
        s.ContentMarginRight = 12;
        return s;
    }

    private void OnStartGame()
    {
        var loading = ResourceLoader.Load<PackedScene>("res://scenes/LoadingScreen.tscn").Instantiate<LoadingScreen>();
        GetTree().Root.AddChild(loading);
        loading.SetStatus("正在初始化模组系统...");
        ModManager.Init();
        loading.SetProgress(1);
        loading.SetStatus("正在加载模组...");
        ModManager.ApplyAll();
        loading.SetProgress(2);
        loading.SetStatus("正在初始化 API...");
        ModAPI.Init();
        loading.SetProgress(3);
        loading.SetStatus("正在准备控制台...");
        // Defer map loading so loading screen renders first
        loading.SetProgress(4);
        loading.SetStatus("正在生成地图...");
        Callable.From(() =>
        {
            GetTree().ChangeSceneToFile("res://scenes/MapView.tscn");
            loading.Complete();
            loading.QueueFree();
        }).CallDeferred();
    }

    private void OnQuit()
    {
        GetTree().Quit();
    }

    // ===== Settings =====
    private void BuildSettingsPanel()
    {
        _settingsPanel = new Control();
        _settingsPanel.Visible = false;
        _settingsPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_settingsPanel);

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.5f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _settingsPanel.AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.Size = new Vector2(500, 400);
        panel.Position = new Vector2(-250, -200);
        var pstyle = new StyleBoxFlat();
        pstyle.BgColor = PanelBg;
        pstyle.CornerRadiusTopLeft = 8;
        pstyle.CornerRadiusTopRight = 8;
        pstyle.CornerRadiusBottomLeft = 8;
        pstyle.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", pstyle);
        _settingsPanel.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.Size = new Vector2(460, 360);
        vbox.Position = new Vector2(20, 20);
        panel.AddChild(vbox);

        // title
        var title = new Label();
        title.Text = "设置";
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", Gold);
        vbox.AddChild(title);

        AddSeparator(vbox);

        // Display
        AddSectionLabel(vbox, "显示");

        var h1 = new HBoxContainer();
        h1.AddChild(MakeLabel("窗口模式"));
        var winOpt = new OptionButton();
        winOpt.AddItem("窗口", 0);
        winOpt.AddItem("无边框", 1);
        winOpt.AddItem("全屏", 2);
        winOpt.Selected = 0;
        winOpt.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        winOpt.ItemSelected += (idx) => { _settings.WinMode = (GlobalSettings.WindowMode)(int)idx; _settings.Apply(); };
        h1.AddChild(winOpt);
        vbox.AddChild(h1);

        var h2 = new HBoxContainer();
        h2.AddChild(MakeLabel("分辨率"));
        var resOpt = new OptionButton();
        int selectedRes = 0;
        for (int i = 0; i < GlobalSettings.Resolutions.Length; i++)
        {
            var r = GlobalSettings.Resolutions[i];
            resOpt.AddItem($"{r.X}x{r.Y}", i);
            if (r.X == _settings.ResolutionX && r.Y == _settings.ResolutionY)
                selectedRes = i;
        }
        resOpt.Selected = selectedRes;
        resOpt.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        resOpt.ItemSelected += (idx) =>
        {
            var r = GlobalSettings.Resolutions[(int)idx];
            _settings.ResolutionX = r.X;
            _settings.ResolutionY = r.Y;
            _settings.Apply();
        };
        h2.AddChild(resOpt);
        vbox.AddChild(h2);

        AddSeparator(vbox);

        // Audio
        AddSectionLabel(vbox, "音频");

        var h3 = new HBoxContainer();
        h3.AddChild(MakeLabel("音量"));
        var volSlider = new HSlider();
        volSlider.MinValue = 0;
        volSlider.MaxValue = 100;
        volSlider.Value = _settings.MasterVolume * 100;
        volSlider.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        volSlider.ValueChanged += (v) =>
        {
            _settings.MasterVolume = (float)v / 100f;
            _settings.Apply();
        };
        h3.AddChild(volSlider);
        vbox.AddChild(h3);

        // close button
        var closeBtn = new Button();
        closeBtn.Text = "关闭";
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        closeBtn.AddThemeColorOverride("font_color", TextColor);
        closeBtn.AddThemeColorOverride("font_hover_color", GoldBright);
        closeBtn.AddThemeStyleboxOverride("normal", MakeBtnStyle(new Color(0.20f, 0.20f, 0.25f)));
        closeBtn.AddThemeStyleboxOverride("hover", MakeBtnStyle(new Color(0.25f, 0.25f, 0.30f)));
        closeBtn.Position = new Vector2(400, 360);
        closeBtn.CustomMinimumSize = new Vector2(80, 28);
        closeBtn.Pressed += () => _settingsPanel.Visible = false;
        panel.AddChild(closeBtn);
    }

    // ===== About =====
    private void OnAbout()
    {
        if (_aboutPanel == null) BuildAboutPanel();
        _aboutPanel.Visible = !_aboutPanel.Visible;
    }

    private void BuildAboutPanel()
    {
        _aboutPanel = new Control();
        _aboutPanel.Visible = false;
        _aboutPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_aboutPanel);

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.5f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _aboutPanel.AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.Size = new Vector2(460, 420);
        panel.Position = new Vector2(-230, -210);
        var pstyle = new StyleBoxFlat();
        pstyle.BgColor = PanelBg;
        pstyle.CornerRadiusTopLeft = 8;
        pstyle.CornerRadiusTopRight = 8;
        pstyle.CornerRadiusBottomLeft = 8;
        pstyle.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", pstyle);
        _aboutPanel.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.Size = new Vector2(420, 360);
        vbox.Position = new Vector2(20, 20);
        panel.AddChild(vbox);

        var title = new Label();
        title.Text = "关于";
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", Gold);
        vbox.AddChild(title);

        AddSeparator(vbox);

        var rich = new RichTextLabel();
        rich.Size = new Vector2(420, 340);
        rich.FitContent = true;
        rich.BbcodeEnabled = true;
        rich.AddThemeFontSizeOverride("normal_font_size", 13);
        rich.AddThemeColorOverride("default_color", TextColor);
        rich.AddThemeColorOverride("meta_color", new Color(0.6f, 0.8f, 1.0f));
        string text = "[center]开发：小坚来了[/center]\n"
            + "[center]QQ号：1099155831[/center]\n"
            + "[center]邮箱：xjllqw@163.com[/center]\n"
            + "[center]QQ群：495063635[/center]\n\n"
            + "[center]引擎：Godot 4.7 Mono (C# .NET 8)[/center]\n\n"
            + "[center]🧠 本游戏由 [url=https://deepseek.com]DeepSeek[/url] 辅助开发[/center]\n\n"
            + "[center][url=https://github.com/xiaojianll123045/SectStorm]🌐 开源地址[/url][/center]\n\n"
            + "[center]本游戏没有任何防盗版反破解措施[/center]\n"
            + "[center]源码开源，欢迎参考[/center]\n"
            + "[center]玩得开心 ❤️[/center]";
        rich.Text = text;
        rich.MetaClicked += (meta) => { OS.ShellOpen(meta.AsString()); };
        vbox.AddChild(rich);

        var closeBtn = new Button();
        closeBtn.Text = "关闭";
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        closeBtn.AddThemeColorOverride("font_color", TextColor);
        closeBtn.AddThemeColorOverride("font_hover_color", GoldBright);
        closeBtn.AddThemeStyleboxOverride("normal", MakeBtnStyle(new Color(0.20f, 0.20f, 0.25f)));
        closeBtn.AddThemeStyleboxOverride("hover", MakeBtnStyle(new Color(0.25f, 0.25f, 0.30f)));
        closeBtn.Position = new Vector2(360, 380);
        closeBtn.CustomMinimumSize = new Vector2(80, 28);
        closeBtn.Pressed += () => _aboutPanel.Visible = false;
        panel.AddChild(closeBtn);
    }

    private Control _modsPanel;

    private void OnMods()
    {
        if (_modsPanel == null) BuildModsPanel();
        _modsPanel.Visible = !_modsPanel.Visible;
    }

    private void BuildModsPanel()
    {
        _modsPanel = MakePanel(460, 380);
        var vbox = new VBoxContainer();
        vbox.Size = new Vector2(420, 320);
        vbox.Position = new Vector2(20, 20);
        _modsPanel.GetChild(1).AddChild(vbox);

        var title = new Label();
        title.Text = "模组管理";
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", Gold);
        vbox.AddChild(title);
        AddSeparator(vbox);

        // init mod manager to scan mods
        ModSandbox.Init();
        var mods = ModSandbox.ScanModFolders();
        if (mods.Count == 0)
        {
            var lbl = new Label();
            lbl.Text = "未检测到模组\n\n将模组放入 mods/ 文件夹即可";
            lbl.AddThemeColorOverride("font_color", TextColor);
            lbl.AddThemeFontSizeOverride("font_size", 13);
            vbox.AddChild(lbl);
        }
        else
        {
            foreach (var folder in mods)
            {
                var m = ModManifest.Load(folder);
                var hbox = new HBoxContainer();
                var cb = new CheckBox();
                cb.Text = $"{m.Name} v{m.Version}";
                cb.ButtonPressed = ModManager.IsEnabled(m.Id);
                cb.Toggled += (on) => ModManager.SetEnabled(m.Id, on);
                cb.AddThemeColorOverride("font_color", TextColor);
                hbox.AddChild(cb);

                if (!string.IsNullOrEmpty(m.Author))
                {
                    var author = new Label();
                    author.Text = $"by {m.Author}";
                    author.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
                    author.AddThemeFontSizeOverride("font_size", 11);
                    hbox.AddChild(author);
                }
                vbox.AddChild(hbox);
            }
        }

        var closeBtn = new Button();
        closeBtn.Text = "关闭";
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        closeBtn.AddThemeColorOverride("font_color", TextColor);
        closeBtn.AddThemeStyleboxOverride("normal", MakeBtnStyle(new Color(0.20f, 0.20f, 0.25f)));
        closeBtn.AddThemeStyleboxOverride("hover", MakeBtnStyle(new Color(0.25f, 0.25f, 0.30f)));
        closeBtn.Position = new Vector2(360, 340);
        closeBtn.CustomMinimumSize = new Vector2(80, 28);
        closeBtn.Pressed += () => _modsPanel.Visible = false;
        _modsPanel.GetChild(1).AddChild(closeBtn);
    }

    private Control _dlcPanel;

    private void OnDLC()
    {
        if (_dlcPanel == null) BuildDLCPanel();
        _dlcPanel.Visible = !_dlcPanel.Visible;
    }

    private void BuildDLCPanel()
    {
        _dlcPanel = MakePanel(420, 280);
        var vbox = new VBoxContainer();
        vbox.Size = new Vector2(380, 200);
        vbox.Position = new Vector2(20, 20);
        _dlcPanel.GetChild(1).AddChild(vbox);

        var title = new Label();
        title.Text = "DLC";
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", Gold);
        vbox.AddChild(title);
        AddSeparator(vbox);

        var lbl = new Label();
        lbl.Text = "暂无 DLC\n\n敬请期待";
        lbl.AddThemeColorOverride("font_color", TextColor);
        lbl.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(lbl);

        var closeBtn = new Button();
        closeBtn.Text = "关闭";
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        closeBtn.AddThemeColorOverride("font_color", TextColor);
        closeBtn.AddThemeStyleboxOverride("normal", MakeBtnStyle(new Color(0.20f, 0.20f, 0.25f)));
        closeBtn.AddThemeStyleboxOverride("hover", MakeBtnStyle(new Color(0.25f, 0.25f, 0.30f)));
        closeBtn.Position = new Vector2(320, 240);
        closeBtn.CustomMinimumSize = new Vector2(80, 28);
        closeBtn.Pressed += () => _dlcPanel.Visible = false;
        _dlcPanel.GetChild(1).AddChild(closeBtn);
    }

    private Control MakePanel(float w, float h)
    {
        var c = new Control();
        c.Visible = false;
        c.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(c);

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.5f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        c.AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.Size = new Vector2(w, h);
        panel.Position = new Vector2(-w / 2, -h / 2);
        var pstyle = new StyleBoxFlat();
        pstyle.BgColor = PanelBg;
        pstyle.CornerRadiusTopLeft = 8;
        pstyle.CornerRadiusTopRight = 8;
        pstyle.CornerRadiusBottomLeft = 8;
        pstyle.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", pstyle);
        c.AddChild(panel);
        return c;
    }

    private void OnSettings()
    {
        _settingsPanel.Visible = !_settingsPanel.Visible;
    }

    private Label MakeLabel(string text)
    {
        var l = new Label();
        l.Text = text;
        l.CustomMinimumSize = new Vector2(100, 0);
        l.AddThemeColorOverride("font_color", TextColor);
        return l;
    }

    private void AddSectionLabel(Container parent, string text)
    {
        var l = new Label();
        l.Text = text;
        l.AddThemeFontSizeOverride("font_size", 14);
        l.AddThemeColorOverride("font_color", Gold);
        parent.AddChild(l);
    }

    private void AddSeparator(Container parent)
    {
        var sep = new ColorRect();
        sep.Color = new Color(0.3f, 0.3f, 0.35f, 0.3f);
        sep.CustomMinimumSize = new Vector2(0, 1);
        sep.Size = new Vector2(460, 1);
        parent.AddChild(sep);
    }
}
