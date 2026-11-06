using System.Collections.Generic;
using UnityEngine;

// Creates the rows of invaders and moves them all together, like the original arcade game:
// step sideways, and when one touches the edge, the whole group drops down and turns around.
// The fewer invaders are left, the faster they move!
public class InvaderGrid : MonoBehaviour
{
    [Header("Formation")]
    public Invader[] rowPrefabs;          // one prefab per row, from top to bottom
    public int columns = 11;
    public Vector2 spacing = new Vector2(0.8f, 0.65f);
    public float extraDropPerWave = 0.3f; // later waves start a bit lower
    public int maxExtraDropWaves = 4;

    [Header("Movement")]
    public float stepDistance = 0.15f;
    public float dropDistance = 0.35f;
    public float slowestStepTime = 0.6f;  // seconds between steps when all invaders are alive
    public float fastestStepTime = 0.04f; // seconds between steps when only one is left
    public float edgePadding = 0.5f;
    public float landingHeight = -3.4f;   // if an invader gets this low, it's game over

    [Header("Shooting")]
    public Bullet bulletPrefab;
    public int maxBulletsOnScreen = 3;
    public float minFireDelay = 0.4f;
    public float maxFireDelay = 1.6f;
    [Range(0f, 1f)] public float aimAtPlayerChance = 0.3f;

    [Header("Mystery ship")]
    public MysteryShip mysteryShipPrefab;
    public float mysteryShipHeight = 4.0f;
    public float minMysteryDelay = 15f;
    public float maxMysteryDelay = 30f;

    readonly List<Invader> invaders = new List<Invader>();
    Vector3 startPosition;
    int direction = 1;   // +1 = moving right, -1 = moving left
    int totalCount;
    int wave = 1;
    float stepTimer;
    float fireTimer;
    float mysteryTimer;
    int marchNote;
    MysteryShip currentShip;

    void Awake()
    {
        startPosition = transform.position;
    }

    public void SpawnWave(int waveNumber)
    {
        wave = waveNumber;

        // Remove invaders left over from a previous game
        foreach (Transform child in transform) Destroy(child.gameObject);
        invaders.Clear();
        if (currentShip) Destroy(currentShip.gameObject);

        int extraDrop = Mathf.Min(waveNumber - 1, maxExtraDropWaves);
        transform.position = startPosition + Vector3.down * extraDrop * extraDropPerWave;
        direction = 1;

        for (int row = 0; row < rowPrefabs.Length; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Invader invader = Instantiate(rowPrefabs[row], transform);
                invader.transform.localPosition = new Vector3((col - (columns - 1) / 2f) * spacing.x, -row * spacing.y, 0f);
                invader.column = col;
                invader.grid = this;
                invaders.Add(invader);
            }
        }

        totalCount = invaders.Count;
        stepTimer = CurrentStepTime();
        fireTimer = Random.Range(minFireDelay, maxFireDelay);
        mysteryTimer = Random.Range(minMysteryDelay, maxMysteryDelay);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying || invaders.Count == 0) return;

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            Step();
            stepTimer = CurrentStepTime();
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            TryFire();
            fireTimer = Random.Range(minFireDelay, maxFireDelay) / (1f + 0.1f * (wave - 1));
        }

        if (mysteryShipPrefab != null && currentShip == null)
        {
            mysteryTimer -= Time.deltaTime;
            if (mysteryTimer <= 0f)
            {
                SpawnMysteryShip();
                mysteryTimer = Random.Range(minMysteryDelay, maxMysteryDelay);
            }
        }
    }

    float CurrentStepTime()
    {
        float aliveFraction = totalCount > 0 ? (float)invaders.Count / totalCount : 0f;
        float time = Mathf.Lerp(fastestStepTime, slowestStepTime, aliveFraction);
        return time / (1f + 0.15f * (wave - 1)); // each wave is a little faster
    }

    void Step()
    {
        Camera cam = Camera.main;
        float limit = cam.orthographicSize * cam.aspect - edgePadding;

        // Would any invader go past the edge with the next step?
        bool hitEdge = false;
        foreach (Invader invader in invaders)
        {
            float nextX = invader.transform.position.x + direction * stepDistance;
            if (nextX > limit || nextX < -limit) { hitEdge = true; break; }
        }

        // We move the parent object - all the invaders are its children, so they move with it.
        if (hitEdge)
        {
            transform.position += Vector3.down * dropDistance;
            direction = -direction;
        }
        else
        {
            transform.position += Vector3.right * direction * stepDistance;
        }

        foreach (Invader invader in invaders) invader.NextFrame();
        SoundFX.PlayMarch(marchNote);
        marchNote = (marchNote + 1) % 4;

        foreach (Invader invader in invaders)
        {
            if (invader.transform.position.y <= landingHeight)
            {
                GameManager.Instance.InvadersLanded();
                return;
            }
        }
    }

    void TryFire()
    {
        if (invaders.Count == 0 || Bullet.CountActive(Bullet.Owner.Enemy) >= maxBulletsOnScreen) return;

        // Pick a random invader... or sometimes the one closest to the player
        Invader shooter = invaders[Random.Range(0, invaders.Count)];
        PlayerController player = GameManager.Instance.player;
        if (player != null && Random.value < aimAtPlayerChance)
        {
            float bestDistance = float.MaxValue;
            foreach (Invader invader in invaders)
            {
                float distance = Mathf.Abs(invader.transform.position.x - player.transform.position.x);
                if (distance < bestDistance) { bestDistance = distance; shooter = invader; }
            }
        }

        // Only the lowest invader in a column shoots, so they never shoot their friends
        Invader lowest = shooter;
        foreach (Invader invader in invaders)
        {
            if (invader.column == shooter.column && invader.transform.position.y < lowest.transform.position.y)
                lowest = invader;
        }

        Instantiate(bulletPrefab, lowest.transform.position + Vector3.down * 0.4f, Quaternion.identity);
    }

    void SpawnMysteryShip()
    {
        Camera cam = Camera.main;
        float halfWidth = cam.orthographicSize * cam.aspect;
        int dir = Random.value < 0.5f ? 1 : -1;
        Vector3 position = new Vector3(-dir * (halfWidth + 1f), mysteryShipHeight, 0f);
        currentShip = Instantiate(mysteryShipPrefab, position, Quaternion.identity);
        currentShip.direction = dir;
    }

    // Called by an invader when it is destroyed.
    public void OnInvaderKilled(Invader invader)
    {
        invaders.Remove(invader);
        if (invaders.Count == 0) GameManager.Instance.WaveCleared();
    }
}
