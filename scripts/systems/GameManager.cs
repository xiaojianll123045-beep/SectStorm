using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
    public static GameManager Instance;
    public GameState State;
    public bool AiProcessing;
    public int AiProgress;
    public int AiTotal;
    public List<LocationData> Locations = new();
    public List<ArmyData> Armies = new();
    public List<WarData> Wars = new();

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
    }

    public void InitSects(int playerSectId)
    {
        State.PlayerSectId = playerSectId;
        for (int i = 0; i < State.Sects.Count; i++)
            for (int j = i + 1; j < State.Sects.Count; j++)
            {
                int a = State.Sects[i].Id, b = State.Sects[j].Id;
                var rel = new RelationData(a, b);
                State.Relations.Add(rel);
                State.RelationMap[(a, b)] = rel;
            }
    }

    private bool _turnRunning;

    public void StartGameLoop()
    {
        _turnTimer = new Timer();
        _turnTimer.OneShot = true;
        _turnTimer.WaitTime = 0.001f; // debug: no wait between turns
        _turnTimer.Timeout += () => {
            if (_turnRunning) {
                GD.Print("[Game] TURN SKIPPED (still running)");
                _turnTimer.Start(0.1f);
                return;
            }
            _turnRunning = true;
            var t0 = Time.GetTicksMsec();
            try { ProcessTurn(); }
            catch (System.Exception e) { GD.PrintErr($"[Game] Turn error: {e.Message}"); }
            int dt = (int)(Time.GetTicksMsec() - t0);
            if (dt > 200) GD.Print($"[Game] Turn took {dt}ms");
            _turnRunning = false;
            _turnTimer.Start();
        };
        AddChild(_turnTimer);
        _turnTimer.Start();
        State.Log("游戏开始");
    }

    private void ProcessTurn()
    {
        GD.Print($"=== Turn {State.TotalTurns} start ===");
        State.AdvanceTurn();
        // costs
        foreach (var army in Armies)
        {
            if (!army.IsAlive) continue;
            var sect = State.GetSect(army.SectId);
            if (sect == null) continue;
            float cost = army.Count * 2f;
            if (sect.Lingshi < cost || sect.SpiritVein < 1)
                army.TurnsWithoutSupply++;
            else
            {
                sect.Lingshi -= cost;
                army.TurnsWithoutSupply = 0;
            }
        }

        // process army movement with territory restrictions
        foreach (var army in Armies)
        {
            if (!army.IsAlive) continue;
            if (army.Order != ArmyOrder.Moving && army.Order != ArmyOrder.Attacking) continue;

            // determine target position
            Vector2 targetPos = army.Order == ArmyOrder.Moving ? army.MoveTarget :
                (army.AttackTargetArmyId >= 0 ? Armies.FirstOrDefault(a => a.Id == army.AttackTargetArmyId)?.Position ?? army.Position : army.Position);

            if (army.Order == ArmyOrder.Attacking)
            {
                var t = Armies.FirstOrDefault(a => a.Id == army.AttackTargetArmyId);
                if (t == null || !t.IsAlive) { army.Order = ArmyOrder.Idle; continue; }
                targetPos = t.Position;
            }

            // check if target is valid territory
            float dist = (targetPos - army.Position).Length();
            if (dist <= 5f)
            {
                if (army.Order == ArmyOrder.Moving) army.Order = ArmyOrder.Idle;
                else if (army.Order == ArmyOrder.Attacking) ResolveArmyBattle(army, Armies.First(a => a.Id == army.AttackTargetArmyId));
                continue;
            }

            // calculate step
            Vector2 step = (targetPos - army.Position).Normalized() * 100f;
            if (step.Length() > dist) step = targetPos - army.Position;
            Vector2 newPos = army.Position + step;

            // block entry to neutral/unauthorized territory
            int newOwner = OwnerAtPosition(newPos);
            bool canEnter = (newOwner == army.SectId || newOwner < 0);
            if (!canEnter && newOwner >= 0)
            {
                var rel = State.GetRelation(army.SectId, newOwner);
                canEnter = (rel != null && (rel.State == RelationState.Ally || rel.State == RelationState.War));
            }
            if (!canEnter) continue; // blocked at border

            army.Position = newPos;
        }

        // disciples: idle cultivation or army attrition
        foreach (var d in State.Disciples)
        {
            if (d.SectId < 0) continue;
            // check if in an army
            bool inArmy = Armies.Any(a => a.DiscipleIds.Contains(d.Id));
            if (!inArmy && d.State == "idle")
            {
                var sect = State.GetSect(d.SectId);
                if (sect != null)
                {
                    d.CultivationProgress += (int)(5 * sect.BreakthroughBonus());
                    if (d.CultivationProgress >= 100)
                    {
                        d.CultivationProgress = 0;
                        if (TryBreakthrough(d, sect))
                            State.Log($"{d.Name} 突破！");
                    }
                }
                d.Lifespan--;
                if (d.Lifespan <= 0)
                {
                    State.Disciples.Remove(d);
                    State.Log($"{d.Name} 陨落");
                }
            }
        }

        // city income
        foreach (var loc in Locations)
        {
            if (loc.Type != LocationType.City) continue;
            var sect = State.GetSect(loc.OwnerSectId);
            if (sect == null) continue;
            float share = loc.GetInfluence(loc.OwnerSectId) / 100f;
            float income = loc.TaxBase * share * 0.1f;
            sect.Lingshi += income;
        }

        // wars
        foreach (var war in Wars.ToList())
        {
            if (war.Ended) continue;
            WarSystem.ProcessWarTurn(war, State, Locations);
            if (war.Ended) Wars.Remove(war);
        }

        // AI (main thread, with progress)
        if (State.TotalTurns % 9 == 0)
        {
            GD.Print($"  AI start...");
            AiProcessing = true;
            AiTotal = _ai.BatchCount();
            AiProgress = 0;
            try
            {
                var t0 = Time.GetTicksMsec();
                _ai.ProcessAllAi();
                GD.Print($"  AI done ({Time.GetTicksMsec() - t0}ms)");
            }
            catch (System.Exception e)
            {
                GD.PrintErr($"[Game] AI error: {e.Message}\n{e.StackTrace}");
            }
            AiProgress = AiTotal;
            AiProcessing = false;
        }

        // AI disciple recruitment (every 3 turns)
        if (State.TotalTurns % 3 == 0)
        {
            foreach (var sect in State.Sects)
            {
                if (!sect.IsAlive) continue;
                int current = State.Disciples.Count(d => d.SectId == sect.Id);
                int max = sect.MaxDisciples();
                if (current < max && sect.Lingshi > 50)
                {
                    var home = Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == sect.Id);
                    if (home != null)
                    {
                        CreateDisciple($"{sect.Name}弟子{current + 1}", sect.Id, home);
                        sect.Lingshi -= 30;
                    }
                }
            }
        }

        GD.Print($"=== Turn {State.TotalTurns} done ===");
    }

    private void ResolveArmyBattle(ArmyData atk, ArmyData def)
    {
        var result = BattleSystem.Resolve(new List<ArmyData>{atk}, new List<ArmyData>{def}, 0, 0);
        State.Log($"战斗: {atk.Name} vs {def.Name}, 攻方胜={result.AttackerWon}, 攻方损失{result.AtKilled}, 守方损失{result.DefKilled}");

        var war = Wars.FirstOrDefault(w =>
            (w.AttackerSectId == atk.SectId && w.DefenderSectId == def.SectId) ||
            (w.AttackerSectId == def.SectId && w.DefenderSectId == atk.SectId));
        if (war != null)
            war.ScoreFromBattles += result.AtKilled + result.DefKilled;

        // attacker wins → defender routed (removed even if survivors)
        if (result.AttackerWon)
        {
            Armies.Remove(def);
            if (!atk.IsAlive) Armies.Remove(atk);
        }
        else
        {
            Armies.Remove(atk);
            if (!def.IsAlive) Armies.Remove(def);
        }
    }

    private bool TryBreakthrough(DiscipleData d, SectData sect)
    {
        float chance = 0.3f + d.Mood * 0.002f + sect.BreakthroughBonus() * 0.1f;
        if (d.Mood < 20) chance *= 0.5f;
        if (GD.Randf() < chance)
        {
            d.SubRealm++;
            if (d.SubRealm > 2)
            {
                d.SubRealm = 0;
                d.Realm = (Realm)((int)d.Realm + 1);
                d.Lifespan += (int)d.Realm * 100;
            }
            return true;
        }
        d.Mood -= 10;
        return false;
    }

    public int OwnerAtPosition(Vector2 pos)
    {
        float bestDist = float.MaxValue;
        int bestOwner = -1;
        // check a subset of nearby locations for efficiency
        int checkCount = Mathf.Min(100, Locations.Count);
        for (int i = 0; i < checkCount; i++)
        {
            int idx = (int)((pos.X + pos.Y * 13) % Locations.Count + i) % Locations.Count;
            var loc = Locations[idx];
            if (loc.OwnerSectId < 0) continue;
            float dx = pos.X - loc.Position.X;
            float dy = pos.Y - loc.Position.Y;
            float d = dx * dx + dy * dy;
            if (d < bestDist) { bestDist = d; bestOwner = loc.OwnerSectId; }
        }
        return bestOwner;
    }

    public int CreateDisciple(string name, int sectId, LocationData source)
    {
        var d = new DiscipleData(State.NextDiscipleId++, name);
        d.SectId = sectId;
        d.SectId = sectId;
        float quality = source.Prosperity / 100f;
        d.Combat = (int)(d.Combat * (0.5f + quality));
        d.CultivationProgress = (int)(quality * 20);
        State.Disciples.Add(d);
        return d.Id;
    }

    public ArmyData CreateArmy(int sectId, List<int> discipleIds, Vector2 pos)
    {
        var army = new ArmyData
        {
            Id = Armies.Count + 1,
            Name = $"部队{Armies.Count + 1}",
            SectId = sectId,
            DiscipleIds = new List<int>(discipleIds),
            Position = pos,
            ResolveDisciple = (id) => State.Disciples.FirstOrDefault(d => d.Id == id),
        };
        Armies.Add(army);
        return army;
    }

    public void DisbandArmy(ArmyData army)
    {
        Armies.Remove(army);
    }

    public WarData DeclareWar(int attackerId, int defenderId)
    {
        var war = WarSystem.DeclareWar(attackerId, defenderId);
        Wars.Add(war);
        var atk = State.GetSect(attackerId);
        var def = State.GetSect(defenderId);
        State.Log($"{atk?.Name} 对 {def?.Name} 宣战！");
        return war;
    }
}
