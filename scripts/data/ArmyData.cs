using Godot;
using System.Collections.Generic;
using System.Linq;

public enum ArmyOrder { Idle, Moving, Attacking }

public class ArmyData
{
    public int Id;
    public string Name;
    public int SectId;
    public List<int> DiscipleIds = new();
    public Vector2 Position;
    public ArmyOrder Order = ArmyOrder.Idle;
    public Vector2 MoveTarget;
    public int AttackTargetArmyId = -1; // pursuing a specific army
    public int AttackTargetLocId = -1;  // attacking a location
    public int TurnsUntilArrival;
    public List<Vector2> PathWaypoints;
    public int TurnsWithoutSupply;

    // cached stats
    public int TotalCombat => DiscipleIds.Sum(id => _GetDisciple(id)?.Combat ?? 0);
    public int Count => DiscipleIds.Count;
    public bool IsAlive => DiscipleIds.Count > 0;

    private DiscipleData _GetDisciple(int id)
    {
        // this will be set by GameManager
        return null;
    }

    public System.Func<int, DiscipleData> ResolveDisciple;

    public int EffectiveCombat
    {
        get
        {
            int total = 0;
            foreach (var did in DiscipleIds)
            {
                var d = ResolveDisciple?.Invoke(did);
                if (d == null) continue;
                float realmMul = d.Realm switch
                {
                    Realm.QiRefining => 1f + d.SubRealm * 0.3f,
                    Realm.Foundation => 2.5f + d.SubRealm * 1.0f,
                    Realm.Core => 8f + d.SubRealm * 4f,
                    Realm.NascentSoul => 30f + d.SubRealm * 15f,
                    Realm.DeitySynthesis => 80f + d.SubRealm * 40f,
                    Realm.Tribulation => 200f,
                    _ => 1f
                };
                total += (int)(d.Combat * realmMul);
            }
            // supply penalty
            if (TurnsWithoutSupply > 3)
                total = (int)(total * (1f - (TurnsWithoutSupply - 3) * 0.1f));
            return total;
        }
    }

    public int MaxRange => DiscipleIds.Count > 0 ? 1 : 0; // can target adjacent

    public bool IsAtLocation(Vector2 pos)
    {
        return (Position - pos).LengthSquared() < 400f; // 20px tolerance
    }
}
