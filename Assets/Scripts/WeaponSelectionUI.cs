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
        selectedWeapon = PlayerPrefs.GetInt("SelectedWeapon", 0);
        UpdateHighlight();
    }

    public void SelectKnife()
    {
        selectedWeapon = 0;
        PlayerPrefs.SetInt("SelectedWeapon", selectedWeapon);
        PlayerPrefs.Save();
        UpdateHighlight();
        ApplyToPlayer();
    }

    public void SelectKunai()
    {
        selectedWeapon = 1;
        PlayerPrefs.SetInt("SelectedWeapon", selectedWeapon);
        PlayerPrefs.Save();
        UpdateHighlight();
        ApplyToPlayer();
    }

    private void ApplyToPlayer()
    {
        PlayerAttack playerAttack = FindAnyObjectByType<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.SetWeapon((PlayerAttack.WeaponType)selectedWeapon);
        }
    }

    private void UpdateHighlight()
    {
        if (knifeSelectedHighlight != null)
            knifeSelectedHighlight.SetActive(selectedWeapon == 0);

        if (kunaiSelectedHighlight != null)
            kunaiSelectedHighlight.SetActive(selectedWeapon == 1);
    }

    public void OpenWeaponSelection()
    {
        if (weaponSelectionPanel != null)
            weaponSelectionPanel.SetActive(true);
    }

    public void CloseWeaponSelection()
    {
        if (weaponSelectionPanel != null)
            weaponSelectionPanel.SetActive(false);
    }
}
