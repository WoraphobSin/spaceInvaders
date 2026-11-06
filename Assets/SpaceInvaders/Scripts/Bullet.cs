using System.Collections.Generic;
using UnityEngine;

// A bullet flies in a straight line and destroys what it hits.
// The same script is used for the player's bullets and the invaders' bullets - "owner" tells them apart.
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public enum Owner { Player, Enemy }

    public Owner owner = Owner.Player;
    public float speed = 14f;
    public Vector2 direction = Vector2.up;
    public float maxDistanceFromCenter = 7f; // destroyed when it flies off screen

    // A list of every bullet currently in the game, so we can count or clear them.
    static readonly List<Bullet> activeBullets = new List<Bullet>();
    bool used;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => activeBullets.Clear();

    public static int CountActive(Owner owner)
    {
        int count = 0;
        foreach (Bullet b in activeBullets)
            if (b.owner == owner) count++;
        return count;
    }

    public static void ClearAll()
    {
        foreach (Bullet b in activeBullets.ToArray())
            Destroy(b.gameObject);
    }

    void OnEnable() => activeBullets.Add(this);
    void OnDisable() => activeBullets.Remove(this);

    void Start()
    {
        // The Rigidbody2D is "Kinematic": it moves at the velocity we give it and ignores gravity.
        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
    }

    void Update()
    {
        if (Mathf.Abs(transform.position.y) > maxDistanceFromCenter)
            Destroy(gameObject);
    }

    // Unity calls this automatically when our trigger collider touches another collider.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;

        if (owner == Owner.Player)
        {
            if (other.TryGetComponent(out Invader invader)) { invader.Die(); Consume(); return; }
            if (other.TryGetComponent(out MysteryShip ship)) { ship.Die(); Consume(); return; }
        }
        else
        {
            if (other.TryGetComponent(out PlayerController player)) { player.Hit(); Consume(); return; }
        }

        if (other.TryGetComponent(out ShieldBlock block)) { block.Damage(); Consume(); return; }

        // Player bullet and enemy bullet hit each other: both disappear
        if (other.TryGetComponent(out Bullet otherBullet) && otherBullet.owner != owner)
        {
            otherBullet.Consume();
            Consume();
        }
    }

    public void Consume()
    {
        if (used) return;
        used = true;
        Destroy(gameObject);
    }
}
