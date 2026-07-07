using Godot;
using System.Collections.Generic;
using System.Linq;

public class AISystem
{
    private GameManager _gm;
    private System.Random _rng = new();

    public AISystem(GameManager gm) => _gm = gm;

    public void ProcessAllAi()
    {
        foreach (var sect in _gm.State.AiSects.ToList())
        {
            if (!sect.IsAlive) continue;
            if (sect.LastDecisionTurn == _gm.State.TotalTurns) continue;
            sect.LastDecisionTurn = _gm.State.TotalTurns;

            // check if already in a war
            var myWar = _gm.Wars.FirstOrDefault(w =>
                (w.AttackerSectId == sect.Id || w.DefenderSectId == sect.Id) && !w.Ended);

            if (myWar != null)
                ProcessWarAI(sect, myWar);
            else
                ProcessPeaceAI(sect);
        }
    }

    private void ProcessPeaceAI(SectData sect)
    {
        var enemies = FindEnemies(sect);
        var targets = new List<(SectData sect, float score)>();

        foreach (var e in enemies)
        {
            if (_gm.Wars.Any(w => (w.AttackerSectId == e.Id || w.DefenderSectId == e.Id))) continue;

            int myPower = EstimatePower(sect);
            int ePower = EstimatePower(e);
            float score = ePower > 0 ? (float)myPower / ePower : 3f;

            // bonus for adjacent territory
            if (SharesBorder(sect, e)) score *= 1.5f;
            targets.Add((e, score));
        }

        // pick best target with score > 1.3 (we're stronger)
        var best = targets.Where(t => t.score > 1.3f).OrderByDescending(t => t.score).FirstOrDefault();

        if (best.sect != null)
        {
            // declare war!
            bool aggressive = sect.Personality == AIPersonality.Aggressive;
            bool balanced = sect.Personality == AIPersonality.Balanced && _rng.Next(2) == 0;
            if (aggressive || balanced)
            {
                _gm.DeclareWar(sect.Id, best.sect.Id);
                // create a small army
                CreateAIResponseArmy(sect);
            }
        }
    }

    private void ProcessWarAI(SectData sect, WarData war)
    {
        int enemyId = war.AttackerSectId == sect.Id ? war.DefenderSectId : war.AttackerSectId;
        var enemy = _gm.State.GetSect(enemyId);
        if (enemy == null) return;

        // respond with army if none exists
        if (!_gm.Armies.Any(a => a.SectId == sect.Id))
            CreateAIResponseArmy(sect);

        var myArmies = _gm.Armies.Where(a => a.SectId == sect.Id).ToList();
        foreach (var army in myArmies)
        {
            if (army.Order != ArmyOrder.Idle) continue;

            // find enemy army or location to attack
            var targetArmy = _gm.Armies.FirstOrDefault(a => a.SectId == enemyId);
            if (targetArmy != null)
            {
                army.AttackTargetArmyId = targetArmy.Id;
                army.Order = ArmyOrder.Attacking;
            }
            else
            {
                // attack nearest enemy city
                // nearest city by squared distance
                var targetLoc = _gm.Locations
                    .Where(l => l.OwnerSectId == enemyId && l.Type == LocationType.City)
                    .OrderBy(l => { float dx = l.Position.X - army.Position.X; float dy = l.Position.Y - army.Position.Y; return dx * dx + dy * dy; })
                    .FirstOrDefault();
                if (targetLoc != null)
                {
                    army.MoveTarget = targetLoc.Position;
                    army.Order = ArmyOrder.Moving;
                    float dx = army.Position.X - targetLoc.Position.X;
                    float dy = army.Position.Y - targetLoc.Position.Y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    army.TurnsUntilArrival = (int)(dist / 50f) + 1;
                }
            }
        }
    }

    private void CreateAIResponseArmy(SectData sect)
    {
        var disciples = _gm.State.Disciples.Where(d => d.SectId == sect.Id && d.State == "idle").Take(5).ToList();
        if (disciples.Count == 0) return;
        var home = _gm.Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == sect.Id);
        if (home == null) return;
        _gm.CreateArmy(sect.Id, disciples.Select(d => d.Id).ToList(), home.Position);
    }

    private List<SectData> FindEnemies(SectData sect)
    {
        var result = new List<SectData>();
        foreach (var other in _gm.State.AiSects)
        {
            if (other.Id == sect.Id || !other.IsAlive) continue;
            var rel = _gm.State.GetRelation(sect.Id, other.Id);
            if (rel != null && rel.Favor < -20) result.Add(other);
        }
        // also target owners of adjacent un-allied territory
        foreach (var loc in _gm.Locations)
        {
            if (loc.OwnerSectId < 0 || loc.OwnerSectId == sect.Id) continue;
            var rel = _gm.State.GetRelation(sect.Id, loc.OwnerSectId);
            if (rel != null && rel.State == RelationState.Ally) continue;
            if (SharesBorder(sect, _gm.State.GetSect(loc.OwnerSectId)))
            {
                var other = _gm.State.GetSect(loc.OwnerSectId);
                if (other != null && !result.Contains(other))
                    result.Add(other);
            }
        }
        return result;
    }

    private int EstimatePower(SectData sect)
    {
        return sect.ControlledCityIds.Count * 10
            + _gm.State.Disciples.Count(d => d.SectId == sect.Id) * 5
            + _gm.Armies.Where(a => a.SectId == sect.Id).Sum(a => a.EffectiveCombat);
    }

    private bool SharesBorder(SectData a, SectData b)
    {
        if (a == null || b == null) return false;
        var aLocs = _gm.Locations.Where(l => l.OwnerSectId == a.Id).ToList();
        var bLocs = _gm.Locations.Where(l => l.OwnerSectId == b.Id).ToList();
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
}
