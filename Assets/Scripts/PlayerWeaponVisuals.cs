using UnityEngine;

public class PlayerWeaponVisuals : MonoBehaviour
{
    [Header("Weapon Models")]
    [SerializeField] private GameObject knifeModel;
    [SerializeField] private GameObject kunaiModel;

    private void Start()
    {
        // 0 = Knife, 1 = Kunai
        int selectedWeapon = PlayerPrefs.GetInt("SelectedWeapon", 0);

        bool useKnife = selectedWeapon == 0;

        if (knifeModel != null)
            knifeModel.SetActive(useKnife);

        if (kunaiModel != null)
            kunaiModel.SetActive(!useKnife);
    }
}
