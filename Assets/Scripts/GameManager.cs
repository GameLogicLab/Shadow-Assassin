using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private int enemyCount = 0;
    private int maxEnemy = 0;

    [Header("Notification Panel")]
    public GameObject image;
    private TMP_Text text;

    [Header("Pause Panel")]
    public GameObject pausePanel;
    public bool IsPaused { get; private set; }

    [Header("Game Flow Panels (Optional)")]
    public GameObject victoryPanel;
    public GameObject gameOverPanel;

    [Header("Audio (Optional)")]
    public AudioClip victoryAudio;
    public AudioClip gameOverAudio;

    public int RemainingEnemies => enemyCount;
    public int TotalEnemies => maxEnemy;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Set target frame rate for smooth 60 FPS (especially on mobile)
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    void Start()
    {
        // Count enemies efficiently without iterating all GameObjects in the scene
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);
        enemyCount = enemies.Length;
        maxEnemy = enemyCount;

        Debug.Log("Total Enemies: " + enemyCount);

        if (image != null)
        {
            text = image.GetComponentInChildren<TMP_Text>();
        }

        ShowNotification(0);

        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void ShowNotification(int decrement)
    {
        enemyCount -= decrement;
        if (enemyCount < 0) enemyCount = 0;

        if (text != null)
        {
            text.text = "Enemies " + enemyCount + "/" + maxEnemy;
        }

        if (image != null)
        {
            StopCoroutine(nameof(NotificationActivateRoutine));
            StartCoroutine(NotificationActivateRoutine());
        }

        // Check Victory Condition
        if (enemyCount <= 0 && maxEnemy > 0)
        {
            StartCoroutine(VictoryRoutine());
        }
    }

    private IEnumerator NotificationActivateRoutine()
    {
        image.SetActive(true);
        yield return new WaitForSeconds(3f);
        image.SetActive(false);
    }

    private IEnumerator VictoryRoutine()
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("Level Cleared! All enemies eliminated.");

        if (victoryAudio != null)
        {
            AudioSource.PlayClipAtPoint(victoryAudio, Camera.main ? Camera.main.transform.position : transform.position);
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
        else if (text != null && image != null)
        {
            // Fallback victory display if no special panel is assigned
            image.SetActive(true);
            text.text = "MISSION COMPLETE!";
            yield return new WaitForSeconds(2.5f);
            NextLevel();
        }
    }

    public void OnPlayerDied()
    {
        Debug.Log("GameManager: Player has fallen.");

        if (gameOverAudio != null)
        {
            AudioSource.PlayClipAtPoint(gameOverAudio, Camera.main ? Camera.main.transform.position : transform.position);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else if (text != null && image != null)
        {
            // Fallback Game Over display
            image.SetActive(true);
            text.text = "MISSION FAILED!";
            StartCoroutine(AutoRestartRoutine(3f));
        }
        else
        {
            StartCoroutine(AutoRestartRoutine(2f));
        }
    }

    private IEnumerator AutoRestartRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestartLevel();
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene.StartsWith("Level"))
        {
            string numberPart = currentScene.Substring("Level".Length);
            if (int.TryParse(numberPart, out int currentLevelIndex))
            {
                int nextLevelIndex = currentLevelIndex + 1;
                string nextSceneName = "Level" + nextLevelIndex;

                if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                {
                    SceneManager.LoadScene(nextSceneName);
                    return;
                }
            }
        }

        // Fallback or end of levels: return to Main Menu
        SceneManager.LoadScene("MainMenu");
    }

    public void PauseGame()
    {
        IsPaused = true;
        if (pausePanel != null)
            pausePanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
