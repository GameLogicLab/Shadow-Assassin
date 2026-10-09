using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    int enemyCount = 0;
    int maxEnemy;
    int enemyLayer;

    [Header("Notification Panel")]
    public GameObject image;
    private TMP_Text text;

    [Header("Pause")]
    public GameObject pausePanel;
    private bool isPaused = false;


    void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");

        GameObject[] objects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Exclude
        );

        foreach (GameObject obj in objects)
        {
            if (obj.transform == obj.transform.root &&
                obj.layer == enemyLayer)
            {
                enemyCount++;
            }
        }

        maxEnemy = enemyCount;

        Debug.Log("Total Enemies: " + enemyCount);

        text = image.GetComponentInChildren<TMP_Text>();

        ShowNotification(0);

        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    public void ShowNotification(int decrement)
    {
        enemyCount -= decrement;
        text.text = "Enemies " + enemyCount + "/" + maxEnemy;
        StartCoroutine(NotificationActivateRoutine());
    }

    IEnumerator NotificationActivateRoutine()
    {
        image.SetActive(true);

        yield return new WaitForSeconds(3f); 

        image.SetActive(false);
    }

    public void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}