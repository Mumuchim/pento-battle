using UnityEngine;

public class DraftClickPicker : MonoBehaviour
{
    public DraftManager draft;
    public Camera cam;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (draft == null) draft = GetComponent<DraftManager>();

        Debug.Log($"[DraftClickPicker] Awake. cam={(cam != null)}, draft={(draft != null)}");
    }

    void Update()
    {
        // ✅ Make sure script is alive
        // (You can comment this out later, it will spam)
        // Debug.Log("[DraftClickPicker] Update running");

        // ✅ Stop draft clicking once draft is finished
        if (draft != null && draft.draftFinished) return;

        if (draft == null || cam == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            // ✅ FIX: Correct ScreenToWorldPoint depth for 2D plane at Z=0
            Vector3 mp = Input.mousePosition;
            mp.z = -cam.transform.position.z;

            Vector3 w3 = cam.ScreenToWorldPoint(mp);
            Vector2 world = new Vector2(w3.x, w3.y);

            Collider2D[] hits = Physics2D.OverlapPointAll(world);

            Debug.Log($"[DraftClickPicker] Click at {world}. Colliders hit: {hits.Length}");

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;

                // ✅ collider might be on child, so use InParent
                PentominoPiece piece = hits[i].GetComponentInParent<PentominoPiece>();

                Debug.Log($"  - hit: {hits[i].name}, piece={(piece ? piece.name : "NONE")}");

                if (piece != null)
                {
                    draft.PickPiece(piece);
                    return;
                }
            }
        }
    }
}