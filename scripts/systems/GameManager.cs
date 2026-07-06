using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
    public static GameManager Instance;
    public GameState State;
    public List<LocationData> Locations = new();

    private Timer _turnTimer;
    private AISystem _ai;

    public override void _Ready()
    {
        Instance = this;
        State = new GameState { Seed = (int)(GD.Randi() % 100000) };
        _ai = new AISystem(this);
        GD.Print("[GameManager] ready");
    }

    public void InitFromMapData(List<MapLocation> mapLocs)
    {
        int id = 0;
        foreach (var ml in mapLocs)
        {
            var loc = new LocationData(id++, ml.Name, ml.Type, ml.Position, ml.Population);
            Locations.Add(loc);
        }
        GD.Print($"[GameManager] initialized {Locations.Count} locations");
    }

    public void InitSects(int playerSectId)
    {
        State.PlayerSectId = playerSectId;
        // init relations
        for (int i = 0; i < State.Sects.Count; i++)
            for (int j = i + 1; j < State.Sects.Count; j++)
                State.Relations.Add(new RelationData(State.Sects[i].Id, State.Sects[j].Id));
        GD.Print($"[GameManager] {State.Sects.Count} sects, {State.Relations.Count} relations");
    }

    public void StartGameLoop()
    {
        _turnTimer = new Timer();
        _turnTimer.WaitTime = 0.5f; // 0.5s per turn for dev
        _turnTimer.Timeout += ProcessTurn;
        AddChild(_turnTimer);
        _turnTimer.Start();
        State.Log("游戏开始");
    }

    private void ProcessTurn()
    {
        State.AdvanceTurn();

        // 1. disciple cultivation
        foreach (var d in State.Disciples)
        {
            if (d.SectId < 0 || d.State != "idle") continue;
            var sect = State.GetSect(d.SectId);
            if (sect == null) continue;
            d.CultivationProgress += (int)(5 * sect.BreakthroughBonus());
            if (d.CultivationProgress >= 100)
            {
                d.CultivationProgress = 0;
                if (TryBreakthrough(d, sect))
                    State.Log($"{d.Name} 突破至 {d.Realm}后期！");
            }
            d.Lifespan--;
            if (d.Lifespan <= 0)
            {
                State.Disciples.Remove(d);
                State.Log($"{d.Name} 寿元耗尽，陨落");
            }
        }

        // 2. city income
        foreach (var loc in Locations)
        {
            if (loc.Type != LocationType.City) continue;
            var sect = State.GetSect(loc.OwnerSectId);
            if (sect == null) continue;
            float share = loc.GetInfluence(loc.OwnerSectId) / 100f;
            float income = loc.TaxBase * share * 0.1f;
            sect.Lingshi += income;
        }

        // 3. village crop income
        foreach (var loc in Locations)
        {
            if (loc.Type != LocationType.Village) continue;
            var sect = State.GetSect(loc.OwnerSectId);
            if (sect == null) continue;
            // village gives small passive influence growth
            loc.AddInfluence(loc.OwnerSectId, 0.1f);
        }

        // 4. disciple task progress
        foreach (var d in State.Disciples)
        {
            if (d.State != "task") continue;
            d.TaskTurnsLeft--;
            if (d.TaskTurnsLeft <= 0)
            {
                d.State = "idle";
                d.TaskTargetId = -1;
                // task complete logic handled elsewhere
            }
        }

        // 5. AI decisions (every 9 turns = quarterly)
        if (State.TotalTurns % 9 == 0)
            _ai.ProcessAllAi();

        // 6. passive events every ~10 turns
        if (GD.RandRange(0, 10) == 0)
            TryGenerateEvent();
    }

    private bool TryBreakthrough(DiscipleData d, SectData sect)
    {
        float chance = 0.3f + d.Mood * 0.002f + d.Loyalty * 0.001f + sect.BreakthroughBonus() * 0.1f;
        if (d.Mood < 20) chance *= 0.5f;
        if (GD.Randf() < chance)
        {
            d.SubRealm++;
            if (d.SubRealm > 2) // late->next realm
            {
                d.SubRealm = 0;
                d.Realm = (Realm)((int)d.Realm + 1);
                d.Lifespan += (int)d.Realm * 100;
            }
            return true;
        }
        else
        {
            d.Mood -= 10;
            if (GD.Randf() < 0.15f)
            {
                d.State = "heal";
                State.Log($"{d.Name} 走火入魔，需疗伤");
            }
            return false;
        }
    }

    private void TryGenerateEvent()
    {
        // pick a random location
        if (Locations.Count == 0) return;
        var loc = Locations[(int)(GD.Randi() % Locations.Count)];
        if (loc.Status != "normal") return;

        float roll = GD.Randf();
        if (roll < 0.3f)
        {
            loc.Status = "disaster";
            loc.Prosperity -= 10;
            State.Log($"{loc.Name} 发生天灾，繁荣度下降");
        }
        else if (roll < 0.5f)
        {
            loc.Prosperity += 5;
            State.Log($"{loc.Name} 丰收，繁荣度上升");
        }
    }

    public int CreateDisciple(string name, int sectId, LocationData source)
    {
        var d = new DiscipleData(State.NextDiscipleId++, name);
        d.SectId = sectId;
        // quality based on source prosperity
        float quality = source.Prosperity / 100f;
        d.Combat = (int)(d.Combat * (0.5f + quality));
        d.Alchemy = (int)(d.Alchemy * (0.5f + quality));
        d.CultivationProgress = (int)(quality * 20);
        State.Disciples.Add(d);
        return d.Id;
    }
}
