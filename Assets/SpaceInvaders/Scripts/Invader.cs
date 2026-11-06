using UnityEngine;

// One alien. It switches between two pictures each step (that's the "walking" animation)
// and gives points when it is shot.
public class Invader : MonoBehaviour
{
    public int points = 10;
    public Sprite[] animationFrames;
    public GameObject explosionPrefab;

    [HideInInspector] public int column;
    [HideInInspector] public InvaderGrid grid;

    SpriteRenderer spriteRenderer;
    int frame;
    bool dead;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void NextFrame()
    {
        if (animationFrames == null || animationFrames.Length == 0) return;
        frame = (frame + 1) % animationFrames.Length;
        spriteRenderer.sprite = animationFrames[frame];
    }

    public void Die()
    {
        if (dead) return;
        dead = true;
        Explosion.Spawn(explosionPrefab, transform.position, spriteRenderer.color);
        SoundFX.Play(SoundFX.Sound.InvaderKilled);
        GameManager.Instance.AddScore(points);
        if (grid != null) grid.OnInvaderKilled(this);
        Destroy(gameObject);
    }
}
