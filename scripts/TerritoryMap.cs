using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class TerritoryMap
{
    private static readonly Color[] SectColors =
    {
        new Color(0.75f, 0.20f, 0.20f), new Color(0.20f, 0.60f, 0.30f),
        new Color(0.35f, 0.30f, 0.80f), new Color(0.80f, 0.60f, 0.10f),
        new Color(0.70f, 0.30f, 0.70f), new Color(0.20f, 0.60f, 0.60f),
        new Color(0.70f, 0.50f, 0.20f), new Color(0.60f, 0.20f, 0.40f),
        new Color(0.30f, 0.50f, 0.20f), new Color(0.40f, 0.30f, 0.60f),
        new Color(0.70f, 0.40f, 0.30f), new Color(0.30f, 0.40f, 0.70f),
        new Color(0.60f, 0.60f, 0.20f), new Color(0.50f, 0.30f, 0.50f),
        new Color(0.80f, 0.30f, 0.30f),
    };

    private static readonly Color Neutral = new Color(0.55f, 0.52f, 0.48f);
    private static readonly Color CellBorder = new Color(0.92f, 0.90f, 0.87f);
    private static readonly Color RegimeBorder = new Color(0.06f, 0.04f, 0.03f);

    public struct CellInfo { public int Seed; public int Regime; }

    public class Result
    {
        public ImageTexture Texture;
        public Vector2[] Centroids;
        public int[] CellSeeds;
        public int CellW;
        public int CellH;
    }

    public static Result Generate(List<MapLocation> allLocations, int mapW, int mapH)
    {
        if (allLocations.Count < 2) return null;

        int cellSize = 8;
        int cw = mapW / cellSize;
        int ch = mapH / cellSize;
        float c2w = cellSize;

        var regimeColors = new Dictionary<int, int>();
        int nextColor = 0;
        foreach (var loc in allLocations)
        {
            int r = loc.OwnerIndex;
            if (r < 0) continue;
            if (!regimeColors.ContainsKey(r))
                regimeColors[r] = nextColor++ % SectColors.Length;
        }

        Color GetColor(int regime)
        {
            if (regime >= 0 && regimeColors.TryGetValue(regime, out int ci))
                return SectColors[ci];
            return Neutral;
        }

        // spatial grid
        int gridSize = 256;
        int gw = (mapW + gridSize - 1) / gridSize;
        int gh = (mapH + gridSize - 1) / gridSize;
        var grid = new List<int>[gw * gh];
        for (int i = 0; i < allLocations.Count; i++)
        {
            var loc = allLocations[i];
            int gx = (int)(loc.Position.X / gridSize);
            int gy = (int)(loc.Position.Y / gridSize);
            if (gx < 0) gx = 0; if (gx >= gw) gx = gw - 1;
            if (gy < 0) gy = 0; if (gy >= gh) gy = gh - 1;
            int gi = gy * gw + gx;
            if (grid[gi] == null) grid[gi] = new List<int>();
            grid[gi].Add(i);
        }

        float WrapDelta(float a, float b, float worldSize)
        {
            float d = a - b;
            if (d > worldSize / 2f) d -= worldSize;
            else if (d < -worldSize / 2f) d += worldSize;
            return d;
        }

        // inline FindSeed for performance
        var img = Image.CreateEmpty(cw, ch, false, Image.Format.Rgba8);
        var cells = new CellInfo[cw * ch];
        var cellSeeds = new int[cw * ch];
        var count = new int[allLocations.Count];

        // ---- PARALLEL FILL ----
        Parallel.For(0, ch, cy =>
        {
            for (int cx = 0; cx < cw; cx++)
            {
                float wx = cx * c2w + c2w / 2f;
                float wy = cy * c2w + c2w / 2f;

                int gx = (int)(wx / gridSize); if (gx < 0) gx = 0; if (gx >= gw) gx = gw - 1;
                int gy = (int)(wy / gridSize); if (gy < 0) gy = 0; if (gy >= gh) gy = gh - 1;

                int nearest = -1;
                float bestD = float.MaxValue;
                for (int dgy = -1; dgy <= 1; dgy++)
                    for (int dgx = -1; dgx <= 1; dgx++)
                    {
                        int ngx = gx + dgx, ngy = gy + dgy;
                        if (ngx < 0 || ngx >= gw || ngy < 0 || ngy >= gh) continue;
                        var bucket = grid[ngy * gw + ngx];
                        if (bucket == null) continue;
                        foreach (int idx in bucket)
                        {
                            float dx = WrapDelta(allLocations[idx].Position.X, wx, mapW);
                            float dy = WrapDelta(allLocations[idx].Position.Y, wy, mapH);
                            float d = dx * dx + dy * dy;
                            if (d < bestD) { bestD = d; nearest = idx; }
                        }
                    }

                if (nearest < 0) { img.SetPixel(cx, cy, Neutral); continue; }

                int regime = allLocations[nearest].OwnerIndex;
                int idx2 = cy * cw + cx;
                cells[idx2] = new CellInfo { Seed = nearest, Regime = regime };
                cellSeeds[idx2] = nearest;
                img.SetPixel(cx, cy, GetColor(regime));
            }
        });

        // centroids from cellSeeds (sequential, fast)
        var centroids = new Vector2[allLocations.Count];
        var cxSum = new double[allLocations.Count];
        var cySum = new double[allLocations.Count];
        for (int i = 0; i < cellSeeds.Length; i++)
        {
            int s = cellSeeds[i];
            if (s >= 0) { count[s]++; cxSum[s] += (i % cw) * c2w + c2w / 2f; cySum[s] += (i / cw) * c2w + c2w / 2f; }
        }
        for (int i = 0; i < allLocations.Count; i++)
        {
            centroids[i] = count[i] > 0
                ? new Vector2((float)(cxSum[i] / count[i]), (float)(cySum[i] / count[i]))
                : allLocations[i].Position;
        }

        // ---- PARALLEL BORDERS ----
        Parallel.For(0, ch, cy =>
        {
            for (int cx = 0; cx < cw; cx++)
            {
                int idx2 = cy * cw + cx;
                var ci = cells[idx2];
                bool diffSeed = false, diffRegime = false;

                void Check(int nx, int ny)
                {
                    if (nx < 0 || nx >= cw || ny < 0 || ny >= ch)
                    { diffRegime = true; return; }
                    var o = cells[ny * cw + nx];
                    if (o.Seed != ci.Seed) diffSeed = true;
                    if (o.Regime != ci.Regime) diffRegime = true;
                }

                Check(cx - 1, cy); Check(cx + 1, cy);
                Check(cx, cy - 1); Check(cx, cy + 1);

                if (diffRegime) img.SetPixel(cx, cy, RegimeBorder);
                else if (diffSeed) img.SetPixel(cx, cy, CellBorder);
            }
        });

        return new Result
        {
            Texture = ImageTexture.CreateFromImage(img),
            Centroids = centroids,
            CellSeeds = cellSeeds,
            CellW = cw,
            CellH = ch
        };
    }
}
