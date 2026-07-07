using Godot;
using System.Collections.Generic;
using System.Linq;

public static class FogOfWar
{
    /// <summary>
    /// Check if a position is visible to the given sect.
    /// Visible = within owned/controlled territory + one adjacent tile,
    /// or within 200px of any army belonging to the sect or its allies.
    /// </summary>
    public static bool IsVisible(Vector2 pos, int sectId, GameState state, List<LocationData> locations,
                                  List<ArmyData> armies)
    {
        var sect = state.GetSect(sectId);
        if (sect == null) return false;

        // check owned territory
        foreach (var loc in locations)
        {
            if (loc.OwnerSectId != sectId) continue;
            if ((pos - loc.Position).Length() < 600f) return true;
        }

        // check allied territory
        foreach (var loc in locations)
        {
            var rel = state.GetRelation(sectId, loc.OwnerSectId);
            if (rel == null || rel.State != RelationState.Ally) continue;
            if ((pos - loc.Position).Length() < 600f) return true;
        }

        // check own armies
        foreach (var army in armies)
        {
            if (army.SectId != sectId) continue;
            if ((pos - army.Position).Length() < 250f) return true;
        }

        // check allied armies
        foreach (var army in armies)
        {
            var rel = state.GetRelation(sectId, army.SectId);
            if (rel == null || rel.State != RelationState.Ally) continue;
            if ((pos - army.Position).Length() < 250f) return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a fog-of-war image where visible=transparent, hidden=dark.
    /// </summary>
    public static Image GenerateFogImage(int mapW, int mapH, int cellSize,
        int sectId, GameState state, List<LocationData> locations, List<ArmyData> armies)
    {
        int cw = mapW / cellSize;
        int ch = mapH / cellSize;
        var img = Image.CreateEmpty(cw, ch, false, Image.Format.Rgba8);
        Color visible = new Color(0, 0, 0, 0);
        Color hidden = new Color(0, 0, 0, 1f);

        var visibleCells = new HashSet<(int, int)>();

        // mark visible cells from locations
        foreach (var loc in locations)
        {
            if (loc.OwnerSectId == sectId)
                MarkRadius(loc.Position, 600f, cellSize, cw, ch, visibleCells);
            else
            {
                var rel = state.GetRelation(sectId, loc.OwnerSectId);
                if (rel != null && rel.State == RelationState.Ally)
                    MarkRadius(loc.Position, 600f, cellSize, cw, ch, visibleCells);
            }
        }

        // mark visible cells from armies
        foreach (var army in armies)
        {
            if (army.SectId == sectId)
                MarkRadius(army.Position, 250f, cellSize, cw, ch, visibleCells);
            else
            {
                var rel = state.GetRelation(sectId, army.SectId);
                if (rel != null && rel.State == RelationState.Ally)
                    MarkRadius(army.Position, 250f, cellSize, cw, ch, visibleCells);
            }
        }

        // edge visible cells = also visible (adjacent to visible)
        var edgeCells = new HashSet<(int, int)>();
        foreach (var (cx, cy) in visibleCells)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (nx >= 0 && nx < cw && ny >= 0 && ny < ch && !visibleCells.Contains((nx, ny)))
                        edgeCells.Add((nx, ny));
                }
        }
        foreach (var c in edgeCells) visibleCells.Add(c);

        // fill image
        for (int cy = 0; cy < ch; cy++)
            for (int cx = 0; cx < cw; cx++)
                img.SetPixel(cx, cy, visibleCells.Contains((cx, cy)) ? visible : hidden);

        return img;
    }

    private static void MarkRadius(Vector2 center, float radius, int cellSize, int cw, int ch,
                                    HashSet<(int, int)> cells)
    {
        int cx = (int)(center.X / cellSize);
        int cy = (int)(center.Y / cellSize);
        int r = (int)(radius / cellSize) + 1;
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (nx >= 0 && nx < cw && ny >= 0 && ny < ch)
                    if (dx * dx + dy * dy <= r * r)
                        cells.Add((nx, ny));
            }
    }
}
