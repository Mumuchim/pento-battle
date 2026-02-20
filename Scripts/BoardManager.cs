using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Size")]
    public int width = 10;
    public int height = 6;

    [Header("Cell Prefab")]
    public GameObject cellPrefab;

    [Header("Layout")]
    public float cellSize = 1f;

    // board[x,y] = 0 empty, 1 occupied
    private int[,] board;

    // total occupied squares (for "first move" rule)
    public int occupiedCount { get; private set; } = 0;

    // Grid origin in world space (CENTER of cell 0,0)
    private Vector3 origin;

    void Awake()
    {
        board = new int[width, height];
        GenerateGrid();
    }

    void GenerateGrid()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("BoardManager: cellPrefab is not assigned!");
            return;
        }

        // If origin is the CENTER of cell (0,0), then total span is (width-1)*cellSize
        float offsetX = (width - 1) * cellSize * 0.5f;
        float offsetY = (height - 1) * cellSize * 0.5f;

        // centered around (0,0)
        origin = new Vector3(-offsetX, -offsetY, 0f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = origin + new Vector3(x * cellSize, y * cellSize, 0f);
                var cell = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                cell.name = $"Cell_{x}_{y}";
            }
        }

        Debug.Log($"Generated grid: {width} x {height}");
    }

    public bool WorldToGrid(Vector3 world, out Vector2Int cell)
    {
        Vector3 local = world - origin;

        // Safer snapping: nearest cell center
        int x = Mathf.FloorToInt(local.x / cellSize + 0.5f);
        int y = Mathf.FloorToInt(local.y / cellSize + 0.5f);

        cell = new Vector2Int(x, y);
        return IsInside(cell);
    }

    public Vector3 GridToWorld(Vector2Int cell)
    {
        // world position of cell center
        return origin + new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
    }

    public bool IsInside(Vector2Int c)
    {
        return c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;
    }

    public bool IsEmpty(Vector2Int c)
    {
        return IsInside(c) && board[c.x, c.y] == 0;
    }

    public void SetOccupied(Vector2Int c, bool occupied)
    {
        if (!IsInside(c)) return;

        int before = board[c.x, c.y];
        int after = occupied ? 1 : 0;

        if (before == after) return;

        board[c.x, c.y] = after;
        occupiedCount += (after - before);
    }

    public bool HasSideNeighborOccupied(Vector2Int c)
    {
        Vector2Int[] dirs =
        {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1),
        };

        foreach (var d in dirs)
        {
            Vector2Int n = c + d;
            if (IsInside(n) && board[n.x, n.y] == 1)
                return true;
        }
        return false;
    }
}