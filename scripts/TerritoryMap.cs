using Godot;
using System.Collections.Generic;

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

        // regime color lookup
        var regimeColors = new Dictionary<int, int>();
        int nextColor = 0;
        foreach (var loc in allLocations)
        {
            int r = loc.OwnerIndex;
            if (r < 0) continue;
            if (!regimeColors.ContainsKey(r)) regimeColors[r] = nextColor++ % SectColors.Length;
        }

        // spatial grid
        int gridSize = 256;
        int gw = (mapW + gridSize - 1) / gridSize;
        int gh = (mapH + gridSize - 1) / gridSize;
        var grid = new List<int>[gw * gh];
        for (int i = 0; i < allLocations.Count; i++)
        {
            var loc = allLocations[i];
            int gx = (int)(loc.Position.X / gridSize); if (gx < 0) gx = 0; if (gx >= gw) gx = gw - 1;
            int gy = (int)(loc.Position.Y / gridSize); if (gy < 0) gy = 0; if (gy >= gh) gy = gh - 1;
            int gi = gy * gw + gx;
            if (grid[gi] == null) grid[gi] = new List<int>();
            grid[gi].Add(i);
        }

        int FindSeed(float wx, float wy)
        {
            int gx = (int)(wx / gridSize); if (gx < 0) gx = 0; if (gx >= gw) gx = gw - 1;
            int gy = (int)(wy / gridSize); if (gy < 0) gy = 0; if (gy >= gh) gy = gh - 1;
            int best = -1;
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
                        float dx = allLocations[idx].Position.X - wx;
                        float dy = allLocations[idx].Position.Y - wy;
                        float d = dx * dx + dy * dy;
                        if (d < bestD) { bestD = d; best = idx; }
                    }
                }
            return best;
        }

        // byte array (no Godot API during computation)
        int totalPixels = cw * ch;
        var data = new byte[totalPixels * 4];
        var cellSeeds = new int[totalPixels];
        var cells = new CellInfo[totalPixels];

        for (int cy = 0; cy < ch; cy++)
        {
            for (int cx = 0; cx < cw; cx++)
            {
                float wx = cx * c2w + c2w / 2f;
                float wy = cy * c2w + c2w / 2f;
                int seed = FindSeed(wx, wy);
                if (seed < 0) continue;

                int idx = cy * cw + cx;
                int regime = allLocations[seed].OwnerIndex;
                cellSeeds[idx] = seed;
                cells[idx] = new CellInfo { Seed = seed, Regime = regime };

                int pi = idx * 4;
                Color c = Neutral;
                if (regime >= 0 && regimeColors.TryGetValue(regime, out int ci)) c = SectColors[ci];
                data[pi] = (byte)(c.R * 255); data[pi + 1] = (byte)(c.G * 255);
                data[pi + 2] = (byte)(c.B * 255); data[pi + 3] = 255;
            }
        }

        // borders
        for (int cy = 0; cy < ch; cy++)
            for (int cx = 0; cx < cw; cx++)
            {
                int idx = cy * cw + cx;
                var ci = cells[idx];
                bool diffSeed = false, diffRegime = false;

                if (cx > 0) { var o = cells[cy * cw + cx - 1]; if (o.Seed != ci.Seed) diffSeed = true; if (o.Regime != ci.Regime) diffRegime = true; }
                if (cx < cw - 1) { var o = cells[cy * cw + cx + 1]; if (o.Seed != ci.Seed) diffSeed = true; if (o.Regime != ci.Regime) diffRegime = true; }
                if (cy > 0) { var o = cells[(cy - 1) * cw + cx]; if (o.Seed != ci.Seed) diffSeed = true; if (o.Regime != ci.Regime) diffRegime = true; }
                if (cy < ch - 1) { var o = cells[(cy + 1) * cw + cx]; if (o.Seed != ci.Seed) diffSeed = true; if (o.Regime != ci.Regime) diffRegime = true; }

                if (diffRegime || cx == 0 || cx == cw - 1 || cy == 0 || cy == ch - 1)
                {
                    Color c = RegimeBorder;
                    data[idx * 4] = (byte)(c.R * 255); data[idx * 4 + 1] = (byte)(c.G * 255);
                    data[idx * 4 + 2] = (byte)(c.B * 255); data[idx * 4 + 3] = 255;
                }
                else if (diffSeed)
                {
                    Color c = CellBorder;
                    data[idx * 4] = (byte)(c.R * 255); data[idx * 4 + 1] = (byte)(c.G * 255);
                    data[idx * 4 + 2] = (byte)(c.B * 255); data[idx * 4 + 3] = 255;
                }
            }

        var t0 = Time.GetTicksMsec();
        var img = Image.CreateFromData(cw, ch, false, Image.Format.Rgba8, data);
        GD.Print($"  img create: {Time.GetTicksMsec() - t0}ms");

        // centroids
        var centroids = new Vector2[allLocations.Count];
        var count = new int[allLocations.Count];
        var cxSum = new double[allLocations.Count];
        var cySum = new double[allLocations.Count];
        for (int i = 0; i < cellSeeds.Length; i++)
        {
            int s = cellSeeds[i];
            if (s >= 0) { count[s]++; cxSum[s] += (i % cw) * c2w + c2w / 2f; cySum[s] += (i / cw) * c2w + c2w / 2f; }
        }
        for (int i = 0; i < allLocations.Count; i++)
            centroids[i] = count[i] > 0
                ? new Vector2((float)(cxSum[i] / count[i]), (float)(cySum[i] / count[i]))
                : allLocations[i].Position;

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
