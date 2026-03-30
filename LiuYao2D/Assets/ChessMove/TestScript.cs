using UnityEngine;

public class TestScript : MonoBehaviour
{
    public ChessPiece piece;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            piece.Fire(Vector2.right + Vector2.up);
        }
    }
}