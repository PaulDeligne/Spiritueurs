using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startMenuPanel;
    public GameObject calibrationPanel; // NEW: Panel for AR calibration
    public GameObject arUIPanel;

    [Header("Calibration UI")]
    public TMPro.TextMeshProUGUI calibrationText; // NEW: Text to show calibration status

    [Header("Debug")]
    public TMPro.TextMeshProUGUI debugText; // Optional: debug info on screen

    [Header("Controls")]
    public Dropdown slotDropdown;
    [Header("Door List")]
    public RectTransform doorListContainer; // UI container for door buttons
    public Button doorButtonPrefab; // prefab for an item in the list
    public Button launchButton; // enabled when all doors placed

    [Header("Refs")]
    public SpawnPlacementManager spawnPlacementManager;

    private List<Button> doorButtons = new();
    private int doorCount = 0;
    private bool freeMode = true;
    // counts per type: 0 = Door, 1 = Window, 2 = Hatch
    private int[] remainingCounts = new int[3];
    
    // Calibration state
    private bool isCalibrating = false;

    void Start()
    {
        ShowStartMenu();
    }
    
    private float calibrationTimer = 0f;
    
    void Update()
    {
        // During calibration, check if enough surfaces are detected
        if (isCalibrating && spawnPlacementManager != null)
        {
            calibrationTimer += Time.deltaTime;
            int planeCount = spawnPlacementManager.GetDetectedPlaneCount();
            
            if (calibrationText != null)
            {
                string statusText;
                if (planeCount == 0)
                {
                    statusText = "Scannez lentement votre environnement...\n\n";
                    statusText += "Bougez le téléphone pour détecter les surfaces.\n\n";
                    statusText += $"Temps: {calibrationTimer:F1}s\n";
                    
                    // After 5 seconds, allow skipping
                    if (calibrationTimer > 5f)
                    {
                        statusText += "\n<color=yellow>Appuyez pour continuer sans détection</color>";
                    }
                }
                else
                {
                    statusText = $"<color=green>Surfaces détectées : {planeCount}</color>\n\n";
                    statusText += "Appuyez n'importe où pour continuer.";
                }
                
                calibrationText.text = statusText;
            }
            
            // Allow skip after 5 seconds OR if planes detected
            bool canContinue = (planeCount >= 1) || (calibrationTimer > 5f);
            
            if (canContinue)
            {
                // Check for tap to continue
                if (UnityEngine.InputSystem.Touchscreen.current != null && 
                    UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    OnCalibrationComplete();
                }
                else if (UnityEngine.InputSystem.Mouse.current != null && 
                         UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    OnCalibrationComplete();
                }
            }
        }
        
        // Update debug text if available
        UpdateDebugText();
    }
    
    private void UpdateDebugText()
    {
        if (debugText == null) return;
        
        string debug = "";
        debug += $"AR State: {ARSession.state}\n";
        
        if (Camera.main != null)
        {
            debug += $"Cam Pos: {Camera.main.transform.position:F2}\n";
        }
        
        if (spawnPlacementManager != null)
        {
            debug += $"Planes: {spawnPlacementManager.GetDetectedPlaneCount()}\n";
            debug += $"Selected: {spawnPlacementManager.selectedEntryType}\n";
            debug += $"Entries placed: {spawnPlacementManager.GetPlacedEntriesCount()}\n";
        }
        
        debugText.text = debug;
    }

    public void ShowStartMenu()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (calibrationPanel != null) calibrationPanel.SetActive(false);
        if (arUIPanel != null) arUIPanel.SetActive(false);
        if (spawnPlacementManager != null)
            spawnPlacementManager.DisableARMode();
        // hide launch button while in the start menu
        if (launchButton != null) launchButton.gameObject.SetActive(false);
        isCalibrating = false;
        calibrationTimer = 0f;
    }

    public void OnStartButton()
    {
        // Hide start menu
        if (startMenuPanel != null) startMenuPanel.SetActive(false);

        // Show calibration panel
        if (calibrationPanel != null)
        {
            calibrationPanel.SetActive(true);
            if (calibrationText != null)
            {
                calibrationText.text = "Initialisation AR...\n\nPatientez...";
            }
        }
        
        // Hide placement UI for now
        if (arUIPanel != null) arUIPanel.SetActive(false);

        // Ensure we have a reference to SpawnPlacementManager
        if (spawnPlacementManager == null)
        {
            spawnPlacementManager = FindFirstObjectByType<SpawnPlacementManager>();
        }

        // Start AR mode for calibration
        if (spawnPlacementManager != null)
        {
            spawnPlacementManager.EnterARMode();
            spawnPlacementManager.StartCalibration();
            
            // Log AR status for debugging
            Debug.Log($"UIManager: AR Status:\n{spawnPlacementManager.GetARStatus()}");
        }
        
        isCalibrating = true;
        calibrationTimer = 0f;
        Debug.Log("UIManager: Started calibration phase");
    }
    
    public void OnCalibrationComplete()
    {
        isCalibrating = false;
        
        // Hide calibration panel
        if (calibrationPanel != null) calibrationPanel.SetActive(false);
        
        // Show AR UI with placement buttons
        ShowPlacementUI();
        
        Debug.Log("UIManager: Calibration complete, showing placement UI");
    }
    
    private void ShowPlacementUI()
    {
        if (arUIPanel != null)
        {
            arUIPanel.SetActive(true);
            var cg2 = arUIPanel.GetComponent<UnityEngine.CanvasGroup>();
            if (cg2 != null)
            {
                cg2.alpha = 1f;
                cg2.interactable = true;
                cg2.blocksRaycasts = true;
            }
        }

        // Show launch button
        if (launchButton != null) launchButton.gameObject.SetActive(true);

        // Ensure door list container is visible
        if (doorListContainer != null)
        {
            if (arUIPanel != null)
                doorListContainer.SetParent(arUIPanel.transform, false);
            doorListContainer.gameObject.SetActive(true);
        }

        // Ensure launch button is parented under AR UI
        if (launchButton != null && arUIPanel != null)
        {
            launchButton.transform.SetParent(arUIPanel.transform, false);
            launchButton.gameObject.SetActive(true);
        }

        // Populate the placement buttons
        PopulatePlacementList(true);
        
        // End calibration mode in SpawnPlacementManager
        if (spawnPlacementManager != null)
        {
            spawnPlacementManager.EndCalibration();
        }
    }

    public void OnSlotChanged(int index)
    {
        if (spawnPlacementManager != null)
            spawnPlacementManager.SelectSlot(index);
    }

    // Populate placement list with the three types. In free mode counts are shown as infinity.
    public void PopulatePlacementList(bool isFreeMode, int doorCount = 0, int windowCount = 0, int hatchCount = 0)
    {
        freeMode = isFreeMode;
        remainingCounts[0] = doorCount;
        remainingCounts[1] = windowCount;
        remainingCounts[2] = hatchCount;

        // defensive checks: ensure we have a container and a button prefab
        if (doorListContainer == null)
        {
            var found = GameObject.Find("DoorListContainer");
            if (found != null)
                doorListContainer = found.GetComponent<RectTransform>();
            else if (arUIPanel != null)
            {
                // create a container under the AR UI
                var go = new GameObject("DoorListContainer", typeof(RectTransform));
                go.transform.SetParent(arUIPanel.transform, false);
                doorListContainer = go.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogWarning("UIManager: doorListContainer is null and no AR UI to parent to. Placement list will not be visible.");
            }
        }

        Button runtimeFallbackPrefab = null;
        if (doorButtonPrefab == null)
        {
            Debug.LogWarning("UIManager: doorButtonPrefab is not assigned. Creating a runtime fallback button prefab.");
            // create a simple button prefab at runtime
            var btnGO = new GameObject("RuntimePlaceButton");
            var img = btnGO.AddComponent<UnityEngine.UI.Image>();
            var btn = btnGO.AddComponent<UnityEngine.UI.Button>();
            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new UnityEngine.Vector2(160f, 48f);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var txt = textGO.AddComponent<UnityEngine.UI.Text>();
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            txt.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

            runtimeFallbackPrefab = btn;
        }
        // clear existing
        foreach (var b in doorButtons) if (b != null) Destroy(b.gameObject);
        doorButtons.Clear();

        // ensure the container is anchored at the bottom and laid out horizontally
        if (doorListContainer != null)
        {
            var rt = doorListContainer as RectTransform;
            if (rt != null)
            {
                // anchor to bottom and stretch horizontally so layout centers correctly
                rt.anchorMin = new UnityEngine.Vector2(0f, 0f);
                rt.anchorMax = new UnityEngine.Vector2(1f, 0f);
                rt.pivot = new UnityEngine.Vector2(0.5f, 0f);
                rt.anchoredPosition = new UnityEngine.Vector2(0f, 10f);
                rt.sizeDelta = new UnityEngine.Vector2(0f, 60f);
            }

            var h = doorListContainer.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            var v = doorListContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            var g = doorListContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();

            if (h != null)
            {
                h.childAlignment = TextAnchor.MiddleCenter;
                h.spacing = 8f;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;
            }
            else if (v != null)
            {
                // a VerticalLayoutGroup exists — configure it safely instead of adding a HorizontalLayoutGroup
                v.childAlignment = TextAnchor.MiddleCenter;
                v.spacing = 8f;
                v.childForceExpandWidth = false;
                v.childForceExpandHeight = false;
            }
            else if (g != null)
            {
                g.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedRowCount;
            }
            else
            {
                // no layout group present: add a HorizontalLayoutGroup
                h = doorListContainer.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleCenter;
                h.spacing = 8f;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;
            }
        }

        string[] labels = new[] { "Door", "Window", "Hatch" };
        Button prefabToUse = (doorButtonPrefab != null) ? doorButtonPrefab : runtimeFallbackPrefab;
        for (int i = 0; i < 3; i++)
        {
            var b = Instantiate(prefabToUse, (doorListContainer != null) ? doorListContainer : null, false);
            int idx = i;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => OnPlacementButtonClicked(idx));
            string countText = freeMode ? "∞" : remainingCounts[i].ToString();
            // set a clear name for the button and update its label text
            b.name = $"PlaceButton_{labels[i]}";
            // ensure RectTransform and layout behave
            var brt = b.GetComponent<RectTransform>();
            if (brt != null)
            {
                brt.localScale = Vector3.one;
                brt.sizeDelta = new UnityEngine.Vector2(160f, 48f);
            }
            var le = b.GetComponent<UnityEngine.UI.LayoutElement>();
            if (le == null) le = b.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = 160f;
            le.preferredHeight = 48f;
            string labelText = $"{labels[i]} — {countText}";
            var textComp = b.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                textComp.text = labelText;
            }
            else
            {
                // fallback: try to find any child component that exposes a writable 'text' property (TextMeshPro support)
                bool set = false;
                var comps = b.GetComponentsInChildren<UnityEngine.Component>(true);
                foreach (var c in comps)
                {
                    var prop = c.GetType().GetProperty("text");
                    if (prop != null && prop.PropertyType == typeof(string) && prop.CanWrite)
                    {
                        prop.SetValue(c, labelText, null);
                        set = true;
                        break;
                    }
                }

                if (!set)
                    Debug.LogWarning($"UIManager: button prefab for {labels[i]} has no Text/TextMeshProUGUI child.");
            }
            b.gameObject.SetActive(true);
            doorButtons.Add(b);
        }

        UpdateLaunchButton();
    }

    void OnPlacementButtonClicked(int index)
    {
        // visually indicate selection
        for (int i = 0; i < doorButtons.Count; i++)
        {
            var cg = doorButtons[i].GetComponent<UnityEngine.UI.Graphic>();
            if (cg != null) cg.color = (i == index) ? Color.cyan : Color.white;
        }

        if (spawnPlacementManager != null)
            spawnPlacementManager.SelectTypeToPlace(index, this);
    }

    // Called by SpawnPlacementManager after an item is placed
    public void OnPlaced(int typeIndex)
    {
        if (!freeMode && typeIndex >= 0 && typeIndex < remainingCounts.Length)
        {
            remainingCounts[typeIndex] = Mathf.Max(0, remainingCounts[typeIndex] - 1);
            var textComp = doorButtons[typeIndex].GetComponentInChildren<Text>();
            if (textComp != null) textComp.text = $"{(typeIndex==0?"Door":typeIndex==1?"Window":"Hatch")} — {remainingCounts[typeIndex]}";

            if (remainingCounts[typeIndex] == 0)
            {
                // disable button when done
                doorButtons[typeIndex].interactable = false;
                var cg = doorButtons[typeIndex].GetComponent<UnityEngine.UI.Graphic>();
                if (cg != null) cg.color = Color.gray;
            }
        }

        UpdateLaunchButton();
    }

    void UpdateLaunchButton()
    {
        if (launchButton == null) return;
        if (freeMode)
        {
            // in free mode, allow launch anytime (if there is at least one placement button)
            launchButton.interactable = doorButtons.Count > 0;
            return;
        }

        // in level mode, only enable when all counts are zero
        bool allPlaced = true;
        for (int i = 0; i < remainingCounts.Length; i++) if (remainingCounts[i] > 0) { allPlaced = false; break; }
        launchButton.interactable = allPlaced && doorButtons.Count > 0;
    }

    // Hook this to the Launch button in the inspector
    public void OnLaunchButton()
    {
        if (spawnPlacementManager != null)
            spawnPlacementManager.StartGame();
    }

    // backward-compatible call name used previously
    public void OnDoorPlaced(int index)
    {
        OnPlaced(index);
    }
}

