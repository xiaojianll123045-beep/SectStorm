using Godot;
using System.Collections.Generic;
using System.Linq;

public static class PathFinder
{
    private const int CellSize = 256;

    public struct PathCell { public int X, Y; }

    public static List<Vector2> FindPath(Vector2 from, Vector2 to, int armySectId, GameState state, List<LocationData> locations)
    {
        int cw = 16384 / CellSize;
        int ch = 16384 / CellSize;

        int sx = (int)(from.X / CellSize); if (sx < 0) sx = 0; if (sx >= cw) sx = cw - 1;
        int sy = (int)(from.Y / CellSize); if (sy < 0) sy = 0; if (sy >= ch) sy = ch - 1;
        int ex = (int)(to.X / CellSize); if (ex < 0) ex = 0; if (ex >= cw) ex = cw - 1;
        int ey = (int)(to.Y / CellSize); if (ey < 0) ey = 0; if (ey >= ch) ey = ch - 1;

        if (sx == ex && sy == ey)
            return new List<Vector2> { to };

        // all territory passable - just return direct path
        return new List<Vector2> { to };
    }
}
