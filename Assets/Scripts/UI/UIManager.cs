using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startMenuPanel;
    public GameObject arUIPanel;

    [Header("Controls")]
    public Dropdown slotDropdown;

    [Header("Refs")]
    public SpawnPlacementManager spawnPlacementManager;

    void Start()
    {
        ShowStartMenu();
    }

    public void ShowStartMenu()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (arUIPanel != null) arUIPanel.SetActive(false);
        if (spawnPlacementManager != null)
            spawnPlacementManager.DisableARMode();
    }

    public void OnStartButton()
    {
        // make the start menu visually transparent but keep it in the hierarchy
        if (startMenuPanel != null)
        {
            var cg = startMenuPanel.GetComponent<UnityEngine.CanvasGroup>();
            if (cg == null) cg = startMenuPanel.AddComponent<UnityEngine.CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        if (arUIPanel != null) arUIPanel.SetActive(true);

        if (spawnPlacementManager != null)
            spawnPlacementManager.EnterARMode();
    }

    public void OnSlotChanged(int index)
    {
        if (spawnPlacementManager != null)
            spawnPlacementManager.SelectSlot(index);
    }
}

