using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using TMPro;

/// <summary>
/// Debug script to visualize AR tracking state and placed objects
/// Attach to a GameObject in your scene to see what's happening
/// </summary>
public class ARDebugger : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;
    
    private ARSession arSession;
    private XROrigin xrOrigin;
    private Camera arCamera;
    
    // Store camera starting position to detect if it moves
    private Vector3 lastCameraPosition;
    private int framesSinceCameraMove = 0;
    
    void Start()
    {
        arSession = FindFirstObjectByType<ARSession>();
        xrOrigin = FindFirstObjectByType<XROrigin>();
        arCamera = Camera.main;
        
        if (arCamera != null)
            lastCameraPosition = arCamera.transform.position;
            
        Debug.Log($"[ARDebugger] XROrigin found: {xrOrigin != null}");
    }
    
    void Update()
    {
        if (debugText == null) return;
        
        string debug = "<b>=== AR DEBUG ===</b>\n\n";
        
        // AR Session State
        debug += $"<color=yellow>AR State:</color> {ARSession.state}\n";
        debug += $"<color=yellow>Not Tracking:</color> {ARSession.notTrackingReason}\n";
        
        // XR Origin info
        if (xrOrigin != null)
        {
            debug += $"<color=green>XROrigin: OK</color>\n";
        }
        else
        {
            debug += $"<color=red>XROrigin: MISSING!</color>\n";
        }
        debug += "\n";
        
        // Camera info
        if (arCamera != null)
        {
            Vector3 camPos = arCamera.transform.position;
            debug += $"<color=cyan>Cam Pos:</color> ({camPos.x:F2}, {camPos.y:F2}, {camPos.z:F2})\n";
            
            // Check if camera is actually moving in world space
            float cameraMoveDistance = Vector3.Distance(camPos, lastCameraPosition);
            if (cameraMoveDistance > 0.001f)
            {
                framesSinceCameraMove = 0;
                lastCameraPosition = camPos;
            }
            else
            {
                framesSinceCameraMove++;
            }
            
            if (framesSinceCameraMove > 60)
            {
                debug += $"<color=red>CAM NOT MOVING!</color>\n";
            }
            else
            {
                debug += $"<color=green>Cam tracking OK</color>\n";
            }
        }
        
        // Placed objects info
        debug += "\n<color=yellow>=== PLACED OBJECTS ===</color>\n";
        
        // Find all placed entries
        var entries = GameObject.FindGameObjectsWithTag("Untagged");
        int entryCount = 0;
        foreach (var obj in entries)
        {
            if (obj.name.Contains("Door") || obj.name.Contains("Window") || obj.name.Contains("Hatch"))
            {
                debug += $"\n{obj.name}:\n";
                debug += $"  World Pos: {obj.transform.position:F2}\n";
                debug += $"  Parent: {(obj.transform.parent != null ? obj.transform.parent.name : "NONE")}\n";
                
                // Check if object is moving with camera
                if (arCamera != null)
                {
                    float distToCam = Vector3.Distance(obj.transform.position, arCamera.transform.position);
                    debug += $"  Dist to Cam: {distToCam:F2}m\n";
                }
                
                entryCount++;
                if (entryCount >= 3) break; // Limit display
            }
        }
        
        if (entryCount == 0)
        {
            debug += "No entries placed yet.\n";
        }
        
        debugText.text = debug;
    }
}
