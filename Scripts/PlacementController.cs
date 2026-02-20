using System.Collections.Generic;
using UnityEngine;

public class PlacementController : MonoBehaviour
{
    public BoardManager board;
    public Camera cam;
    public GameLoop loop;

    private Owner currentTurn = Owner.P1;
    private PentominoPiece selectedPiece;

    private Vector3 grabOffset;
    private Vector3 originalPos;
    private bool isDragging = false;

    private readonly Color validTint = new Color(0.7f, 1f, 0.7f, 1f);
    private readonly Color invalidTint = new Color(1f, 0.6f, 0.6f, 1f);

    private const int SelectedSortingOrder = 100;
    private const int NormalHandSortingOrder = 10;

    // ✅ NEW: placed pieces should not tie with board (often 0)
    private const int PlacedSortingOrder = 20;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (board == null) board = FindFirstObjectByType<BoardManager>();
        if (loop == null) loop = FindFirstObjectByType<GameLoop>();
    }

    public void SetTurn(Owner turn)
    {
        currentTurn = turn;
        DropSelection(forceReset: true);
        Debug.Log($"[Placement] Turn set to {currentTurn}");
    }

    void Update()
    {
        if (board == null || cam == null || loop == null) return;

        // cancel selection
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            DropSelection(forceReset: true);
            return;
        }

        // click to pick / switch
        if (Input.GetMouseButtonDown(0))
        {
            TryStartOrSwitchSelection();
        }

        // drag update + rotate/flip
        if (isDragging && selectedPiece != null && !IsPlaced(selectedPiece))
        {
            FollowMouseSmart();

            // ✅ E rotate, Q flip
            if (Input.GetKeyDown(KeyCode.E))
                RotateSelectedAnchoredAndKeepVisible();

            if (Input.GetKeyDown(KeyCode.Q))
                FlipSelectedAnchoredAndKeepVisible();

            bool canPlace = CanPlaceHere(out _);
            TintSelected(canPlace ? validTint : invalidTint);

            // IMPORTANT: Rebuild recreates renderers, so keep sorting every frame while dragging
            SetSortingOrder(selectedPiece, SelectedSortingOrder);
        }

        // release to place
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging && selectedPiece != null && !IsPlaced(selectedPiece))
            {
                bool canPlace = CanPlaceHere(out Vector2Int anchorGrid);

                if (canPlace)
                {
                    PlacePiece(anchorGrid);

                    var st = selectedPiece.GetComponent<PieceState>();
                    if (st != null) st.placed = true;

                    DisablePieceColliders(selectedPiece);
                    ResetTint(selectedPiece);

                    // ✅ FIX: avoid tie with board sorting (random draw order)
                    SetSortingOrder(selectedPiece, PlacedSortingOrder);

                    // ✅ (extra safe) force Z = 0 so it doesn't drift behind
                    var p = selectedPiece.transform.position;
                    p.z = 0f;
                    selectedPiece.transform.position = p;

                    selectedPiece = null;
                    isDragging = false;

                    loop.EndTurn();
                }
                else
                {
                    ReturnToOriginal();
                }
            }
        }
    }

    // ----------------------------
    // Selection
    // ----------------------------
    void TryStartOrSwitchSelection()
    {
        var pieceUnderMouse = GetOwnUnplacedPieceUnderMouse();
        if (pieceUnderMouse == null) return;

        // switch piece mid-drag
        if (selectedPiece != null && pieceUnderMouse != selectedPiece)
        {
            ReturnToOriginal();
        }

        selectedPiece = pieceUnderMouse;
        originalPos = selectedPiece.transform.position;

        Vector3 mouseWorld = GetMouseWorld3D();
        mouseWorld.z = 0f;
        grabOffset = selectedPiece.transform.position - mouseWorld;

        isDragging = true;

        SetSortingOrder(selectedPiece, SelectedSortingOrder);

        Debug.Log($"[Placement] Selected {selectedPiece.name} ({selectedPiece.shapeKey})");
    }

    PentominoPiece GetOwnUnplacedPieceUnderMouse()
    {
        Vector2 world = GetMouseWorld2D();
        var hits = Physics2D.OverlapPointAll(world);

        if (hits == null || hits.Length == 0) return null;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            var piece = hits[i].GetComponentInParent<PentominoPiece>();
            if (piece == null) continue;

            var st = piece.GetComponent<PieceState>();
            if (st == null) continue;

            if (st.owner != currentTurn) continue;
            if (st.placed) continue;

            return piece;
        }

        return null;
    }

    // ----------------------------
    // Movement + anchor-safe rotate
    // ----------------------------
    void FollowMouseSmart()
    {
        Vector3 mouse = GetMouseWorld3D();
        mouse.z = 0f;

        Vector3 freePos = mouse + grabOffset;

        // snap if mouse is inside board
        if (board.WorldToGrid(mouse, out Vector2Int anchor))
            selectedPiece.transform.position = board.GridToWorld(anchor);
        else
            selectedPiece.transform.position = freePos;
    }

    void RotateSelectedAnchoredAndKeepVisible()
    {
        if (selectedPiece == null) return;

        Vector3 beforeWorld = selectedPiece.transform.position;
        bool onBoard = board.WorldToGrid(beforeWorld, out Vector2Int anchor);

        selectedPiece.RotateCW(); // this Rebuild() recreates renderers

        // keep same anchor/position
        if (onBoard)
            selectedPiece.transform.position = board.GridToWorld(anchor);
        else
            selectedPiece.transform.position = beforeWorld;

        // ✅ keep visible after rebuild
        SetSortingOrder(selectedPiece, SelectedSortingOrder);

        // re-tint immediately after rebuild
        bool canPlace = CanPlaceHere(out _);
        TintSelected(canPlace ? validTint : invalidTint);
    }

    void FlipSelectedAnchoredAndKeepVisible()
    {
        if (selectedPiece == null) return;

        Vector3 beforeWorld = selectedPiece.transform.position;
        bool onBoard = board.WorldToGrid(beforeWorld, out Vector2Int anchor);

        selectedPiece.FlipX(); // this Rebuild() recreates renderers

        if (onBoard)
            selectedPiece.transform.position = board.GridToWorld(anchor);
        else
            selectedPiece.transform.position = beforeWorld;

        // ✅ keep visible after rebuild
        SetSortingOrder(selectedPiece, SelectedSortingOrder);

        bool canPlace = CanPlaceHere(out _);
        TintSelected(canPlace ? validTint : invalidTint);
    }

    // ----------------------------
    // Placement rules
    // ----------------------------
    bool CanPlaceHere(out Vector2Int anchorGrid)
    {
        anchorGrid = default;

        if (selectedPiece == null) return false;

        if (!board.WorldToGrid(selectedPiece.transform.position, out anchorGrid))
            return false;

        bool touchesExisting = false;

        foreach (var c in selectedPiece.cells)
        {
            Vector2Int boardCell = anchorGrid + c;

            if (!board.IsInside(boardCell)) return false;
            if (!board.IsEmpty(boardCell)) return false;

            if (board.occupiedCount > 0 && board.HasSideNeighborOccupied(boardCell))
                touchesExisting = true;
        }

        if (board.occupiedCount == 0) return true;
        return touchesExisting;
    }

    void PlacePiece(Vector2Int anchorGrid)
    {
        foreach (var c in selectedPiece.cells)
        {
            Vector2Int boardCell = anchorGrid + c;
            board.SetOccupied(boardCell, true);
        }

        Debug.Log($"[Placement] Placed {selectedPiece.shapeKey}");
    }

    // ----------------------------
    // Helpers
    // ----------------------------
    void ReturnToOriginal()
    {
        if (selectedPiece == null) return;

        selectedPiece.transform.position = originalPos;
        ResetTint(selectedPiece);
        SetSortingOrder(selectedPiece, NormalHandSortingOrder);

        isDragging = false;
    }

    void DropSelection(bool forceReset)
    {
        if (selectedPiece == null)
        {
            isDragging = false;
            return;
        }

        if (forceReset)
            selectedPiece.transform.position = originalPos;

        ResetTint(selectedPiece);
        SetSortingOrder(selectedPiece, NormalHandSortingOrder);

        selectedPiece = null;
        isDragging = false;
    }

    bool IsPlaced(PentominoPiece piece)
    {
        var st = piece.GetComponent<PieceState>();
        return st != null && st.placed;
    }

    void DisablePieceColliders(PentominoPiece piece)
    {
        var cols = piece.GetComponentsInChildren<Collider2D>();
        foreach (var c in cols)
            c.enabled = false;
    }

    void TintSelected(Color c)
    {
        if (selectedPiece == null) return;
        var srs = selectedPiece.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
            sr.color = c;
    }

    void ResetTint(PentominoPiece piece)
    {
        if (piece == null) return;
        var srs = piece.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
            sr.color = piece.pieceColor;
    }

    void SetSortingOrder(PentominoPiece piece, int order)
    {
        if (piece == null) return;
        var srs = piece.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
            sr.sortingOrder = order;
    }

    Vector3 GetMouseWorld3D()
    {
        Vector3 mp = Input.mousePosition;
        mp.z = -cam.transform.position.z;
        return cam.ScreenToWorldPoint(mp);
    }

    Vector2 GetMouseWorld2D()
    {
        var w = GetMouseWorld3D();
        return new Vector2(w.x, w.y);
    }
}