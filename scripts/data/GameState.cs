using Godot;
using System.Collections.Generic;
using System.Linq;

public class GameState
{
    public int Seed;
    public int Year = 1;
    public int Month = 1; // 1-12
    public int Xun = 1;  // 1-36 (旬)
    public int PlayerSectId;
    public List<SectData> Sects = new();
    public Dictionary<int, SectData> SectMap = new();
    public List<DiscipleData> Disciples = new();
    public List<RelationData> Relations = new();
    public Dictionary<(int, int), RelationData> RelationMap = new();
    public List<string> TurnLog = new();
    public int NextDiscipleId = 1;

    public int TotalTurns => (Year - 1) * 36 + Xun;

    public SectData GetSect(int id) => SectMap.TryGetValue(id, out var s) ? s : null;
    public SectData PlayerSect => GetSect(PlayerSectId);
    public List<SectData> AiSects => Sects.Where(s => !s.IsPlayer && s.IsAlive).ToList();

    public RelationData GetRelation(int a, int b)
    {
        if (a > b) { int t = a; a = b; b = t; }
        return RelationMap.TryGetValue((a, b), out var r) ? r : null;
    }

    public void AdvanceTurn()
    {
        Xun++;
        if (Xun > 36) { Xun = 1; Month = 1; Year++; }
        else Month = (Xun - 1) / 3 + 1;
    }

    public void Log(string msg)
    {
        string entry = $"第{Year}年 {Month}月  {msg}";
        TurnLog.Add(entry);
        if (TurnLog.Count > 200) TurnLog.RemoveAt(0);
        GD.Print($"[Game] {entry}");
    }
}
