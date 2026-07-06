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
    private static readonly Color RegimeBorder = new Color(0.06f, 0.04f, 0.03f);

    public struct CellInfo { public int Seed; public int Regime; }

    public class Result
    {
        public ImageTexture Texture;
        public Vector2[] Centroids;
        public int[] CellSeeds; // flat array cw*ch, seed index at each cell
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

        var img = Image.CreateEmpty(cw, ch, false, Image.Format.Rgba8);
        var cells = new CellInfo[cw * ch];
        var cellSeeds = new int[cw * ch];
        var sumX = new double[allLocations.Count];
        var sumY = new double[allLocations.Count];
        var count = new int[allLocations.Count];

        for (int cy = 0; cy < ch; cy++)
            for (int cx = 0; cx < cw; cx++)
            {
                float wx = cx * c2w + c2w / 2f;
                float wy = cy * c2w + c2w / 2f;
                int seed = FindSeed(wx, wy);
                if (seed < 0) { img.SetPixel(cx, cy, Neutral); cellSeeds[cy * cw + cx] = -1; continue; }
                int regime = allLocations[seed].OwnerIndex;
                cells[cy * cw + cx] = new CellInfo { Seed = seed, Regime = regime };
                cellSeeds[cy * cw + cx] = seed;
                img.SetPixel(cx, cy, GetColor(regime));
                sumX[seed] += wx;
                sumY[seed] += wy;
                count[seed]++;
            }

        var centroids = new Vector2[allLocations.Count];
        for (int i = 0; i < allLocations.Count; i++)
        {
            if (count[i] > 0)
                centroids[i] = new Vector2((float)(sumX[i] / count[i]), (float)(sumY[i] / count[i]));
            else
                centroids[i] = allLocations[i].Position;
        }

        for (int cy = 0; cy < ch; cy++)
            for (int cx = 0; cx < cw; cx++)
            {
                int idx = cy * cw + cx;
                var ci = cells[idx];
                bool diffRegime = false;

                void Check(int nx, int ny)
                {
                    if (nx < 0 || nx >= cw || ny < 0 || ny >= ch)
                    { diffRegime = true; return; }
                    if (cells[ny * cw + nx].Regime != ci.Regime)
                        diffRegime = true;
                }

                Check(cx - 1, cy); Check(cx + 1, cy);
                Check(cx, cy - 1); Check(cx, cy + 1);

                if (diffRegime) img.SetPixel(cx, cy, RegimeBorder);
            }

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
