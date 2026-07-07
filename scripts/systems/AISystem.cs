using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class AISystem
{
    private GameManager _gm;
    private Random _rng = new();
    private Dictionary<int, List<LocationData>> _cachedLocs = new();

    public AISystem(GameManager gm) => _gm = gm;

    public int BatchCount() => _gm.State.AiSects.Count();

    public void ProcessAllAi()
    {
        _cachedLocs.Clear();
        int done = 0, total = _gm.State.AiSects.Count();
        foreach (var sect in _gm.State.AiSects.ToList())
        {
            if (!sect.IsAlive) continue;
            if (sect.LastDecisionTurn == _gm.State.TotalTurns) continue;
            sect.LastDecisionTurn = _gm.State.TotalTurns;

            try
            {
                var myWar = _gm.Wars.FirstOrDefault(w =>
                    (w.AttackerSectId == sect.Id || w.DefenderSectId == sect.Id) && !w.Ended);
                if (myWar != null)
                    ProcessWarAI(sect, myWar);
                else
                    ProcessPeaceAI(sect);
            }
            catch (Exception e)
            {
                GD.PrintErr($"[AI] Error processing {sect.Name}: {e.Message}");
            }

            done++;
            _gm.AiProgress = done;
            if (done % 50 == 0)
                System.Threading.Thread.Sleep(1); // let UI breathe
        }
        GD.Print($"  [AI] done {done}/{total} sects");
    }

    private void ProcessPeaceAI(SectData sect)
    {
        var enemies = FindEnemies(sect);
        var targets = new List<(SectData sect, float score)>();

        foreach (var e in enemies)
        {
            if (InWar(e.Id)) continue;
            int myPower = EstimatePower(sect);
            int ePower = EstimatePower(e);
            float score = ePower > 0 ? (float)myPower / ePower : 3f;
            if (SharesBorderCached(sect, e)) score *= 1.5f;
            targets.Add((e, score));
        }

        var best = targets.Where(t => t.score > 1.3f).OrderByDescending(t => t.score).FirstOrDefault();
        if (best.sect != null)
        {
            bool aggr = sect.Personality == AIPersonality.Aggressive || _rng.Next(3) == 0;
            if (aggr)
            {
                _gm.DeclareWar(sect.Id, best.sect.Id);
                CreateAIResponseArmy(sect);
            }
        }
    }

    private void ProcessWarAI(SectData sect, WarData war)
    {
        int enemyId = war.AttackerSectId == sect.Id ? war.DefenderSectId : war.AttackerSectId;
        if (!HasArmy(sect.Id)) CreateAIResponseArmy(sect);
        if (!HasArmy(sect.Id) || HasOrder(sect.Id, ArmyOrder.Attacking)) return;

        var targetArmy = GetArmy(enemyId);
        if (targetArmy != null)
        {
            var army = GetArmy(sect.Id);
            if (army != null)
            {
                army.AttackTargetArmyId = targetArmy.Id;
                army.Order = ArmyOrder.Attacking;
            }
        }
    }

    private List<SectData> FindEnemies(SectData sect)
    {
        var result = new List<SectData>();
        var checkedOwners = new HashSet<int>();

        foreach (var other in _gm.State.Sects)
        {
            if (other.Id == sect.Id || !other.IsAlive || other.IsPlayer) continue;
            var rel = _gm.State.GetRelation(sect.Id, other.Id);
            if (rel != null && rel.Favor < -20) result.Add(other);
        }

        // cache locs by owner
        var locsByOwner = new Dictionary<int, List<LocationData>>();
        foreach (var loc in _gm.Locations)
        {
            if (loc.OwnerSectId < 0) continue;
            if (!locsByOwner.ContainsKey(loc.OwnerSectId))
                locsByOwner[loc.OwnerSectId] = new List<LocationData>();
            locsByOwner[loc.OwnerSectId].Add(loc);
        }

        float rangeSq = 500f * 500f;
        var myLocs = locsByOwner.TryGetValue(sect.Id, out var ml) ? ml : new List<LocationData>();

        foreach (var kv in locsByOwner)
        {
            if (kv.Key == sect.Id || checkedOwners.Contains(kv.Key)) continue;
            checkedOwners.Add(kv.Key);
            var rel = _gm.State.GetRelation(sect.Id, kv.Key);
            if (rel != null && rel.State == RelationState.Ally) continue;

            bool border = false;
            foreach (var al in myLocs)
                foreach (var bl in kv.Value)
                {
                    float dx = al.Position.X - bl.Position.X;
                    float dy = al.Position.Y - bl.Position.Y;
                    if (dx * dx + dy * dy < rangeSq) { border = true; break; }
                }
            if (border)
            {
                var other = _gm.State.GetSect(kv.Key);
                if (other != null && !result.Contains(other)) result.Add(other);
            }
        }
        return result;
    }

    private bool SharesBorderCached(SectData a, SectData b)
    {
        if (a == null || b == null) return false;
        if (!_cachedLocs.TryGetValue(a.Id, out var aLocs))
            _cachedLocs[a.Id] = aLocs = _gm.Locations.Where(l => l.OwnerSectId == a.Id).ToList();
        if (!_cachedLocs.TryGetValue(b.Id, out var bLocs))
            _cachedLocs[b.Id] = bLocs = _gm.Locations.Where(l => l.OwnerSectId == b.Id).ToList();
        float rangeSq = 500f * 500f;
        foreach (var al in aLocs)
            foreach (var bl in bLocs)
            {
                float dx = al.Position.X - bl.Position.X;
                float dy = al.Position.Y - bl.Position.Y;
                if (dx * dx + dy * dy < rangeSq) return true;
            }
        return false;
    }

    private int EstimatePower(SectData sect) =>
        sect.ControlledCityIds.Count * 10
        + _gm.State.Disciples.Count(d => d.SectId == sect.Id) * 5
        + _gm.Armies.Where(a => a.SectId == sect.Id).Sum(a => a.EffectiveCombat);

    private bool InWar(int sId) => _gm.Wars.Any(w => (w.AttackerSectId == sId || w.DefenderSectId == sId) && !w.Ended);
    private bool HasArmy(int sId) => _gm.Armies.Any(a => a.SectId == sId && a.IsAlive);
    private ArmyData GetArmy(int sId) => _gm.Armies.FirstOrDefault(a => a.SectId == sId && a.IsAlive);
    private bool HasOrder(int sId, ArmyOrder o) => _gm.Armies.Any(a => a.SectId == sId && a.Order == o);

    private void CreateAIResponseArmy(SectData sect)
    {
        var disciples = _gm.State.Disciples.Where(d => d.SectId == sect.Id && d.State == "idle").Take(5).ToList();
        if (disciples.Count == 0) return;
        var home = _gm.Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == sect.Id);
        if (home == null) return;
        _gm.CreateArmy(sect.Id, disciples.Select(d => d.Id).ToList(), home.Position);
    }
}
