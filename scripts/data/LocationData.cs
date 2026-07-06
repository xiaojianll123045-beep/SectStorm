using Godot;
using System.Collections.Generic;
using System.Linq;

public class LocationData
{
    public int Id;
    public Vector2 Position;
    public string Name;
    public LocationType Type;
    public int Population;
    public int OwnerSectId = -1; // highest influence sect
    public Dictionary<int, float> Influence = new(); // sectId -> influence
    public float Prosperity = 50f;
    public float Loyalty = 50f;
    public string Status = "normal"; // normal, disaster, war
    public List<int> GarrisonIds = new(); // disciple ids stationed here

    public int XunSinceLastCheck;
    public int CropYield; // for villages
    public int TaxBase; // for cities

    public LocationData(int id, string name, LocationType type, Vector2 pos, int pop)
    {
        Id = id; Name = name; Type = type; Position = pos; Population = pop;
        TaxBase = pop / 10;
        CropYield = type == LocationType.Village ? pop / 20 : 0;
    }

    public void AddInfluence(int sectId, float amount)
    {
        if (!Influence.ContainsKey(sectId)) Influence[sectId] = 0;
        Influence[sectId] += amount;
        // decay other influences slightly
        foreach (var k in Influence.Keys.ToList())
            if (k != sectId)
                Influence[k] *= 0.98f;
        RecalcOwner();
    }

    public void RecalcOwner()
    {
        int best = -1;
        float bestV = 0;
        foreach (var kv in Influence)
        {
            if (kv.Value > bestV) { bestV = kv.Value; best = kv.Key; }
        }
        OwnerSectId = best;
    }

    public float GetInfluence(int sectId) =>
        Influence.TryGetValue(sectId, out var v) ? v : 0f;
}
