using TMPro;
using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    int enemyCount = 0;
    int maxEnemy;
    int enemyLayer;

    [Header("Notification Panel")]
    public GameObject image;
    private TMP_Text text;


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
}