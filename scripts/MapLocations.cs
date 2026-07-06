using Godot;
using System;
using System.Collections.Generic;

public static class MapLocations
{

    public static List<MapLocation> Generate(int mapW, int mapH, int seed)
    {
        var rng = new Random(seed + 999);
        var locations = new List<MapLocation>();
        var usedNames = new HashSet<string>();

        var noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        noise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
        noise.FractalOctaves = 6;
        noise.FractalLacunarity = 2.0f;
        noise.FractalGain = 0.5f;
        noise.Frequency = 0.003f;
        noise.Seed = seed;

        float Sample(int px, int py) => noise.GetNoise2D(px, py);

        // scale distances with map size
        float s = mapW / 2048f;
        int edgePad = (int)(200 * s / 8f); // keep icons away from edge

        // --- Sects (山地) ---
        var sectPositions = new List<Vector2>();
        int sectTarget = (int)(15 * s * s);
        for (int i = 0; i < sectTarget * 30; i++)
        {
            int x = rng.Next(edgePad, mapW - edgePad);
            int y = rng.Next(edgePad, mapH - edgePad);
            float v = Sample(x, y);
            if (v < 0.25f || v > 0.7f) continue;
            if (TooClose(sectPositions, new Vector2(x, y), 150f)) continue;
            sectPositions.Add(new Vector2(x, y));
            locations.Add(new MapLocation(new Vector2(x, y), NameDB.GenerateName(LocationType.Sect, rng, usedNames), LocationType.Sect, 0));
            if (sectPositions.Count >= sectTarget) break;
        }

        // --- Cities (平原) ---
        var cityPositions = new List<Vector2>();
        int cityTarget = (int)(25 * s * s);
        for (int i = 0; i < cityTarget * 30; i++)
        {
            int x = rng.Next(edgePad, mapW - edgePad);
            int y = rng.Next(edgePad, mapH - edgePad);
            float v = Sample(x, y);
            if (v < -0.3f || v > 0.25f) continue;
            if (TooClose(cityPositions, new Vector2(x, y), 150f)) continue;
            cityPositions.Add(new Vector2(x, y));
            locations.Add(new MapLocation(new Vector2(x, y), NameDB.GenerateName(LocationType.City, rng, usedNames), LocationType.City, rng.Next(5000, 50001)));
            if (cityPositions.Count >= cityTarget) break;
        }

        // --- Villages (密集) ---
        var villagePositions = new List<Vector2>();
        int villageTarget = (int)(120 * s * s);
        for (int i = 0; i < villageTarget * 30; i++)
        {
            int x = rng.Next(edgePad, mapW - edgePad);
            int y = rng.Next(edgePad, mapH - edgePad);
            float v = Sample(x, y);
            if (v < -0.15f || v > 0.45f) continue;
            if (TooClose(villagePositions, new Vector2(x, y), 80f)) continue;
            if (TooClose(cityPositions, new Vector2(x, y), 60f)) continue;
            villagePositions.Add(new Vector2(x, y));
            locations.Add(new MapLocation(new Vector2(x, y), NameDB.GenerateName(LocationType.Village, rng, usedNames), LocationType.Village, rng.Next(100, 3001)));
            if (villagePositions.Count >= villageTarget) break;
        }

        // assign owner (nearest sect)
        for (int i = 0; i < locations.Count; i++)
        {
            if (locations[i].Type == LocationType.Sect)
            {
                locations[i].OwnerIndex = i;
                continue;
            }
            float bestDist = float.MaxValue;
            int bestSect = -1;
            for (int j = 0; j < locations.Count; j++)
            {
                if (locations[j].Type != LocationType.Sect) continue;
                float d = (locations[i].Position - locations[j].Position).LengthSquared();
                if (d < bestDist) { bestDist = d; bestSect = j; }
            }
            locations[i].OwnerIndex = bestSect;
        }

        return locations;
    }

    private static bool TooClose(List<Vector2> existing, Vector2 pos, float minDist)
    {
        float minSq = minDist * minDist;
        foreach (var p in existing)
            if ((p - pos).LengthSquared() < minSq) return true;
        return false;
    }
}
