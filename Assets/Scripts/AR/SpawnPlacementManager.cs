using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

/// <summary>
/// Simple AR placement manager for placing entry points (doors, windows, hatches) in AR.
/// </summary>
public class SpawnPlacementManager : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private ARCameraBackground arCameraBackground;

    [Header("Entry Prefab")]
    [Tooltip("Drag your entry prefab here. If empty, a simple cube will be created.")]
    [SerializeField] private GameObject entryPrefab;

    [Header("State")]
    public bool isPlacementMode = true;
    public int selectedEntryType = -1; // 0=Door, 1=Window, 2=Hatch
    
    // Calibration state
    private bool isCalibrating = false;

    // Reference to UI manager for callbacks
    private UIManager uiManager;
    
    // List of all placed entries
    private List<GameObject> placedEntries = new List<GameObject>();
    
    // AR raycast hits buffer
    private static List<ARRaycastHit> arHits = new List<ARRaycastHit>();
    
    // Reference to AR Session for tracking state
    private ARSession arSession;
    private XROrigin xrOrigin; // NEW: Use XROrigin instead of deprecated ARSessionOrigin

    void Start()
    {
        // Try to find AR components if not assigned
        if (arRaycastManager == null)
            arRaycastManager = FindObjectOfType<ARRaycastManager>();
        if (arPlaneManager == null)
            arPlaneManager = FindObjectOfType<ARPlaneManager>();
        if (arCameraBackground == null && Camera.main != null)
            arCameraBackground = Camera.main.GetComponent<ARCameraBackground>();
        
        // Find AR Session and XR Origin
        arSession = FindObjectOfType<ARSession>();
        xrOrigin = FindObjectOfType<XROrigin>();
        
        Debug.Log($"[SpawnPlacementManager] Start - arRaycastManager: {arRaycastManager != null}, arPlaneManager: {arPlaneManager != null}, arSession: {arSession != null}, xrOrigin: {xrOrigin != null}");
        
        // Verify XR Origin setup
        if (xrOrigin != null)
        {
            Debug.Log($"[SpawnPlacementManager] XROrigin found: {xrOrigin.gameObject.name}");
            Debug.Log($"[SpawnPlacementManager] XROrigin Camera: {xrOrigin.Camera?.name ?? "NULL"}");
        }
        else
        {
            Debug.LogError("[SpawnPlacementManager] XROrigin not found! Make sure you have an XR Origin in your scene. AR tracking will not work correctly.");
        }
    }

    void Update()
    {
        // During calibration, don't process placement input
        if (isCalibrating) return;
        
        if (!isPlacementMode) return;
        if (selectedEntryType < 0) return; // No entry type selected

        Vector2 screenPos = Vector2.zero;
        bool inputDetected = false;

        // New Input System: Touch
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                screenPos = touch.position.ReadValue();
                inputDetected = true;
                Debug.Log($"[SpawnPlacementManager] Touch detected at: {screenPos}");
            }
        }
        // New Input System: Mouse fallback for editor
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            inputDetected = true;
            Debug.Log($"[SpawnPlacementManager] Mouse click at: {screenPos}");
        }

        if (!inputDetected) return;

        // Don't place if touching UI
        if (IsPointerOverUI())
        {
            Debug.Log("[SpawnPlacementManager] Pointer is over UI, ignoring");
            return;
        }

        TryPlaceEntry(screenPos);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void TryPlaceEntry(Vector2 screenPosition)
    {
        Debug.Log($"TryPlaceEntry at screen position: {screenPosition}");

        bool placed = false;

        // Try multiple AR raycast methods (from most to least precise)
        if (arRaycastManager != null)
        {
            // 1. First try: exact polygon hit
            if (arRaycastManager.Raycast(screenPosition, arHits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = arHits[0].pose;
                Debug.Log($"AR Raycast (PlaneWithinPolygon) hit at: {hitPose.position}");
                PlaceEntryAtPosition(hitPose.position, hitPose.rotation);
                placed = true;
            }
            // 2. Second try: any plane (including estimated bounds)
            else if (arRaycastManager.Raycast(screenPosition, arHits, TrackableType.PlaneEstimated))
            {
                Pose hitPose = arHits[0].pose;
                Debug.Log($"AR Raycast (PlaneEstimated) hit at: {hitPose.position}");
                PlaceEntryAtPosition(hitPose.position, hitPose.rotation);
                placed = true;
            }
            // 3. Third try: any plane at all
            else if (arRaycastManager.Raycast(screenPosition, arHits, TrackableType.Planes))
            {
                Pose hitPose = arHits[0].pose;
                Debug.Log($"AR Raycast (Planes) hit at: {hitPose.position}");
                PlaceEntryAtPosition(hitPose.position, hitPose.rotation);
                placed = true;
            }
            // 4. Fourth try: feature points (works without plane detection)
            else if (arRaycastManager.Raycast(screenPosition, arHits, TrackableType.FeaturePoint))
            {
                Pose hitPose = arHits[0].pose;
                Debug.Log($"AR Raycast (FeaturePoint) hit at: {hitPose.position}");
                PlaceEntryAtPosition(hitPose.position, Quaternion.identity);
                placed = true;
            }
            // 5. Last resort: use depth/all trackables
            else if (arRaycastManager.Raycast(screenPosition, arHits, TrackableType.All))
            {
                Pose hitPose = arHits[0].pose;
                Debug.Log($"AR Raycast (All) hit at: {hitPose.position}");
                PlaceEntryAtPosition(hitPose.position, hitPose.rotation);
                placed = true;
            }
        }

        if (!placed)
        {
            // Fallback: place in front of camera
            Debug.Log("All AR Raycasts failed, using camera fallback");
            
            Camera cam = Camera.main ?? FindObjectOfType<Camera>();
            if (cam != null)
            {
                // Place 1.5 meters in front of camera, on an imaginary floor plane
                Vector3 forward = cam.transform.forward;
                forward.y = 0; // project to horizontal
                if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
                forward.Normalize();
                
                Vector3 position = cam.transform.position + forward * 1.5f;
                position.y = cam.transform.position.y - 1f; // Roughly floor level
                
                Quaternion rotation = Quaternion.LookRotation(forward);
                Debug.Log($"Fallback placement at: {position}");
                PlaceEntryAtPosition(position, rotation);
            }
            else
            {
                Debug.LogError("No camera found! Cannot place entry.");
            }
        }
    }

    private void PlaceEntryAtPosition(Vector3 worldPosition, Quaternion rotation)
    {
        Debug.Log($"PlaceEntryAtPosition called - worldPosition: {worldPosition}, rotation: {rotation.eulerAngles}");
        
        // Check AR tracking state
        var trackingState = ARSession.state;
        Debug.Log($"AR Session state: {trackingState}");
        
        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        if (cam != null)
        {
            Debug.Log($"Camera world position: {cam.transform.position}");
            Debug.Log($"Camera local position: {cam.transform.localPosition}");
            
            float distanceFromCamera = Vector3.Distance(worldPosition, cam.transform.position);
            Debug.Log($"Distance from camera: {distanceFromCamera}m");
            
            // If position is too close to camera, something is wrong
            if (distanceFromCamera < 0.3f)
            {
                Debug.LogWarning("Position too close to camera! Adjusting...");
                worldPosition = cam.transform.position + cam.transform.forward * 1.5f;
                worldPosition.y -= 0.5f;
            }
        }
        
        GameObject entry;

        // Create a simple cube
        entry = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        // CRITICAL: Set position in WORLD space, not local
        entry.transform.SetParent(null); // Ensure no parent first!
        entry.transform.position = worldPosition;
        entry.transform.rotation = rotation;
        entry.transform.localScale = new Vector3(0.3f, 0.5f, 0.05f);
        
        // Double-check it's in world space
        Debug.Log($"Entry parent: {(entry.transform.parent != null ? entry.transform.parent.name : "NONE (world)")}");
        Debug.Log($"Entry final world position: {entry.transform.position}");
        
        // Remove collider
        var collider = entry.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        
        // Set a bright color to be visible
        var renderer = entry.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(renderer.material);
            renderer.material.color = Color.green;
        }

        // Name it based on type
        string[] typeNames = { "Door", "Window", "Hatch" };
        string typeName = (selectedEntryType >= 0 && selectedEntryType < typeNames.Length) 
            ? typeNames[selectedEntryType] 
            : "Entry";
        entry.name = $"{typeName}_{placedEntries.Count}";

        // Add to list
        placedEntries.Add(entry);

        // Notify UI
        if (uiManager != null)
        {
            uiManager.OnPlaced(selectedEntryType);
        }

        // Deselect after placing (user must select again for next placement)
        selectedEntryType = -1;
        
        Debug.Log($"Entry placed successfully: {entry.name} at world pos {entry.transform.position}");
    }

    /// <summary>
    /// Called by UIManager when user selects an entry type to place
    /// </summary>
    public void SelectTypeToPlace(int typeIndex, UIManager ui)
    {
        selectedEntryType = typeIndex;
        uiManager = ui;
        isPlacementMode = true;
        
        // Enable AR plane detection
        if (arPlaneManager != null) arPlaneManager.enabled = true;
        if (arCameraBackground != null) arCameraBackground.enabled = true;
        
        Debug.Log($"Selected entry type: {typeIndex}");
    }

    /// <summary>
    /// Called when entering AR mode from start menu
    /// </summary>
    public void EnterARMode()
    {
        isPlacementMode = true;
        if (arPlaneManager != null) arPlaneManager.enabled = true;
        if (arCameraBackground != null) arCameraBackground.enabled = true;
    }

    /// <summary>
    /// Called to disable AR mode (back to menu)
    /// </summary>
    public void DisableARMode()
    {
        isPlacementMode = false;
        isCalibrating = false;
        if (arPlaneManager != null) arPlaneManager.enabled = false;
        if (arCameraBackground != null) arCameraBackground.enabled = false;
    }

    /// <summary>
    /// Called when player starts the game (after placing all entries)
    /// </summary>
    public void StartGame()
    {
        isPlacementMode = false;
        if (arPlaneManager != null) arPlaneManager.enabled = false;
        
        Debug.Log($"Game started with {placedEntries.Count} entries placed");
    }
    
    #region Calibration
    
    /// <summary>
    /// Start the calibration phase - AR detects surfaces before placement begins
    /// </summary>
    public void StartCalibration()
    {
        isCalibrating = true;
        isPlacementMode = false;
        selectedEntryType = -1;
        
        // Enable AR plane detection
        if (arPlaneManager != null) 
        {
            arPlaneManager.enabled = true;
            Debug.Log($"[SpawnPlacementManager] ARPlaneManager enabled, detection mode: {arPlaneManager.requestedDetectionMode}");
        }
        else
        {
            Debug.LogError("[SpawnPlacementManager] ARPlaneManager is NULL! AR detection will not work.");
        }
        
        if (arCameraBackground != null) arCameraBackground.enabled = true;
        
        Debug.Log("[SpawnPlacementManager] Calibration started - scanning for surfaces");
    }
    
    /// <summary>
    /// End calibration and allow placement
    /// </summary>
    public void EndCalibration()
    {
        isCalibrating = false;
        isPlacementMode = true;
        
        Debug.Log($"[SpawnPlacementManager] Calibration ended - {GetDetectedPlaneCount()} planes detected");
    }
    
    /// <summary>
    /// Get the number of AR planes currently detected
    /// </summary>
    public int GetDetectedPlaneCount()
    {
        if (arPlaneManager == null) 
        {
            Debug.LogWarning("[SpawnPlacementManager] GetDetectedPlaneCount: arPlaneManager is null");
            return 0;
        }
        
        int count = 0;
        foreach (var plane in arPlaneManager.trackables)
        {
            if (plane.gameObject.activeSelf)
                count++;
        }
        return count;
    }
    
    /// <summary>
    /// Get detailed AR status for debugging
    /// </summary>
    public string GetARStatus()
    {
        string status = "";
        status += $"ARRaycastManager: {(arRaycastManager != null ? "OK" : "NULL")}\n";
        status += $"ARPlaneManager: {(arPlaneManager != null ? "OK" : "NULL")}\n";
        if (arPlaneManager != null)
        {
            status += $"PlaneManager enabled: {arPlaneManager.enabled}\n";
            status += $"Detection mode: {arPlaneManager.requestedDetectionMode}\n";
            status += $"Planes detected: {GetDetectedPlaneCount()}\n";
        }
        status += $"ARCameraBackground: {(arCameraBackground != null ? "OK" : "NULL")}\n";
        return status;
    }
    
    #endregion

    // Keep old method names for compatibility
    public void SelectSlot(int index) => selectedEntryType = index;
    public void SelectDoorToPlace(int index, UIManager ui) => SelectTypeToPlace(index, ui);
    
    /// <summary>
    /// Get the number of entries placed so far
    /// </summary>
    public int GetPlacedEntriesCount() => placedEntries.Count;
}
