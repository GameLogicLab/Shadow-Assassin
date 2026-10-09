using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject levelPanel;
    void Start()
    {
        levelPanel.SetActive(false);
    }
    
    public void levelPanelSelection()
    {
        mainMenuPanel.SetActive(false);
        levelPanel.SetActive(true);
    }

    public void levelSelection(int level)
    {
        SceneManager.LoadScene(""+level);       
    }

    public void Quit()
    {
        Application.Quit();
    }
}
