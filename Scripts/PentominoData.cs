using System.Collections.Generic;
using UnityEngine;

public static class PentominoData
{
    // Canonical (one orientation) for each pentomino (normalized)
    public static readonly Dictionary<string, Vector2Int[]> Shapes = new()
    {
        { "I", new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0) } },

        { "L", new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(1,0) } },

        { "P", new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(0,2) } },

        { "N", new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(1,3) } },

        { "T", new[] { new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(1,1), new Vector2Int(1,0) } },

        { "U", new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(2,1) } },

        { "V", new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,0), new Vector2Int(2,0) } },

        { "W", new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(2,2) } },

        { "X", new[] { new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(1,2) } },

        { "Y", new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(1,2) } },

        // ✅ FIXED Z (matches your purple reference)
        // Shape:
        // XX.
        // .X.
        // .XX
        { "Z", new[] {
            new Vector2Int(0,2), new Vector2Int(1,2),
            new Vector2Int(1,1),
            new Vector2Int(1,0), new Vector2Int(2,0)
        }},

        { "F", new[] { new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(2,2) } },
    };
}