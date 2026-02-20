using UnityEngine;

public enum Owner { None, P1, P2 }

public class PieceState : MonoBehaviour
{
    public Owner owner = Owner.None;
    public bool placed = false;
}