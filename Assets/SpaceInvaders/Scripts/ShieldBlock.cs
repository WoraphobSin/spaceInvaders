using UnityEngine;

// A small piece of a shield. It fades a little each time it is hit, then breaks.
public class ShieldBlock : MonoBehaviour
{
    public int health = 3;

    SpriteRenderer spriteRenderer;
    Color baseColor;
    int maxHealth;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
        maxHealth = health;
    }

    public void Damage()
    {
        health--;
        if (health <= 0)
        {
            Destroy(gameObject);
            return;
        }
        Color c = baseColor;
        c.a = (float)health / maxHealth;
        spriteRenderer.color = c;
    }
}
