using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The "brain" of the game.
// It keeps the score and lives, and decides when the game starts, pauses, goes to the next wave and ends.
// Other scripts talk to it through GameManager.Instance (there is only ever one GameManager).
public class GameManager : MonoBehaviour
{
    public enum GameState { Title, Playing, Respawning, WaveCleared, GameOver }

    public static GameManager Instance { get; private set; }

    [Header("References (filled in by the setup tool)")]
    public PlayerController player;
    public InvaderGrid invaderGrid;
    public ShieldSpawner shields;

    [Header("UI")]
    public Text scoreText;
    public Text hiScoreText;
    public Text livesText;
    public Text messageText;

    [Header("Rules")]
    public int startingLives = 3;
    public float respawnDelay = 1.5f;
    public float nextWaveDelay = 2f;

    public GameState State { get; private set; } = GameState.Title;
    public bool IsPaused { get; private set; }

    // Other scripts check this to know if they should move / shoot right now.
    public bool IsPlaying => State == GameState.Playing && !IsPaused;

    const string HiScoreKey = "SpaceInvaders.HiScore";
    int score;
    int hiScore;
    int lives;
    int wave;

    // Makes sure the static Instance is cleared when you press Play again in the editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        Instance = this;
        hiScore = PlayerPrefs.GetInt(HiScoreKey, 0); // PlayerPrefs saves small values between play sessions
    }

    void Start()
    {
        wave = 1;
        lives = startingLives;
        invaderGrid.SpawnWave(wave);
        shields.Build();
        player.ResetPlayer();
        State = GameState.Title;
        ShowMessage("SPACE INVADERS\n\nPress ENTER to start\n\nARROWS or A / D : move      SPACE : shoot      P : pause");
        UpdateUI();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        bool enterPressed = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;

        if ((State == GameState.Title || State == GameState.GameOver) && (enterPressed || kb.rKey.wasPressedThisFrame))
        {
            StartNewGame();
        }
        else if (kb.pKey.wasPressedThisFrame && State != GameState.Title && State != GameState.GameOver)
        {
            TogglePause();
        }
    }

    public void StartNewGame()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        IsPaused = false;
        score = 0;
        lives = startingLives;
        wave = 1;
        Bullet.ClearAll();
        invaderGrid.SpawnWave(wave);
        shields.Build();
        player.ResetPlayer();
        State = GameState.Playing;
        HideMessage();
        UpdateUI();
    }

    void TogglePause()
    {
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f; // timeScale 0 freezes everything that uses Time.deltaTime and physics
        if (IsPaused) ShowMessage("PAUSED\n\nPress P to continue");
        else HideMessage();
    }

    public void AddScore(int points)
    {
        score += points;
        if (score > hiScore)
        {
            hiScore = score;
            PlayerPrefs.SetInt(HiScoreKey, hiScore);
        }
        UpdateUI();
    }

    // Called by the player when an enemy bullet hits it.
    public void PlayerHit()
    {
        if (State != GameState.Playing) return;

        lives--;
        UpdateUI();
        Bullet.ClearAll();

        if (lives <= 0) GameOver();
        else StartCoroutine(RespawnRoutine());
    }

    // A coroutine is a function that can wait (yield) and continue later, without freezing the game.
    IEnumerator RespawnRoutine()
    {
        State = GameState.Respawning;
        yield return new WaitForSeconds(respawnDelay);
        player.Respawn();
        State = GameState.Playing;
    }

    // Called by the invader grid when the last invader is destroyed.
    public void WaveCleared()
    {
        if (State != GameState.Playing) return;
        StartCoroutine(NextWaveRoutine());
    }

    IEnumerator NextWaveRoutine()
    {
        State = GameState.WaveCleared;
        Bullet.ClearAll();
        wave++;
        UpdateUI();
        ShowMessage("WAVE " + wave);
        yield return new WaitForSeconds(nextWaveDelay);
        invaderGrid.SpawnWave(wave);
        shields.Build();
        HideMessage();
        State = GameState.Playing;
    }

    // Called by the invader grid when invaders reach the bottom.
    public void InvadersLanded()
    {
        if (State == GameState.GameOver) return;
        player.Explode();
        GameOver();
    }

    void GameOver()
    {
        StopAllCoroutines();
        State = GameState.GameOver;
        Bullet.ClearAll();
        PlayerPrefs.Save();
        ShowMessage("GAME OVER\n\nScore " + score + "\n\nPress ENTER to play again");
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = "SCORE  " + score.ToString("0000");
        if (hiScoreText) hiScoreText.text = "HI-SCORE  " + hiScore.ToString("0000");
        if (livesText) livesText.text = "WAVE  " + wave + "      LIVES  " + lives;
    }

    void ShowMessage(string text)
    {
        if (!messageText) return;
        messageText.text = text;
        messageText.enabled = true;
    }

    void HideMessage()
    {
        if (messageText) messageText.enabled = false;
    }
}
