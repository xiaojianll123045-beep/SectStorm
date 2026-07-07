using Godot;
using System.Collections.Generic;
using System.Linq;

public static class PathFinder
{
    public static List<Vector2> FindPath(Vector2 from, Vector2 to, int armySectId, GameState state, List<LocationData> locations)
    {
        // check if any territory along the path is blocked
        // sample 5 points along the line
        for (int i = 0; i <= 4; i++)
        {
            float t = i / 4f;
            float px = from.X + (to.X - from.X) * t;
            float py = from.Y + (to.Y - from.Y) * t;

            int owner = -1;
            float bestDist = 300f * 300f;
            foreach (var loc in locations)
            {
                if (loc.OwnerSectId < 0) continue;
                float dx = px - loc.Position.X;
                float dy = py - loc.Position.Y;
                float d = dx * dx + dy * dy;
                if (d < bestDist) { bestDist = d; owner = loc.OwnerSectId; }
            }

            if (owner >= 0 && owner != armySectId)
            {
                var rel = state.GetRelation(armySectId, owner);
                if (rel == null) continue; // no relation = passable
                if (rel.State == RelationState.Ally || rel.State == RelationState.War) continue; // ally/enemy = passable
                if (rel.Favor < -20) return null; // hostile neutral = blocked
            }
        }

        return new List<Vector2> { to };
    }
}
