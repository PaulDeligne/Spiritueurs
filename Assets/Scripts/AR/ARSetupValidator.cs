using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

/// <summary>
/// Validates and reports AR setup issues at runtime.
/// Add this to any GameObject in your scene.
/// </summary>
public class ARSetupValidator : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;
    
    private XROrigin xrOrigin;
    private ARSession arSession;
    private Camera arCamera;
    
    void Start()
    {
        ValidateSetup();
    }
    
    public void ValidateSetup()
    {
        Log("=== AR SETUP VALIDATION ===");
        
        bool hasErrors = false;
        
        // 1. Check AR Session
        arSession = FindObjectOfType<ARSession>();
        if (arSession == null)
        {
            LogError("AR Session NOT FOUND! Add an AR Session to your scene.");
            hasErrors = true;
        }
        else
        {
            Log($"✓ AR Session found: {arSession.gameObject.name}");
        }
        
        // 2. Check XR Origin
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin == null)
        {
            LogError("XR Origin NOT FOUND! Your scene needs an XR Origin.");
            LogError("In Unity: GameObject > XR > XR Origin (Mobile AR)");
            hasErrors = true;
        }
        else
        {
            Log($"✓ XR Origin found: {xrOrigin.gameObject.name}");
            
            // Check if XR Origin has a camera assigned
            if (xrOrigin.Camera == null)
            {
                LogError("XR Origin has no Camera assigned!");
                hasErrors = true;
            }
            else
            {
                Log($"✓ XR Origin Camera: {xrOrigin.Camera.name}");
                arCamera = xrOrigin.Camera;
            }
        }
        
        // 3. Check Main Camera
        if (Camera.main == null)
        {
            LogError("No Main Camera found! Tag your AR camera as 'MainCamera'.");
            hasErrors = true;
        }
        else
        {
            Log($"✓ Main Camera: {Camera.main.name}");
            
            // Check if main camera is child of XR Origin
            if (xrOrigin != null)
            {
                bool isChildOfXROrigin = Camera.main.transform.IsChildOf(xrOrigin.transform);
                if (!isChildOfXROrigin)
                {
                    LogError($"Main Camera is NOT a child of XR Origin!");
                    LogError($"Camera parent: {(Camera.main.transform.parent != null ? Camera.main.transform.parent.name : "NONE (root)")}");
                    LogError("FIX: Drag your Main Camera to be a child of XR Origin in the hierarchy.");
                    hasErrors = true;
                }
                else
                {
                    Log("✓ Main Camera is child of XR Origin");
                }
            }
            
            // Check for TrackedPoseDriver
            var trackedPoseDriver = Camera.main.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            var legacyTrackedPoseDriver = Camera.main.GetComponent<UnityEngine.XR.ARFoundation.ARPoseDriver>();
            
            if (trackedPoseDriver == null && legacyTrackedPoseDriver == null)
            {
                LogError("Camera has NO TrackedPoseDriver or ARPoseDriver!");
                LogError("FIX: Add 'Tracked Pose Driver' component to your camera.");
                hasErrors = true;
            }
            else
            {
                if (trackedPoseDriver != null)
                    Log($"✓ TrackedPoseDriver found on camera");
                if (legacyTrackedPoseDriver != null)
                    Log($"✓ ARPoseDriver found on camera");
            }
            
            // Check ARCameraManager
            var arCameraManager = Camera.main.GetComponent<ARCameraManager>();
            if (arCameraManager == null)
            {
                LogWarning("Camera has no ARCameraManager - camera feed won't display.");
            }
            else
            {
                Log("✓ ARCameraManager found");
            }
            
            // Check ARCameraBackground
            var arCameraBackground = Camera.main.GetComponent<ARCameraBackground>();
            if (arCameraBackground == null)
            {
                LogWarning("Camera has no ARCameraBackground - camera feed won't display.");
            }
            else
            {
                Log("✓ ARCameraBackground found");
            }
        }
        
        // 4. Check ARRaycastManager
        var raycastManager = FindObjectOfType<ARRaycastManager>();
        if (raycastManager == null)
        {
            LogError("ARRaycastManager NOT FOUND! Add it to XR Origin.");
            hasErrors = true;
        }
        else
        {
            Log($"✓ ARRaycastManager found on: {raycastManager.gameObject.name}");
        }
        
        // 5. Check ARPlaneManager
        var planeManager = FindObjectOfType<ARPlaneManager>();
        if (planeManager == null)
        {
            LogWarning("ARPlaneManager not found - plane detection won't work.");
        }
        else
        {
            Log($"✓ ARPlaneManager found, detection mode: {planeManager.requestedDetectionMode}");
        }
        
        // Summary
        Log("=== VALIDATION COMPLETE ===");
        if (hasErrors)
        {
            LogError("ERRORS FOUND! AR tracking will NOT work until fixed.");
        }
        else
        {
            Log("✓ All checks passed! AR should work correctly.");
        }
    }
    
    private void Log(string message)
    {
        if (logToConsole)
            Debug.Log($"[ARSetupValidator] {message}");
    }
    
    private void LogWarning(string message)
    {
        if (logToConsole)
            Debug.LogWarning($"[ARSetupValidator] {message}");
    }
    
    private void LogError(string message)
    {
        if (logToConsole)
            Debug.LogError($"[ARSetupValidator] {message}");
    }
}
