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

        // passability check
        bool IsPassable(int cx, int cy)
        {
            float wx = cx * CellSize + CellSize / 2f;
            float wy = cy * CellSize + CellSize / 2f;
            int owner = -1;
            float bestDist = 300f * 300f;
            foreach (var loc in locations)
            {
                if (loc.OwnerSectId < 0) continue;
                float dx = wx - loc.Position.X;
                float dy = wy - loc.Position.Y;
                float d = dx * dx + dy * dy;
                if (d < bestDist) { bestDist = d; owner = loc.OwnerSectId; }
            }
            if (owner < 0) return true;
            if (owner == armySectId) return true;
            return true; // allow all territory for pathfinding
        }

        // A*
        var open = new List<(int x, int y, int g, int f, int px, int py)>();
        var closed = new HashSet<(int, int)>();
        var cameFrom = new Dictionary<(int, int), (int, int)>();

        int Heuristic(int ax, int ay) => System.Math.Abs(ax - ex) + System.Math.Abs(ay - ey);

        open.Add((sx, sy, 0, Heuristic(sx, sy), -1, -1));

        while (open.Count > 0)
        {
            open.Sort((a, b) => a.f.CompareTo(b.f));
            var cur = open[0];
            open.RemoveAt(0);

            if (cur.x == ex && cur.y == ey)
            {
                // reconstruct path
                var path = new List<Vector2>();
                var c = (ex, ey);
                while (cameFrom.ContainsKey(c))
                {
                    path.Add(new Vector2(c.Item1 * CellSize + CellSize / 2f, c.Item2 * CellSize + CellSize / 2f));
                    c = cameFrom[c];
                }
                path.Add(to);
                path.Reverse();
                return path;
            }

            if (closed.Contains((cur.x, cur.y))) continue;
            closed.Add((cur.x, cur.y));
            cameFrom[(cur.x, cur.y)] = (cur.px, cur.py);

            // 4-direction neighbors
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { -1, 0, 1, 0 };
            for (int d = 0; d < 4; d++)
            {
                int nx = cur.x + dx[d];
                int ny = cur.y + dy[d];
                if (nx < 0 || nx >= cw || ny < 0 || ny >= ch) continue;
                if (closed.Contains((nx, ny))) continue;
                if (!IsPassable(nx, ny)) continue;
                int ng = cur.g + 1;
                open.Add((nx, ny, ng, ng + Heuristic(nx, ny), cur.x, cur.y));
            }
        }

        return null; // no path found
    }
}
