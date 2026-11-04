using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject levelPanel;
    public GameObject settingsPanel;  
    public Toggle musicToggle;
    public AudioSource audioSource;
    public AudioClip UIAudio;

    void Start()
    {
        levelPanel.SetActive(false);
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        // Load saved music preference and connect the toggle
        if (musicToggle != null && AudioManager.Instance != null)
        {
            musicToggle.isOn = AudioManager.Instance.IsMusicEnabled();

            musicToggle.onValueChanged.AddListener((isOn) =>
            {
                AudioManager.Instance.SetMusicEnabled(isOn);
            });
        }
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
        public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
