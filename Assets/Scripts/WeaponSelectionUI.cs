
using UnityEngine;

public class WeaponSelectionUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject weaponSelectionPanel;

    [Header("Selection Highlights")]
    public GameObject knifeSelectedHighlight;
    public GameObject kunaiSelectedHighlight;

    // 0 = Knife, 1 = Kunai
    private int selectedWeapon;

    private void Start()
    {
        // Load the previously selected weapon.
        selectedWeapon = PlayerPrefs.GetInt("SelectedWeapon", 0);

        UpdateHighlight();
    }

    public void SelectKnife()
    {
        selectedWeapon = 0;

        PlayerPrefs.SetInt("SelectedWeapon", selectedWeapon);
        PlayerPrefs.Save();

        UpdateHighlight();
    }

    public void SelectKunai()
    {
        selectedWeapon = 1;

        PlayerPrefs.SetInt("SelectedWeapon", selectedWeapon);
        PlayerPrefs.Save();

        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        knifeSelectedHighlight.SetActive(selectedWeapon == 0);
        kunaiSelectedHighlight.SetActive(selectedWeapon == 1);
    }

    public void OpenWeaponSelection()
    {
        weaponSelectionPanel.SetActive(true);
    }

    public void CloseWeaponSelection()
    {
        weaponSelectionPanel.SetActive(false);
    }
}
