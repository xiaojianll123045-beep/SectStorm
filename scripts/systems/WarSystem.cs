using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class WarData
{
    public int AttackerSectId;
    public int DefenderSectId;
    public int WarScore; // positive = attacker winning, negative = defender
    public int TurnsActive;
    public bool Ended;
    public int WinnerId = -1; // 0=attacker won, 1=defender won, or sectId for annexation

    public int ScoreFromBattles;  // kills
    public int ScoreFromOccupation; // captured location value

    public List<int> OccupiedByAttacker = new(); // location ids
    public List<int> OccupiedByDefender = new();

    public bool AttackerProposedPeace;
    public bool DefenderProposedPeace;
}

public static class WarSystem
{
    private static Random _rng = new();

    public static WarData DeclareWar(int attackerId, int defenderId)
    {
        return new WarData
        {
            AttackerSectId = attackerId,
            DefenderSectId = defenderId,
            WarScore = 0,
            TurnsActive = 0,
        };
    }

    public static void ProcessWarTurn(WarData war, GameState state, List<LocationData> locations)
    {
        war.TurnsActive++;

        // update occupation
        war.OccupiedByAttacker.Clear();
        war.OccupiedByDefender.Clear();
        foreach (var loc in locations)
        {
            if (loc.OwnerSectId == war.DefenderSectId && loc.GetInfluence(war.AttackerSectId) > loc.GetInfluence(war.DefenderSectId))
                war.OccupiedByAttacker.Add(loc.Id);
            if (loc.OwnerSectId == war.AttackerSectId && loc.GetInfluence(war.DefenderSectId) > loc.GetInfluence(war.AttackerSectId))
                war.OccupiedByDefender.Add(loc.Id);
        }

        // update war score
        int occupationScore = war.OccupiedByAttacker.Count * 5 - war.OccupiedByDefender.Count * 5;
        war.ScoreFromOccupation = occupationScore;
        war.WarScore = war.ScoreFromBattles + war.ScoreFromOccupation;

        // check annexation: attacker captured defender's home territory
        var defSect = state.GetSect(war.DefenderSectId);
        if (defSect != null)
        {
            var homeLoc = locations.FirstOrDefault(l => l.OwnerSectId == war.DefenderSectId && l.Type == LocationType.Sect);
            if (homeLoc != null && war.OccupiedByAttacker.Contains(homeLoc.Id))
            {
                AnnexSect(war.AttackerSectId, war.DefenderSectId, state, locations);
                war.Ended = true;
                war.WinnerId = war.AttackerSectId;
                state.Log($"{state.GetSect(war.AttackerSectId)?.Name} 吞并了 {defSect.Name}！");
                return;
            }
        }

        // AI auto peace evaluation
        var atk = state.GetSect(war.AttackerSectId);
        var def = state.GetSect(war.DefenderSectId);
        if (atk != null && def != null)
        {
            if (!atk.IsPlayer && ShouldAISueForPeace(war, atk, def))
            {
                war.AttackerProposedPeace = true;
                war.Ended = true;
                war.WinnerId = war.DefenderSectId;
                state.Log($"{atk.Name} 向 {def.Name} 求和");
            }
            if (!def.IsPlayer && ShouldAISueForPeace(war, def, atk))
            {
                war.DefenderProposedPeace = true;
                war.Ended = true;
                war.WinnerId = war.AttackerSectId;
                state.Log($"{def.Name} 向 {atk.Name} 求和");
            }
        }
    }

    private static bool ShouldAISueForPeace(WarData war, SectData self, SectData enemy)
    {
        float myPower = self.ControlledCityIds.Count + 1;
        float enemyPower = enemy.ControlledCityIds.Count + 1;
        float ratio = myPower / Math.Max(enemyPower, 1);

        // losing badly or war lasting too long
        if (war.WarScore < -20) return true;
        if (war.TurnsActive > 72 && ratio < 0.8f) return true; // 2 years
        if (war.OccupiedByAttacker.Count(self.ControlledCityIds.Contains) > 3) return true;

        return false;
    }

    public static void AnnexSect(int winnerId, int loserId, GameState state, List<LocationData> locations)
    {
        var loser = state.GetSect(loserId);
        var winner = state.GetSect(winnerId);
        if (loser == null || winner == null) return;

        // transfer locations
        foreach (var loc in locations)
        {
            if (loc.OwnerSectId == loserId)
            {
                loc.OwnerSectId = winnerId;
                loc.Influence.Clear();
                loc.AddInfluence(winnerId, 50);
            }
        }

        // transfer disciples
        foreach (var d in state.Disciples)
        {
            if (d.SectId == loserId)
            {
                d.SectId = winnerId;
                d.Loyalty = 30;
            }
        }

        // remove from sects list
        loser.IsAlive = false;
        winner.Prestige += 50 + warScoreBonus(loser);
    }

    private static int warScoreBonus(SectData loser)
    {
        return (int)Mathf.Clamp(loser.Prestige / 10f, 0, 100);
    }
}
