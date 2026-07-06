using Godot;
using System.Collections.Generic;

public enum AIPersonality { Peaceful, Balanced, Aggressive }
public enum AIGoal { Expand, Develop, Diplomacy, Defend }

public class SectData
{
    public int Id;
    public string Name;
    public bool IsPlayer;
    public bool IsAlive = true;

    // resources
    public float Lingshi;
    public float Prestige;
    public float SpiritVein; //灵气浓度

    // buildings: level 0-3
    public int MeetingHall;  //议事堂
    public int CultivationRoom; //修炼室
    public int Library; //藏经阁
    public int AlchemyRoom; //丹房
    public int SpiritField; //灵田
    public int ProtectionArray; //护山大阵

    // controlled locations
    public List<int> ControlledCityIds = new();
    public List<int> ControlledVillageIds = new();

    // AI
    public AIPersonality Personality = AIPersonality.Balanced;
    public AIGoal CurrentGoal = AIGoal.Develop;
    public int LastDecisionTurn;

    public int GetBuildingLevel(int buildingIdx)
    {
        return buildingIdx switch
        {
            0 => MeetingHall, 1 => CultivationRoom, 2 => Library,
            3 => AlchemyRoom, 4 => SpiritField, 5 => ProtectionArray,
            _ => 0
        };
    }

    public int TotalCityIncome()
    {
        return ControlledCityIds.Count * (10 + MeetingHall * 5);
    }

    public int MaxDisciples()
    {
        return 5 + CultivationRoom * 5 + MeetingHall * 3;
    }

    public float BreakthroughBonus()
    {
        return 1f + CultivationRoom * 0.15f + SpiritVein * 0.001f;
    }

    public float AlchemyBonus()
    {
        return 1f + AlchemyRoom * 0.2f;
    }

    public float DefenseBonus()
    {
        return 1f + ProtectionArray * 0.3f;
    }
}
