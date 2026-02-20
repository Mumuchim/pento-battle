using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DraftSelectable : MonoBehaviour
{
    public DraftManager draft;
    PentominoPiece piece;

    void Awake()
    {
        piece = GetComponent<PentominoPiece>();
    }

    void OnMouseDown()
    {
        if (draft == null) return;

        draft.PickPiece(piece);
    }
}