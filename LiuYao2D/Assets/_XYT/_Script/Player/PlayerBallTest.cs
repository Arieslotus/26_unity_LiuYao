using UnityEngine;

public class PlayerBallTest : MonoBehaviour, IAttackable
{
    public int hp = 10;

    public void TakeDamage(int damage)
    {
        hp -= damage;
        Debug.Log(name + " ±ª¥Ú¡À£¨ £”‡HP: " + hp);

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }
}