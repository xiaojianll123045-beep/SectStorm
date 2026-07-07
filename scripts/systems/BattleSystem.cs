using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class BattleSystem
{
    private static Random _rng = new();

    /// <summary>
    /// Resolve a battle between attacker and defender stacks at a location.
    /// Returns true if attacker won the location.
    /// </summary>
    public static BattleResult Resolve(
        List<ArmyData> attackers, List<ArmyData> defenders,
        float defenderFortBonus, float attackerDistancePenalty)
    {
        int atkPower = attackers.Sum(a => a.EffectiveCombat);
        int defPower = defenders.Sum(a => a.EffectiveCombat);

        // apply bonuses
        atkPower = (int)(atkPower * (1f - attackerDistancePenalty));
        defPower = (int)(defPower * (1f + defenderFortBonus));

        // random factor
        atkPower = (int)(atkPower * (0.85f + (float)_rng.NextDouble() * 0.3f));
        defPower = (int)(defPower * (0.85f + (float)_rng.NextDouble() * 0.3f));

        var result = new BattleResult();

        if (atkPower > defPower)
        {
            // attacker wins
            result.AttackerWon = true;
            result.AtKilled = Casualties(attackers, 0.05f, 0.15f);
            result.DefKilled = Casualties(defenders, 0.20f, 0.40f);
            result.DefendersRemoved = true;
        }
        else
        {
            // defender wins
            result.AttackerWon = false;
            result.AtKilled = Casualties(attackers, 0.20f, 0.40f);
            result.DefKilled = Casualties(defenders, 0.05f, 0.15f);
        }

        return result;
    }

    private static int Casualties(List<ArmyData> armies, float minRate, float maxRate)
    {
        int total = 0;
        foreach (var a in armies)
        {
            if (a.DiscipleIds.Count == 0) continue;
            float rate = minRate + (float)_rng.NextDouble() * (maxRate - minRate);
            int count = Mathf.Max(0, (int)(a.DiscipleIds.Count * rate));
        if (count > a.DiscipleIds.Count) count = a.DiscipleIds.Count;
            for (int i = 0; i < count && a.DiscipleIds.Count > 0; i++)
            {
                a.DiscipleIds.RemoveAt(a.DiscipleIds.Count - 1);
                total++;
            }
        }
        return total;
    }

    /// <summary>Pursuit: attacker chases defender if defender retreats from a stack.</summary>
    public static void Pursue(ArmyData attacker, ArmyData defender, float speedRatio)
    {
        if (speedRatio <= 0) return;
        float dist = (attacker.Position - defender.Position).Length();
        float catchDist = dist * (1f - speedRatio * 0.3f);
        if (catchDist < 30f)
        {
            // caught! resolve another battle round
            var result = Resolve(new List<ArmyData>{attacker}, new List<ArmyData>{defender}, 0, 0);
            if (result.AttackerWon && defender.DiscipleIds.Count == 0)
                defender.DiscipleIds.Clear();
        }
    }
}

public struct BattleResult
{
    public bool AttackerWon;
    public int AtKilled;
    public int DefKilled;
    public bool DefendersRemoved;
}
