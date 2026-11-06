using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Moves the player's ship left and right, and shoots bullets.
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float edgePadding = 0.6f;   // keeps the ship from touching the screen edge

    [Header("Shooting")]
    public Bullet bulletPrefab;
    public int maxBulletsOnScreen = 1;  // the original arcade game only allowed 1 bullet at a time!
    public float fireCooldown = 0.2f;

    [Header("Getting hit")]
    public GameObject explosionPrefab;
    public float invulnerableTime = 1.5f;

    SpriteRenderer spriteRenderer;
    Collider2D hitBox;
    Vector3 startPosition;
    float nextFireTime;
    bool isAlive = true;
    bool isInvulnerable;

    // Awake runs once, when the object is created - before Start.
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitBox = GetComponent<Collider2D>();
        startPosition = transform.position;
    }

    // Update runs once every frame.
    void Update()
    {
        if (!isAlive || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Read the keyboard: -1 = left, +1 = right, 0 = not moving
        float input = 0f;
        if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) input -= 1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) input += 1f;

        // How wide is the screen in world units? (orthographic size = half the height)
        Camera cam = Camera.main;
        float halfWidth = cam.orthographicSize * cam.aspect;

        // Move, multiplied by deltaTime so the speed is the same on fast and slow computers
        Vector3 pos = transform.position;
        pos.x += input * moveSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -halfWidth + edgePadding, halfWidth - edgePadding);
        transform.position = pos;

        if (kb.spaceKey.wasPressedThisFrame && Time.time >= nextFireTime
            && Bullet.CountActive(Bullet.Owner.Player) < maxBulletsOnScreen)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        nextFireTime = Time.time + fireCooldown;
        // Instantiate = make a copy of a prefab in the scene
        Instantiate(bulletPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        SoundFX.Play(SoundFX.Sound.Shoot);
    }

    // Called by an enemy bullet.
    public void Hit()
    {
        if (!isAlive || isInvulnerable) return;
        Explode();
        GameManager.Instance.PlayerHit();
    }

    public void Explode()
    {
        if (!isAlive) return;
        isAlive = false;
        SetVisible(false);
        Explosion.Spawn(explosionPrefab, transform.position, spriteRenderer.color);
        SoundFX.Play(SoundFX.Sound.PlayerDeath);
    }

    public void ResetPlayer()
    {
        StopAllCoroutines();
        transform.position = startPosition;
        isAlive = true;
        isInvulnerable = false;
        SetVisible(true);
    }

    public void Respawn()
    {
        ResetPlayer();
        StartCoroutine(BlinkWhileInvulnerable());
    }

    IEnumerator BlinkWhileInvulnerable()
    {
        isInvulnerable = true;
        float endTime = Time.time + invulnerableTime;
        while (Time.time < endTime)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
        }
        spriteRenderer.enabled = true;
        isInvulnerable = false;
    }

    void SetVisible(bool visible)
    {
        spriteRenderer.enabled = visible;
        hitBox.enabled = visible;
    }
}
