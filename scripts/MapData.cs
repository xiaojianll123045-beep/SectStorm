using Godot;

public enum LocationType
{
    City,
    Village,
    Sect
}

public class MapLocation
{
    public Vector2 Position;
    public string Name;
    public LocationType Type;
    public int Population;
    public int OwnerIndex = -1; // index into locations list for the controlling sect

    public MapLocation(Vector2 pos, string name, LocationType type, int pop = 0)
    {
        Position = pos;
        Name = name;
        Type = type;
        Population = pop;
    }
}
