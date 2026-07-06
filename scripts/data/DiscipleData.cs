using Godot;

public enum Realm
{
    QiRefining, Foundation, Core, NascentSoul, DeitySynthesis, Tribulation
}

public class DiscipleData
{
    public int Id;
    public string Name;
    public int SectId = -1;
    public Realm Realm;
    public int SubRealm; // 0=early, 1=mid, 2=late
    public int Combat, Alchemy, Crafting, Formation, Management;
    public int Lifespan; // turns remaining
    public int Mood = 80;
    public int Loyalty = 70;
    public string State = "idle"; // idle, task, retreat, cultivate, heal
    public int TaskTargetId = -1;
    public int TaskTurnsLeft;
    public int CultivationProgress;

    public int TotalStats => Combat + Alchemy + Crafting + Formation + Management;

    public DiscipleData(int id, string name)
    {
        Id = id; Name = name;
        Realm = Realm.QiRefining;
        SubRealm = 0;
        Combat = GD.RandRange(5, 20);
        Alchemy = GD.RandRange(5, 20);
        Crafting = GD.RandRange(5, 20);
        Formation = GD.RandRange(5, 20);
        Management = GD.RandRange(5, 20);
        Lifespan = GD.RandRange(800, 1200);
    }
}
