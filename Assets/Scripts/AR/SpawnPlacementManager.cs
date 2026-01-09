using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SpawnPlacementManager : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;

    [Header("Placement")]
    public bool isPlacementMode = true;
    public int selectedEntryType = -1;
    
    [Header("Spawn Settings")]
    [SerializeField] private float minPlaneSize = 0.5f; // Reduced from 1.5f
    [SerializeField] private float entriesToSpawn = 3f; // Auto-spawn 3 entries after calibration
    [SerializeField] private float autoSpawnDelay = 1f;
    [SerializeField] private Transform placementsRoot; // Root to keep placed entries alive after validation

    private static readonly List<ARRaycastHit> hits = new();
    private readonly List<GameObject> placedEntries = new();
    private bool isCalibrating = false;
    private UIManager uiManager;
    private float timeSinceCalibrationEnd = 0f;
    private bool autoSpawnCompleted = false;

    private void Awake()
    {
        if (placementsRoot == null)
            placementsRoot = transform; // Default to this transform (e.g., ARSessionOrigin)
    }

    void Update()
    {
        if (isCalibrating) return; // Don't process placement during calibration
        
        // Auto-spawn entries after calibration
        if (!autoSpawnCompleted && isPlacementMode)
        {
            timeSinceCalibrationEnd += Time.deltaTime;
            if (timeSinceCalibrationEnd >= autoSpawnDelay)
            {
                AutoSpawnEntries();
                autoSpawnCompleted = true;
            }
        }
        
        if (!isPlacementMode || selectedEntryType < 0) return;
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;
        if (!touch.press.isPressed ||
            touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Began)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 pos = touch.position.ReadValue();
        TryPlace(pos);
    }
    
    private void AutoSpawnEntries()
    {
        int spawned = 0;
        int maxAttempts = 20;
        int attempts = 0;
        
        Debug.Log("SpawnPlacementManager: Starting auto-spawn of entries");
        
        while (spawned < entriesToSpawn && attempts < maxAttempts)
        {
            attempts++;
            
            // Try to raycast at a random screen position
            Vector2 screenPos = new Vector2(
                Random.Range(100f, Screen.width - 100f),
                Random.Range(100f, Screen.height - 100f)
            );
            
            if (!raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
                continue;

            ARRaycastHit hit = hits[0];
            ARPlane plane = planeManager.GetPlane(hit.trackableId);

            if (!IsPlaneUsable(plane)) continue;

            Vector3 position = GetSafePositionOnPlane(plane);
            if (!IsPositionPlayable(position)) continue;

            PlaceEntry(position);
            spawned++;
            Debug.Log($"SpawnPlacementManager: Auto-spawned entry {spawned}/{entriesToSpawn}");
        }
        
        if (spawned < entriesToSpawn)
        {
            Debug.LogWarning($"SpawnPlacementManager: Only spawned {spawned}/{entriesToSpawn} entries after {attempts} attempts");
        }
        else
        {
            Debug.Log($"SpawnPlacementManager: Successfully auto-spawned {spawned} entries");
        }
    }

    void TryPlace(Vector2 screenPos)
    {
        if (!raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            return;

        ARRaycastHit hit = hits[0];
        ARPlane plane = planeManager.GetPlane(hit.trackableId);

        if (!IsPlaneUsable(plane)) return;

        Vector3 position = GetSafePositionOnPlane(plane);
        if (!IsPositionPlayable(position)) return;

        PlaceEntry(position);
    }

    bool IsPlaneUsable(ARPlane plane)
    {
        if (plane == null) return false;
        if (plane.alignment != PlaneAlignment.HorizontalUp) return false;
        if (plane.trackingState != TrackingState.Tracking) return false;
        if (plane.size.x < minPlaneSize || plane.size.y < minPlaneSize) return false;
        return true;
    }

    Vector3 GetSafePositionOnPlane(ARPlane plane)
    {
        Vector2 size = plane.size;
        float margin = 0.3f;

        float x = Random.Range(-size.x / 2 + margin, size.x / 2 - margin);
        float z = Random.Range(-size.y / 2 + margin, size.y / 2 - margin);

        return plane.transform.TransformPoint(new Vector3(x, 0, z));
    }

    bool IsPositionPlayable(Vector3 pos)
    {
        Camera cam = Camera.main;
        if (!cam) return false;

        float dist = Vector3.Distance(cam.transform.position, pos);
        if (dist < 1.5f || dist > 4f) return false;

        Vector3 dir = (pos - cam.transform.position).normalized;
        if (Vector3.Dot(cam.transform.forward, dir) < 0.5f) return false;

        return true;
    }

    void PlaceEntry(Vector3 pos)
    {
        var placedType = selectedEntryType;
        GameObject entry = GameObject.CreatePrimitive(PrimitiveType.Cube);
        entry.transform.position = pos;
        entry.transform.rotation = Quaternion.identity;
        entry.transform.localScale = new Vector3(0.3f, 0.5f, 0.05f);
        entry.transform.SetParent(placementsRoot, true);
        Destroy(entry.GetComponent<Collider>());

        entry.GetComponent<MeshRenderer>().material.color = Color.green;
        entry.name = $"Entry_{placedEntries.Count}";

        placedEntries.Add(entry);
        selectedEntryType = -1;

        if (uiManager != null)
        {
            uiManager.OnPlaced(placedType);
        }
    }

    public void StartGame()
    {
        isPlacementMode = false;
        planeManager.enabled = false;
    }
    
    // Methods required by UIManager
    public void DisableARMode()
    {
        if (raycastManager != null) raycastManager.enabled = false;
        if (planeManager != null) planeManager.enabled = false;
    }
    
    public void EnterARMode()
    {
        if (raycastManager != null) raycastManager.enabled = true;
        if (planeManager != null) planeManager.enabled = true;
    }
    
    public void StartCalibration()
    {
        isCalibrating = true;
        isPlacementMode = false;
    }
    
    public void EndCalibration()
    {
        isCalibrating = false;
        isPlacementMode = true;
        timeSinceCalibrationEnd = 0f;
        autoSpawnCompleted = false;
    }
    
    public string GetARStatus()
    {
        string status = "AR Status:\n";
        status += $"Raycast Manager: {(raycastManager != null && raycastManager.enabled ? "Enabled" : "Disabled")}\n";
        status += $"Plane Manager: {(planeManager != null && planeManager.enabled ? "Enabled" : "Disabled")}\n";
        status += $"Planes detected: {GetDetectedPlaneCount()}\n";
        return status;
    }
    
    public int GetDetectedPlaneCount()
    {
        if (planeManager == null) return 0;
        int count = 0;
        foreach (var plane in planeManager.trackables)
        {
            if (plane.trackingState == TrackingState.Tracking)
                count++;
        }
        return count;
    }
    
    public int GetPlacedEntriesCount()
    {
        return placedEntries.Count;
    }
    
    public void SelectSlot(int slotIndex)
    {
        // This method can be implemented if you need slot functionality
        Debug.Log($"Selected slot: {slotIndex}");
    }
    
    public void SelectTypeToPlace(int typeIndex, UIManager ui)
    {
        selectedEntryType = typeIndex;
        uiManager = ui;
        Debug.Log($"Selected type to place: {typeIndex}");
    }
}
