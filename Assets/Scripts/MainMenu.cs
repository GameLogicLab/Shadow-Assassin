using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject levelPanel;
    public AudioSource audioSource;
    public AudioClip UIAudio;

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
        SceneManager.LoadScene("Level"+level); 
        if (audioSource != null && UIAudio != null)
        {
            audioSource.PlayOneShot(UIAudio);
        }
    }

    public void Quit()
    {
        Application.Quit();
    }
}
