using UnityEngine;

// The red UFO that sometimes flies across the top of the screen. Shoot it for bonus points!
public class MysteryShip : MonoBehaviour
{
    public float speed = 3f;
    public int[] possiblePoints = { 50, 100, 150, 300 };
    public GameObject explosionPrefab;

    [HideInInspector] public int direction = 1;

    bool dead;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State == GameManager.GameState.Title || gm.State == GameManager.GameState.GameOver) return;

        transform.position += Vector3.right * direction * speed * Time.deltaTime;

        Camera cam = Camera.main;
        float halfWidth = cam.orthographicSize * cam.aspect;
        if (Mathf.Abs(transform.position.x) > halfWidth + 1.5f) Destroy(gameObject);
    }

    public void Die()
    {
        if (dead) return;
        dead = true;
        int points = possiblePoints[Random.Range(0, possiblePoints.Length)];
        GameManager.Instance.AddScore(points);
        Explosion.Spawn(explosionPrefab, transform.position, GetComponent<SpriteRenderer>().color);
        SoundFX.Play(SoundFX.Sound.MysteryKilled);
        Destroy(gameObject);
    }
}
