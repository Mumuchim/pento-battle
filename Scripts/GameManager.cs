using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Draft,
        Placement,
        GameOver
    }

    public GameState currentState = GameState.Draft;

    void Start()
    {
        Debug.Log("Pentomino Battle started!");
    }
}