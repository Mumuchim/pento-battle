using System.Collections.Generic;
using UnityEngine;

public class DraftManager : MonoBehaviour
{
    [Header("All Pieces in the Pool (drag all 12 here)")]
    public PentominoPiece[] allPieces;

    [Header("Hand Anchors (drag Player1Hand / Player2Hand objects here)")]
    public Transform player1Hand;
    public Transform player2Hand;

    [Header("Draft State")]
    public bool player1Turn = true;
    public bool draftFinished = false;

    [Header("Hands")]
    public List<PentominoPiece> player1 = new();
    public List<PentominoPiece> player2 = new();

    [Header("Hand Layout")]
    [Tooltip("Extra gap between pieces measured in CELLS. Try 1.5 or 2.0 for more spacing.")]
    public float paddingCells = 1.5f;

    [Tooltip("If assigned, hands auto-position above/below board each pick")]
    public Transform boardRoot;

    [Tooltip("Distance away from board bounds (world units)")]
    public float boardClearance = 2.0f;

    public void PickPiece(PentominoPiece piece)
    {
        if (draftFinished) return;
        if (piece == null) return;

        if (player1.Contains(piece) || player2.Contains(piece))
            return;

        if (player1Hand == null || player2Hand == null)
        {
            Debug.LogError("DraftManager: Player hand anchors not assigned!");
            return;
        }

        AutoPlaceHands();

        if (player1Turn)
        {
            player1.Add(piece);

            var state = piece.GetComponent<PieceState>();
            if (state != null)
            {
                state.owner = Owner.P1;
                state.placed = false;
            }

            LayoutHandHorizontal(player1, player1Hand, isTopHand: false);
            Debug.Log("Player 1 picked " + piece.shapeKey);
        }
        else
        {
            player2.Add(piece);

            var state = piece.GetComponent<PieceState>();
            if (state != null)
            {
                state.owner = Owner.P2;
                state.placed = false;
            }

            LayoutHandHorizontal(player2, player2Hand, isTopHand: true);
            Debug.Log("Player 2 picked " + piece.shapeKey);
        }

        player1Turn = !player1Turn;

        if (player1.Count + player2.Count == allPieces.Length)
        {
            draftFinished = true;
            Debug.Log("DRAFT COMPLETE — enable placement now!");
        }
    }

    void LayoutHandHorizontal(List<PentominoPiece> hand, Transform anchor, bool isTopHand)
    {
        if (hand.Count == 0) return;

        float cellSize = hand[0].cellSize;
        float gap = paddingCells * cellSize;

        float totalWidth = 0f;
        float[] widths = new float[hand.Count];

        for (int i = 0; i < hand.Count; i++)
        {
            widths[i] = GetPieceWidthWorld(hand[i]);
            totalWidth += widths[i];
        }
        totalWidth += gap * (hand.Count - 1);

        float x = anchor.position.x - totalWidth * 0.5f;

        for (int i = 0; i < hand.Count; i++)
        {
            var piece = hand[i];
            float w = widths[i];

            x += w * 0.5f;

            Vector2 localCenter = GetPieceLocalCenter(piece);
            float heightWorld = GetPieceHeightWorld(piece);

            float y = anchor.position.y + (isTopHand ? (heightWorld * 0.5f) : -(heightWorld * 0.5f));

            Vector3 desiredCenter = new Vector3(x, y, 0f);
            piece.transform.position = desiredCenter - new Vector3(localCenter.x, localCenter.y, 0f);

            SetSortingOrder(piece, 10);

            x += w * 0.5f + gap;
        }
    }

    float GetPieceWidthWorld(PentominoPiece piece)
    {
        GetCellBounds(piece, out int minX, out int maxX, out _, out _);
        int widthCells = (maxX - minX) + 1;
        return widthCells * piece.cellSize;
    }

    float GetPieceHeightWorld(PentominoPiece piece)
    {
        GetCellBounds(piece, out _, out _, out int minY, out int maxY);
        int heightCells = (maxY - minY) + 1;
        return heightCells * piece.cellSize;
    }

    Vector2 GetPieceLocalCenter(PentominoPiece piece)
    {
        GetCellBounds(piece, out int minX, out int maxX, out int minY, out int maxY);

        float cx = (minX + maxX + 1) * 0.5f * piece.cellSize;
        float cy = (minY + maxY + 1) * 0.5f * piece.cellSize;

        return new Vector2(cx, cy);
    }

    void GetCellBounds(PentominoPiece piece, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = int.MaxValue; maxX = int.MinValue;
        minY = int.MaxValue; maxY = int.MinValue;

        foreach (var c in piece.cells)
        {
            if (c.x < minX) minX = c.x;
            if (c.x > maxX) maxX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.y > maxY) maxY = c.y;
        }

        if (minX == int.MaxValue) { minX = 0; maxX = 0; minY = 0; maxY = 0; }
    }

    void SetSortingOrder(PentominoPiece piece, int order)
    {
        var renderers = piece.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
            r.sortingOrder = order;
    }

    void AutoPlaceHands()
    {
        if (boardRoot == null) return;

        var boardRenderers = boardRoot.GetComponentsInChildren<SpriteRenderer>();
        if (boardRenderers.Length == 0) return;

        Bounds b = boardRenderers[0].bounds;
        for (int i = 1; i < boardRenderers.Length; i++)
            b.Encapsulate(boardRenderers[i].bounds);

        // ✅ center hands horizontally to board center
        float centerX = b.center.x;

        var p2 = player2Hand.position;
        p2.x = centerX;
        p2.y = b.max.y + boardClearance;
        player2Hand.position = p2;

        var p1 = player1Hand.position;
        p1.x = centerX;
        p1.y = b.min.y - boardClearance;
        player1Hand.position = p1;
    }
}