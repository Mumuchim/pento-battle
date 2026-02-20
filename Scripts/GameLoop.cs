using System.Collections.Generic;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
    public PlacementController placement;
    public DraftManager draft;
    public BoardManager board;

    private Owner currentTurn = Owner.P1;
    private bool placementStarted = false;

    void Start()
    {
        if (placement == null) placement = FindFirstObjectByType<PlacementController>();
        if (draft == null) draft = FindFirstObjectByType<DraftManager>();
        if (board == null) board = FindFirstObjectByType<BoardManager>();
    }

    void Update()
    {
        // Start placement right after draft
        if (!placementStarted && draft != null && draft.draftFinished)
        {
            placementStarted = true;
            currentTurn = Owner.P1;

            Debug.Log("[GameLoop] Placement phase started! Turn: P1");
            if (placement != null) placement.SetTurn(currentTurn);

            // If somehow P1 already has no move (rare), end immediately
            CheckNoMoveAndEndIfNeeded(currentTurn);
        }
    }

    public void EndTurn()
    {
        // Switch
        currentTurn = (currentTurn == Owner.P1) ? Owner.P2 : Owner.P1;

        Debug.Log($"[GameLoop] Turn switched: {currentTurn}");
        if (placement != null) placement.SetTurn(currentTurn);

        // ✅ After switching turns, check if this player can move
        CheckNoMoveAndEndIfNeeded(currentTurn);
    }

    void CheckNoMoveAndEndIfNeeded(Owner playerToMove)
    {
        if (draft == null || board == null) return;

        bool hasMove = PlayerHasAnyLegalMove(playerToMove);

        if (!hasMove)
        {
            Owner winner = (playerToMove == Owner.P1) ? Owner.P2 : Owner.P1;
            Debug.Log($"[GameLoop] NO LEGAL MOVES for {playerToMove}. WINNER: {winner}");

            if (placement != null) placement.enabled = false;
        }
    }

    bool PlayerHasAnyLegalMove(Owner player)
    {
        List<PentominoPiece> pieces = (player == Owner.P1) ? draft.player1 : draft.player2;

        foreach (var piece in pieces)
        {
            if (piece == null) continue;

            var st = piece.GetComponent<PieceState>();
            if (st == null) continue;
            if (st.owner != player) continue;
            if (st.placed) continue;

            if (PieceHasAnyLegalPlacement(piece))
                return true;
        }

        return false;
    }

    bool PieceHasAnyLegalPlacement(PentominoPiece piece)
    {
        // Use canonical cells from data (NOT current rotated state), so scan is accurate
        if (!PentominoData.Shapes.TryGetValue(piece.shapeKey.ToUpper(), out var baseArr))
            return false;

        var baseCells = new List<Vector2Int>(baseArr);
        var orientations = GenerateOrientations(baseCells);

        for (int y = 0; y < board.height; y++)
        {
            for (int x = 0; x < board.width; x++)
            {
                Vector2Int anchor = new Vector2Int(x, y);

                foreach (var orient in orientations)
                {
                    if (IsLegalAt(anchor, orient))
                        return true;
                }
            }
        }

        return false;
    }

    bool IsLegalAt(Vector2Int anchor, List<Vector2Int> shapeCells)
    {
        bool touchesExisting = false;

        foreach (var c in shapeCells)
        {
            Vector2Int bc = anchor + c;

            if (!board.IsInside(bc)) return false;
            if (!board.IsEmpty(bc)) return false;

            if (board.occupiedCount > 0 && board.HasSideNeighborOccupied(bc))
                touchesExisting = true;
        }

        if (board.occupiedCount == 0) return true;
        return touchesExisting;
    }

    // ---- orientation helpers (8 max) ----
    List<List<Vector2Int>> GenerateOrientations(List<Vector2Int> baseCells)
    {
        var result = new List<List<Vector2Int>>();
        var seen = new HashSet<string>();

        void AddIfNew(List<Vector2Int> cells)
        {
            var norm = NormalizeCopy(cells);
            string key = Key(norm);
            if (seen.Add(key)) result.Add(norm);
        }

        // rotations
        var cur = new List<Vector2Int>(baseCells);
        for (int r = 0; r < 4; r++)
        {
            AddIfNew(cur);
            cur = RotateCWCopy(cur);
        }

        // flipped rotations
        cur = FlipXCopy(baseCells);
        for (int r = 0; r < 4; r++)
        {
            AddIfNew(cur);
            cur = RotateCWCopy(cur);
        }

        return result;
    }

    List<Vector2Int> RotateCWCopy(List<Vector2Int> src)
    {
        var o = new List<Vector2Int>(src.Count);
        foreach (var v in src) o.Add(new Vector2Int(-v.y, v.x));
        return o;
    }

    List<Vector2Int> FlipXCopy(List<Vector2Int> src)
    {
        var o = new List<Vector2Int>(src.Count);
        foreach (var v in src) o.Add(new Vector2Int(-v.x, v.y));
        return o;
    }

    List<Vector2Int> NormalizeCopy(List<Vector2Int> src)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var v in src)
        {
            if (v.x < minX) minX = v.x;
            if (v.y < minY) minY = v.y;
        }

        var o = new List<Vector2Int>(src.Count);
        foreach (var v in src) o.Add(new Vector2Int(v.x - minX, v.y - minY));
        return o;
    }

    string Key(List<Vector2Int> cells)
    {
        cells.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var c in cells) sb.Append($"{c.x},{c.y};");
        return sb.ToString();
    }
}