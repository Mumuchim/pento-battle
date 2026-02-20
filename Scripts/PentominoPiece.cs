using System.Collections.Generic;
using UnityEngine;

public class PentominoPiece : MonoBehaviour
{
    public GameObject blockPrefab;
    public float cellSize = 1f;

    [Header("Pick a Shape Key (F I L P N T U V W X Y Z)")]
    public string shapeKey = "I";

    [Header("Visual")]
    public Color pieceColor = new Color(0.6f, 0.4f, 1f, 1f);

    [Header("Rendering (kept even after rotate/flip)")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 10;

    public List<Vector2Int> cells = new List<Vector2Int>();

    // Reuse blocks instead of destroying/recreating every rotate/flip
    private readonly List<GameObject> _blocks = new List<GameObject>();
    private bool _adoptedChildren = false;

#if UNITY_EDITOR
    private bool _rebuildQueued = false;
#endif

    void OnValidate()
    {
        AutoKeyFromName();

#if UNITY_EDITOR
        // Avoid Instantiate spam inside OnValidate; queue once
        if (!_rebuildQueued)
        {
            _rebuildQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _rebuildQueued = false;
                if (this == null) return;

                // In edit mode, keep visuals matching key without duplicating
                if (!Application.isPlaying)
                {
                    AdoptExistingChildBlocks();
                    SetShape(shapeKey);
                    RebuildVisualOnly();
                }
            };
        }
#endif
    }

    void Awake()
    {
        // Important: adopt existing children early so we don't double-spawn
        AdoptExistingChildBlocks();
    }

    void Start()
    {
        AutoKeyFromName();
        SetShape(shapeKey);

        AdoptExistingChildBlocks();  // safe to call again
        RebuildVisualOnly();
    }

    void AutoKeyFromName()
    {
        if (name.StartsWith("Piece_") && name.Length >= 7)
        {
            string k = name.Substring(6, 1).ToUpper();
            if (PentominoData.Shapes.ContainsKey(k))
                shapeKey = k;
        }
    }

    // Call from PlacementController when selecting/deselecting/placing
    public void SetRenderOrder(int order, string layer = null)
    {
        sortingOrder = order;
        if (!string.IsNullOrEmpty(layer)) sortingLayerName = layer;
        ApplyRenderSettings();
    }

    public void SetShape(string key)
    {
        shapeKey = key.ToUpper().Trim();

        if (!PentominoData.Shapes.ContainsKey(shapeKey))
        {
            cells.Clear();
            return;
        }

        cells = new List<Vector2Int>(PentominoData.Shapes[shapeKey]);
        Normalize();
    }

    /// <summary>
    /// If the prefab/scene already has Block children from the old system,
    /// we "adopt" them into _blocks so we DON'T instantiate duplicates.
    /// </summary>
    void AdoptExistingChildBlocks()
    {
        if (_adoptedChildren) return;
        _adoptedChildren = true;

        _blocks.Clear();

        // Adopt any existing children as blocks (old Build() used Block_ names)
        for (int i = 0; i < transform.childCount; i++)
        {
            var ch = transform.GetChild(i);
            if (ch == null) continue;

            // Only adopt objects that look like blocks
            var sr = ch.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            if (ch.name.StartsWith("Block_") || ch.GetComponent<Collider2D>() != null)
            {
                _blocks.Add(ch.gameObject);
            }
        }
    }

    public void RebuildVisualOnly()
    {
        if (blockPrefab == null) return;

        // Make sure we are adopting first (prevents duplicates)
        AdoptExistingChildBlocks();

        EnsureBlockCount(cells.Count);

        for (int i = 0; i < _blocks.Count; i++)
        {
            var b = _blocks[i];
            if (b == null) continue;

            var c = cells[i];

            b.transform.localPosition = new Vector3(c.x * cellSize, c.y * cellSize, 0f);
            b.name = $"Block_{c.x}_{c.y}";

            var sr = b.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = pieceColor;
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sortingOrder;
            }
        }
    }

    void EnsureBlockCount(int needed)
    {
        // Add missing blocks
        while (_blocks.Count < needed)
        {
            GameObject b = Instantiate(blockPrefab, transform);
            _blocks.Add(b);
        }

        // Remove extras
        for (int i = _blocks.Count - 1; i >= needed; i--)
        {
            if (_blocks[i] != null)
            {
                if (Application.isPlaying) Destroy(_blocks[i]);
                else DestroyImmediate(_blocks[i]);
            }
            _blocks.RemoveAt(i);
        }
    }

    void ApplyRenderSettings()
    {
        for (int i = 0; i < _blocks.Count; i++)
        {
            var b = _blocks[i];
            if (b == null) continue;

            var sr = b.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
            sr.color = pieceColor;
        }
    }

    public void RotateCW()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            var v = cells[i];
            cells[i] = new Vector2Int(-v.y, v.x);
        }
        Normalize();
        RebuildVisualOnly();
    }

    public void FlipX()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            var v = cells[i];
            cells[i] = new Vector2Int(-v.x, v.y);
        }
        Normalize();
        RebuildVisualOnly();
    }

    void Normalize()
    {
        if (cells.Count == 0) return;

        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var v in cells)
        {
            if (v.x < minX) minX = v.x;
            if (v.y < minY) minY = v.y;
        }

        for (int i = 0; i < cells.Count; i++)
            cells[i] = new Vector2Int(cells[i].x - minX, cells[i].y - minY);
    }
}