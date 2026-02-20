using UnityEngine;

public class PoolLayout : MonoBehaviour
{
    public PentominoPiece[] pieces;

    [Header("Pool Grid")]
    public int columns = 6;
    public float spacingX = 3.0f;
    public float spacingY = 3.0f;

    [Header("Pool Start (top-left)")]
    public Vector2 startPos = new Vector2(-6f, 6f);

    void Start()
    {
        LayoutPieces();
    }

    public void LayoutPieces()
    {
        if (pieces == null) return;

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null) continue;

            int row = i / columns;
            int col = i % columns;

            var pos = startPos + new Vector2(col * spacingX, -row * spacingY);
            pieces[i].transform.position = pos;
        }
    }
}