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
        foreach (var sect in _gm.State.AiSects)
        {
            if (sect.LastDecisionTurn == _gm.State.TotalTurns) continue;
            sect.LastDecisionTurn = _gm.State.TotalTurns;
            ProcessSect(sect);
        }
    }

    private void ProcessSect(SectData sect)
    {
        var state = _gm.State;
        var myLocs = _gm.Locations.Where(l => l.OwnerSectId == sect.Id).ToList();
        var neighbors = FindNeighbors(sect);
        int threat = CalcThreat(sect, neighbors);
        int opportunity = CalcOpportunity(sect, neighbors);

        // pick goal
        if (sect.Personality == AIPersonality.Aggressive && opportunity > threat)
            sect.CurrentGoal = AIGoal.Expand;
        else if (sect.Personality == AIPersonality.Peaceful && threat > opportunity)
            sect.CurrentGoal = AIGoal.Defend;
        else if (opportunity > threat * 2)
            sect.CurrentGoal = AIGoal.Expand;
        else if (threat > opportunity)
            sect.CurrentGoal = AIGoal.Defend;
        else
            sect.CurrentGoal = _rng.Next(2) == 0 ? AIGoal.Develop : AIGoal.Diplomacy;

        // execute based on goal
        switch (sect.CurrentGoal)
        {
            case AIGoal.Expand: TryExpand(sect, neighbors); break;
            case AIGoal.Develop: TryDevelop(sect); break;
            case AIGoal.Diplomacy: TryDiplomacy(sect, neighbors); break;
            case AIGoal.Defend: TryDefend(sect, neighbors); break;
        }
    }

    private List<SectData> FindNeighbors(SectData sect)
    {
        var myLocs = _gm.Locations.Where(l => l.OwnerSectId == sect.Id).ToList();
        if (myLocs.Count == 0) return new List<SectData>();

        var result = new List<SectData>();
        float range = 600f;
        foreach (var other in _gm.State.AiSects)
        {
            if (other.Id == sect.Id) continue;
            var theirLocs = _gm.Locations.Where(l => l.OwnerSectId == other.Id).ToList();
            foreach (var ml in myLocs)
                foreach (var ol in theirLocs)
                    if ((ml.Position - ol.Position).Length() < range)
                    {
                        if (!result.Contains(other)) result.Add(other);
                        goto nextSect;
                    }
            nextSect:;
        }
        return result;
    }

    private int CalcThreat(SectData sect, List<SectData> neighbors)
    {
        int threat = 0;
        foreach (var n in neighbors)
        {
            var rel = _gm.State.GetRelation(sect.Id, n.Id);
            if (rel == null) continue;
            if (rel.State == RelationState.Hostile || rel.State == RelationState.War)
                threat += 2;
            if (n.Personality == AIPersonality.Aggressive) threat += 1;
            // compare strength (disciples + cities)
            int myPower = sect.ControlledCityIds.Count + _gm.State.Disciples.Count(d => d.SectId == sect.Id);
            int nPower = n.ControlledCityIds.Count + _gm.State.Disciples.Count(d => d.SectId == n.Id);
            if (nPower > myPower * 1.5f) threat += 2;
        }
        return threat;
    }

    private int CalcOpportunity(SectData sect, List<SectData> neighbors)
    {
        int opp = 0;
        // unclaimed cities nearby
        foreach (var loc in _gm.Locations)
        {
            if (loc.OwnerSectId >= 0) continue;
            if (loc.Type != LocationType.City) continue;
            var myLocs = _gm.Locations.Where(l => l.OwnerSectId == sect.Id).ToList();
            foreach (var ml in myLocs)
                if ((ml.Position - loc.Position).Length() < 600f) { opp++; break; }
        }
        // weak neighbors
        foreach (var n in neighbors)
        {
            int myPower = sect.ControlledCityIds.Count + _gm.State.Disciples.Count(d => d.SectId == sect.Id);
            int nPower = n.ControlledCityIds.Count + _gm.State.Disciples.Count(d => d.SectId == n.Id);
            if (myPower > nPower * 2) opp += 2;
        }
        return opp;
    }

    private void TryExpand(SectData sect, List<SectData> neighbors)
    {
        // find best unclaimed city nearby
        var targets = _gm.Locations
            .Where(l => l.OwnerSectId < 0 && l.Type == LocationType.City)
            .OrderByDescending(l => l.Prosperity)
            .ToList();

        foreach (var t in targets)
        {
            var myLocs = _gm.Locations.Where(l => l.OwnerSectId == sect.Id).ToList();
            bool nearby = false;
            foreach (var ml in myLocs)
                if ((ml.Position - t.Position).Length() < 800f) { nearby = true; break; }
            if (!nearby) continue;

            // send influence
            t.AddInfluence(sect.Id, 5f);
            _gm.State.Log($"{sect.Name} 向 {t.Name} 渗透影响力");
            return;
        }

        // if no expansion target, develop instead
        TryDevelop(sect);
    }

    private void TryDevelop(SectData sect)
    {
        // upgrade cheapest building
        int[] buildings = { sect.MeetingHall, sect.CultivationRoom, sect.Library,
                            sect.AlchemyRoom, sect.SpiritField, sect.ProtectionArray };
        int cheapest = 0;
        int cheapestCost = int.MaxValue;
        for (int i = 0; i < buildings.Length; i++)
        {
            int cost = (buildings[i] + 1) * 100;
            if (buildings[i] < 3 && cost < cheapestCost && sect.Lingshi >= cost)
            {
                cheapestCost = cost;
                cheapest = i;
            }
        }
        if (cheapestCost < int.MaxValue && sect.Lingshi >= cheapestCost)
        {
            sect.Lingshi -= cheapestCost;
            switch (cheapest)
            {
                case 0: sect.MeetingHall++; break;
                case 1: sect.CultivationRoom++; break;
                case 2: sect.Library++; break;
                case 3: sect.AlchemyRoom++; break;
                case 4: sect.SpiritField++; break;
                case 5: sect.ProtectionArray++; break;
            }
            _gm.State.Log($"{sect.Name} 升级了建筑 Lv.{buildings[cheapest] + 1}");
        }

        // try recruit disciple
        if (sect.Lingshi > 50)
        {
            var myLoc = _gm.Locations.FirstOrDefault(l => l.OwnerSectId == sect.Id);
            if (myLoc != null)
            {
                _gm.CreateDisciple($"{sect.Name}弟子{_gm.State.Disciples.Count}", sect.Id, myLoc);
                sect.Lingshi -= 50;
            }
        }
    }

    private void TryDiplomacy(SectData sect, List<SectData> neighbors)
    {
        foreach (var n in neighbors)
        {
            var rel = _gm.State.GetRelation(sect.Id, n.Id);
            if (rel == null) continue;
            if (rel.Favor < 30 && sect.Lingshi > 100)
            {
                rel.Favor += 10;
                sect.Lingshi -= 50;
                _gm.State.Log($"{sect.Name} 向 {n.Name} 遣使交好");
                return;
            }
        }
        TryDevelop(sect);
    }

    private void TryDefend(SectData sect, List<SectData> neighbors)
    {
        // build protection array if possible
        if (sect.ProtectionArray < 3 && sect.Lingshi > 100)
        {
            sect.Lingshi -= (sect.ProtectionArray + 1) * 80;
            sect.ProtectionArray++;
            _gm.State.Log($"{sect.Name} 升级了护山大阵");
        }
        // send disciples to garrison cities
        foreach (var loc in _gm.Locations.Where(l => l.OwnerSectId == sect.Id && l.Type == LocationType.City))
        {
            if (loc.GarrisonIds.Count == 0)
            {
                var disciple = _gm.State.Disciples.FirstOrDefault(d => d.SectId == sect.Id && d.State == "idle");
                if (disciple != null)
                {
                    disciple.State = "task";
                    disciple.TaskTargetId = loc.Id;
                    disciple.TaskTurnsLeft = 5;
                    loc.GarrisonIds.Add(disciple.Id);
                }
            }
        }
    }
}
