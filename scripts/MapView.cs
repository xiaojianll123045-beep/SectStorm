using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class MapView : Node2D
{
    public static string LoadSlotOnStart;

    [Export] public int MapWidth = 16384;
    [Export] public int MapHeight = 16384;

    private Sprite2D _terrain;
    private Sprite2D _territory;
    private MapCamera _camera;
    private Node2D _markerLayer;
    private int _seed;
    private Label _info;
    private Button _regenerateBtn;
    private List<MapLocation> _locations;
    private Label _tooltip;
    private Vector2[] _centroids;

    private int[] _cellSeeds;
    private int _cellW;
    private int _cellH;
    private int _hoveredIdx = -1;

    private Texture2D _cityTex;
    private Texture2D _villageTex;



    public override void _Ready()
    {
        GD.Print("[MapView] _Ready start");
        var t0 = Time.GetTicksMsec();
        ModConsole.Init(this);

        _terrain = new Sprite2D();
        _terrain.Name = "Terrain";
        AddChild(_terrain);

        _territory = new Sprite2D();
        _territory.Name = "Territory";
        _territory.Modulate = new Color(1, 1, 1, 1f);
        _territory.TextureFilter = TextureFilterEnum.Linear;
        AddChild(_territory);

        _markerLayer = new Node2D();
        _markerLayer.Name = "Markers";
        AddChild(_markerLayer);

        _camera = new MapCamera();
        _camera.Name = "MapCamera";
        _camera.Zoom = new Vector2(0.25f, 0.25f);
        AddChild(_camera);
        _camera.MakeCurrent();

        var canvas = new CanvasLayer();
        canvas.Name = "UI";
        AddChild(canvas);

        var armyRenderer = new ArmyRenderer();
        armyRenderer.Name = "ArmyRenderer";
        AddChild(armyRenderer);

        var fog = new FogRenderer();
        fog.Name = "FogOfWar";
        fog.Init(null, MapWidth, MapHeight);
        AddChild(fog);

        var gameUI = new GameUI();
        gameUI.Name = "GameUI";
        canvas.AddChild(gameUI);

        var pauseMenu = new PauseMenu();
        pauseMenu.Name = "PauseMenu";
        pauseMenu.OpenSectPanel += () => canvas.GetNodeOrNull<SectPanel>("UI/SectPanel")?.Toggle();
        pauseMenu.OpenDiplomacy += () => canvas.GetNodeOrNull<DiplomacyPanel>("UI/DiplomacyPanel")?.Toggle();
        pauseMenu.OpenArmyCreator += () => canvas.GetNodeOrNull<ArmyCreator>("UI/ArmyCreator")?.Show();
        canvas.AddChild(pauseMenu);

        var sectPanel = new SectPanel();
        sectPanel.Name = "SectPanel";
        canvas.AddChild(sectPanel);

        var diploPanel = new DiplomacyPanel();
        diploPanel.Name = "DiplomacyPanel";
        canvas.AddChild(diploPanel);

        var armyCreator = new ArmyCreator();
        armyCreator.Name = "ArmyCreator";
        canvas.AddChild(armyCreator);

        var eventPopup = new EventPopup();
        eventPopup.Name = "EventPopup";
        canvas.AddChild(eventPopup);

        var toast = new Toast();
        toast.Name = "Toast";
        canvas.AddChild(toast);

        var warRenderer = new WarRenderer();
        warRenderer.Name = "WarRenderer";
        AddChild(warRenderer);

        _info = new Label();
        _info.Name = "Info";
        _info.Position = new Vector2(10, 10);
        _info.AddThemeFontSizeOverride("font_size", 14);
        _info.Modulate = new Color(1, 1, 1, 0.85f);
        canvas.AddChild(_info);

        _regenerateBtn = new Button();
        _regenerateBtn.Name = "RegenerateBtn";
        _regenerateBtn.Text = "Regenerate Map";
        _regenerateBtn.Position = new Vector2(10, 30);
        _regenerateBtn.Pressed += OnRegenerate;
        canvas.AddChild(_regenerateBtn);

        _tooltip = new Label();
        _tooltip.Name = "Tooltip";
        _tooltip.Hide();
        _tooltip.AddThemeFontSizeOverride("font_size", 16);
        _tooltip.Modulate = new Color(1, 1, 1, 0.95f);
        canvas.AddChild(_tooltip);

        // defer all heavy work to next frame so engine stays responsive
        Callable.From(() => {
            try { Generate(); }
            catch (System.Exception e) { GD.PrintErr($"[MapView] Generate: {e}\n{e.StackTrace}"); }
            try { InitGame(); }
            catch (System.Exception e) { GD.PrintErr($"[MapView] InitGame: {e}\n{e.StackTrace}"); }

            // load saved game if slot was specified
            if (!string.IsNullOrEmpty(LoadSlotOnStart))
            {
                var gm = GetNodeOrNull<GameManager>("GameManager");
                if (gm != null) SaveLoadManager.LoadGame(gm, LoadSlotOnStart);
                LoadSlotOnStart = null;
                OnLoadGame(); // regenerate visuals from loaded state
            }

            _camera.WorldW = MapWidth;
            _camera.WorldH = MapHeight;
            var gm2 = GetNodeOrNull<GameManager>("GameManager");
            if (gm2 != null)
            {
                var ps = gm2.State.PlayerSect;
                if (ps != null)
                {
                    var sl = gm2.Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == ps.Id);
                    if (sl != null) _camera.Position = sl.Position;
                    else _camera.Position = new Vector2(MapWidth / 2f, MapHeight / 2f);
                }
            }
            _camera.Zoom = new Vector2(3.0f, 3.0f);
            GD.Print($"[MapView] _Ready done in {Time.GetTicksMsec() - t0}ms");
        }).CallDeferred();
    }

    private void Generate()
    {
        foreach (var c in _markerLayer.GetChildren())
            c.QueueFree();

        _seed = (int)(GD.Randi() % 100000);
        GD.Print($"Generating map {MapWidth}x{MapHeight}");

        var t0 = Time.GetTicksMsec();
        ImageTexture tex = MapGenerator.GenerateTerrain(MapWidth, MapHeight, _seed);
        _terrain.Texture = tex;
        _terrain.Centered = false;
        _terrain.Scale = new Vector2(MapWidth, MapHeight);
        GD.Print($"  terrain: {Time.GetTicksMsec() - t0}ms");

        t0 = Time.GetTicksMsec();
        _locations = MapLocations.Generate(MapWidth, MapHeight, _seed);
        GD.Print($"  locations: {Time.GetTicksMsec() - t0}ms ({_locations.Count} locs)");

        t0 = Time.GetTicksMsec();
        PlaceMarkers();
        GD.Print($"  markers: {Time.GetTicksMsec() - t0}ms");

        t0 = Time.GetTicksMsec();
        ApplyTerritory();
        GD.Print($"  territory: {Time.GetTicksMsec() - t0}ms");

        _camera.WorldW = MapWidth;
        _camera.WorldH = MapHeight;

        GD.Print($"[MapView] _Ready done in {Time.GetTicksMsec() - t0}ms");
    }

    private void InitGame()
    {
        var gm = GetNodeOrNull<GameManager>("GameManager");
        if (gm == null)
        {
            gm = new GameManager();
            gm.Name = "GameManager";
            AddChild(gm);
        }

        gm.InitFromMapData(_locations);

        // create sects from map locations
        foreach (var loc in _locations)
        {
            if (loc.Type == LocationType.Sect)
            {
                var sd = new SectData
                {
                    Id = gm.State.Sects.Count,
                    Name = loc.Name,
                    IsPlayer = false,
                    Lingshi = 200,
                    Prestige = 50,
                    SpiritVein = 100,
                };
                gm.State.Sects.Add(sd);
                gm.State.SectMap[sd.Id] = sd;
            }
        }

        // pick a random sect as player
        int playerIdx = 0;
        if (gm.State.Sects.Count > 0)
        {
            playerIdx = (int)(GD.Randi() % gm.State.Sects.Count);
            gm.State.Sects[playerIdx].IsPlayer = true;
            GD.Print($"[MapView] 玩家宗门: {gm.State.Sects[playerIdx].Name}");
        }

        // assign ownership for all locations (same order as _locations)
        for (int i = 0; i < _locations.Count && i < gm.Locations.Count; i++)
        {
            int ownerIdx = _locations[i].OwnerIndex;
            if (ownerIdx >= 0 && ownerIdx < gm.State.Sects.Count)
            {
                int sid = gm.State.Sects[ownerIdx].Id;
                gm.Locations[i].OwnerSectId = sid;
                gm.Locations[i].AddInfluence(sid, 50);
                // track city ownership
                if (_locations[i].Type == LocationType.City)
                    gm.State.Sects[ownerIdx].ControlledCityIds.Add(gm.Locations[i].Id);
            }
        }

        // give each sect initial disciples (skip if home location not found)
        foreach (var sect in gm.State.Sects)
        {
            var home = gm.Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == sect.Id);
            if (home == null) continue;
            for (int di = 0; di < 4; di++)
                gm.CreateDisciple($"{sect.Name}弟子{di + 1}", sect.Id, home);
            sect.Lingshi = 200;
        }

        gm.InitSects(playerIdx);
        gm.StartGameLoop();

        // set up fog with game manager
        var fogNode = GetNodeOrNull<FogRenderer>("FogOfWar");
        if (fogNode != null) fogNode.Init(gm, MapWidth, MapHeight);

        GD.Print("[MapView] game systems initialized");
    }

    private void PlaceMarkers()
    {
        EnsureTextures();

        for (int i = 0; i < _locations.Count; i++)
        {
            var loc = _locations[i];
            var tex = loc.Type switch
            {
                LocationType.City => _cityTex,
                LocationType.Village => _villageTex,
                LocationType.Sect => SectTex(loc.OwnerIndex),
                _ => _cityTex
            };

            var pos = (_centroids != null && i < _centroids.Length) ? _centroids[i] : loc.Position;
            var spr = new Sprite2D();
            spr.Texture = tex;
            spr.Position = pos;
            spr.Centered = true;
            spr.ZIndex = 10;
            spr.Scale = loc.Type switch
            {
                LocationType.City => Vector2.One * 2.0f,
                LocationType.Village => Vector2.One * 1.0f,
                LocationType.Sect => Vector2.One * 1.5f,
                _ => Vector2.One
            };
            _markerLayer.AddChild(spr);
        }
    }

    private void ApplyTerritory()
    {
        if (_locations == null) return;
        var result = TerritoryMap.Generate(_locations, MapWidth, MapHeight);
        if (result == null) { _territory.Texture = null; return; }
        _territory.Texture = result.Texture;
        _territory.Centered = false;
        int cellSize = 8;
        _territory.Scale = new Vector2(cellSize, cellSize);
        _centroids = result.Centroids;
        _cellSeeds = result.CellSeeds;
        _cellW = result.CellW;
        _cellH = result.CellH;
    }

    private int SeedAtWorldPos(Vector2 worldPos)
    {
        if (_cellSeeds == null) return -1;
        // wrap into [0, MapW)
        float wx = worldPos.X;
        while (wx < 0) wx += MapWidth;
        while (wx >= MapWidth) wx -= MapWidth;
        float wy = worldPos.Y;
        while (wy < 0) wy += MapHeight;
        while (wy >= MapHeight) wy -= MapHeight;
        int cx = (int)(wx / 8f);
        int cy = (int)(wy / 8f);
        cx = Mathf.Clamp(cx, 0, _cellW - 1);
        cy = Mathf.Clamp(cy, 0, _cellH - 1);
        return _cellSeeds[cy * _cellW + cx];
    }

    private static Texture2D[] _sectTexs = new Texture2D[0];

    private void EnsureTextures()
    {
        if (_cityTex == null) _cityTex = AssetLoader.MarkerCity;
        if (_villageTex == null) _villageTex = AssetLoader.MarkerVillage;
        if (_sectTexs.Length == 0)
            _sectTexs = new[] {
                AssetLoader.MarkerSectRed,
                AssetLoader.MarkerSectGreen,
                AssetLoader.MarkerSectPurple,
                AssetLoader.MarkerSect,
            };
    }

    private Texture2D SectTex(int ownerIdx)
    {
        if (_sectTexs == null || _sectTexs.Length == 0) return AssetLoader.MarkerSect;
        return _sectTexs[ownerIdx % _sectTexs.Length];
    }

    private ulong _lastFrameTime;
    private float _eventCheckTimer;

    public override void _Process(double delta)
    {
        var now = Time.GetTicksMsec();
        if (_lastFrameTime > 0 && now - _lastFrameTime > 200)
            GD.Print($"[PERF] Frame took {now - _lastFrameTime}ms");
        _lastFrameTime = now;

        _info.Text = $"Seed: {_seed}  |  城{CountType(LocationType.City)} 村{CountType(LocationType.Village)} 宗{CountType(LocationType.Sect)}  |  {MapWidth}x{MapHeight}";

        // skip hover if any modal overlay is open
        bool paused = GetNodeOrNull<PauseMenu>("UI/PauseMenu")?.Visible == true
            || GetNodeOrNull<DiplomacyPanel>("UI/DiplomacyPanel")?.Visible == true
            || GetNodeOrNull<SectPanel>("UI/SectPanel")?.Visible == true
            || GetNodeOrNull<ArmyCreator>("UI/ArmyCreator")?.Visible == true
            || GetNodeOrNull<EventPopup>("UI/EventPopup")?.Visible == true;

        // territory-based hover
        Vector2 mouseWorld = GetGlobalMousePosition();
        int seedIdx = SeedAtWorldPos(mouseWorld);
        if (seedIdx < 0 || seedIdx >= _locations.Count) seedIdx = -1;

        if (paused)
        {
            if (_tooltip.Visible) _tooltip.Hide();
        }
        else if (seedIdx != _hoveredIdx)
        {
            _hoveredIdx = seedIdx;
            if (_hoveredIdx >= 0)
                ShowTooltip(_locations[_hoveredIdx]);
            else
                _tooltip.Hide();
        }

        if (!paused && _tooltip.Visible && _hoveredIdx >= 0)
        {
            Vector2 pos = LocationPos(_hoveredIdx);
            Vector2 screen = _camera.GetCanvasTransform() * pos;
            _tooltip.Position = screen + new Vector2(-_tooltip.Size.X - 10, -10);
        }

        // check pending events
        _eventCheckTimer += (float)delta;
        if (_eventCheckTimer > 0.5f)
        {
            _eventCheckTimer = 0;
            var gm = GetNodeOrNull<GameManager>("GameManager");
            if (gm != null && gm.PendingEvents.TryDequeue(out var ev))
            {
                var popup = GetNodeOrNull<EventPopup>("UI/EventPopup");
                if (popup != null)
                {
                    var options = new System.Collections.Generic.List<(string, System.Action)>();
                    options.Add(("确定", ev.callback));
                    popup.ShowEvent(ev.title, ev.desc, options);
                }
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed)
        {
            if (k.Keycode == Key.Escape)
            {
                if (GetNodeOrNull<PauseMenu>("UI/PauseMenu")?.Visible == true) { GetNodeOrNull<PauseMenu>("UI/PauseMenu")?.Toggle(); return; }
                if (GetNodeOrNull<SectPanel>("UI/SectPanel")?.Visible == true) { GetNodeOrNull<SectPanel>("UI/SectPanel")?.Toggle(); return; }
                if (GetNodeOrNull<DiplomacyPanel>("UI/DiplomacyPanel")?.Visible == true) { GetNodeOrNull<DiplomacyPanel>("UI/DiplomacyPanel")?.Toggle(); return; }
                if (GetNodeOrNull<ArmyCreator>("UI/ArmyCreator")?.Visible == true) { GetNodeOrNull<ArmyCreator>("UI/ArmyCreator")?.Hide(); return; }
                GetNodeOrNull<PauseMenu>("UI/PauseMenu")?.Toggle();
            }

            if (k.Keycode == Key.D) { var dp = GetNodeOrNull<DiplomacyPanel>("UI/DiplomacyPanel"); if (dp != null && !dp.Visible) dp.Toggle(); }
            if (k.Keycode == Key.A) GetNodeOrNull<ArmyCreator>("UI/ArmyCreator")?.Show();
            if (k.Keycode == Key.L) GetNodeOrNull<GameUI>("UI/GameUI")?.ToggleLog();
        }
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            Vector2 mouseWorld = GetGlobalMousePosition();
            int seedIdx = SeedAtWorldPos(mouseWorld);
            var ui = GetNodeOrNull<GameUI>("UI/GameUI");

            if (mb.ButtonIndex == MouseButton.Right)
            {
                if (seedIdx >= 0 && seedIdx < _locations.Count)
                {
                    var loc = _locations[seedIdx];
                    var items = new List<(string, System.Action)>();
                    items.Add(($"查看", () => ShowDebugInfo(loc)));
                    var gm = GetNodeOrNull<GameManager>("GameManager");

                    if (loc.Type == LocationType.City && gm != null)
                    {
                        var ld = gm.Locations.FirstOrDefault(l => l.Name == loc.Name);
                        if (ld != null)
                        {
                            var playerSect = gm.State.PlayerSect;
                            if (playerSect != null)
                            {
                                items.Add(($"驻守弟子", () =>
                                {
                                    var idle = gm.State.Disciples.FirstOrDefault(d => d.SectId == playerSect.Id && d.State == "idle" && !gm.Armies.Any(a => a.DiscipleIds.Contains(d.Id)));
                                    if (idle != null)
                                    {
                                        ld.AddInfluence(playerSect.Id, 10);
                                        gm.State.Log($"{idle.Name} 驻守 {loc.Name}");
                                    }
                                }));
                                items.Add(($"援助物资", () =>
                                {
                                    if (playerSect.Lingshi >= 50)
                                    {
                                        playerSect.Lingshi -= 50;
                                        ld.Prosperity += 5;
                                        ld.AddInfluence(playerSect.Id, 15);
                                        gm.State.Log($"援助 {loc.Name} (+5繁荣, +15影响力)");
                                    }
                                }));
                                items.Add(($"收徒", () =>
                                {
                                    var idle = gm.State.Disciples.FirstOrDefault(d => d.SectId == playerSect.Id && d.State == "idle" && !gm.Armies.Any(a => a.DiscipleIds.Contains(d.Id)));
                                    if (idle != null)
                                    {
                                        ld.AddInfluence(playerSect.Id, 5);
                                        // recruit from this location
                                        gm.CreateDisciple($"新弟子{gm.State.NextDiscipleId}", playerSect.Id, ld);
                                        gm.State.Log($"从 {loc.Name} 收徒");
                                    }
                                }));
                            }
                        }
                    }
                    if (loc.Type == LocationType.Sect)
                    {
                        var sd = gm?.State.GetSect(loc.OwnerIndex);
                        if (sd != null)
                            items.Add(($"宗门详情", () => ui?.ShowSectInfo(sd)));
                    }
                    items.Add(("关闭", () => {}));
                    ui?.ShowContextMenu(GetViewport().GetMousePosition(), items);
                }
                else
                {
                    var items = new List<(string, System.Action)>
                    {
                        ("全局信息", () => ShowGlobalInfo()),
                        ("关闭", () => {})
                    };
                    ui?.ShowContextMenu(GetViewport().GetMousePosition(), items);
                }
            }
            else if (mb.ButtonIndex == MouseButton.Left && !_camera.WasLeftDrag() && seedIdx >= 0 && seedIdx < _locations.Count)
            {
                OnLocationClicked(_locations[seedIdx]);
            }
        }
    }

    private void ShowDebugInfo(MapLocation loc)
    {
        var gm = GetNodeOrNull<GameManager>("GameManager");
        var ui = GetNodeOrNull<GameUI>("UI/GameUI");
        if (gm == null || ui == null) return;
        ui.ShowLocationInfo(loc);
        var ld = gm.Locations.FirstOrDefault(l => l.Name == loc.Name);
        if (ld != null)
        {
            var sect = gm.State.GetSect(ld.OwnerSectId);
            if (sect != null) ui.ShowSectInfo(sect);
        }
    }

    private void ShowGlobalInfo()
    {
        var gm = GetNodeOrNull<GameManager>("GameManager");
        var ui = GetNodeOrNull<GameUI>("UI/GameUI");
        if (gm == null || ui == null) return;
        ui._bottomPanel.Visible = true;
        string info = $"[b]全局状态[/b]\n";
        info += $"时间: 第{gm.State.Year}年 {gm.State.Month}月 ({gm.State.Xun}/36旬)\n";
        info += $"宗门: {gm.State.Sects.Count(s => s.IsAlive)}存活  战争: {gm.Wars.Count(w => !w.Ended)}\n";
        info += $"部队: {gm.Armies.Count}  弟子: {gm.State.Disciples.Count}\n";
        info += $"D:外交  A:编队  L:日志  ESC:暂停\n";
        GetNodeOrNull<GameUI>("UI/GameUI")._bottomInfo.Text = info;
    }

    private Vector2 LocationPos(int idx)
    {
        if (_centroids != null && idx < _centroids.Length)
            return _centroids[idx];
        return _locations[idx].Position;
    }

    public void HideTooltip() { _tooltip.Hide(); _hoveredIdx = -1; }

    private void ShowTooltip(MapLocation loc)
    {
        _tooltip.Text = $"{loc.Name} ({loc.Type})";
        if (loc.Type != LocationType.Sect)
        {
            _tooltip.Text += $"\n人口: {loc.Population}";
            if (loc.OwnerIndex >= 0 && loc.OwnerIndex < _locations.Count)
                _tooltip.Text += $"\n所属: {_locations[loc.OwnerIndex].Name}";
        }
        _tooltip.Show();
    }

    private void OnLocationClicked(MapLocation loc)
    {
        GD.PrintRich($"[color=#ffcc00]点击: {loc.Name}[/color] 类型={loc.Type} 位置=({(int)loc.Position.X}, {(int)loc.Position.Y})");
    }

    private void OnRegenerate()
    {
        Generate();
    }

    public void OnLoadGame()
    {
        var gm = GetNodeOrNull<GameManager>("GameManager");
        if (gm == null) return;
        _seed = gm.State.Seed;

        // rebuild _locations from loaded gm.Locations
        foreach (var c in _markerLayer.GetChildren())
            c.QueueFree();
        _locations = new List<MapLocation>();
        foreach (var ld in gm.Locations)
        {
            var ml = new MapLocation(ld.Position, ld.Name, ld.Type, ld.Population);
            // set OwnerIndex from the loaded data
            for (int i = 0; i < gm.State.Sects.Count; i++)
            {
                if (gm.State.Sects[i].Id == ld.OwnerSectId)
                { ml.OwnerIndex = i; break; }
            }
            _locations.Add(ml);
        }
        PlaceMarkers();
        ApplyTerritory();

        var ps = gm.State.PlayerSect;
        if (ps != null)
        {
            var sl = gm.Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == ps.Id);
            if (sl != null) _camera.Position = sl.Position;
        }
        _camera.Zoom = new Vector2(3.0f, 3.0f);
        GD.Print("[MapView] Load complete");
    }

    private int CountType(LocationType t)
    {
        if (_locations == null) return 0;
        int n = 0;
        foreach (var l in _locations)
            if (l.Type == t) n++;
        return n;
    }
}
