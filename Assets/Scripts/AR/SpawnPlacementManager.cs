using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SpawnPlacementManager : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject spawnPointPrefab;

    [Header("State")]
    public bool placementMode = true;

    private static List<ARRaycastHit> hits = new();
    private readonly List<SpawnPoint> spawnPoints = new();
    [Header("Placement UI")]
    public int selectedSlot = -1;
    public Material placeholderMaterial;

    private readonly Dictionary<int, SpawnPoint> slotPlacements = new();
    [Header("AR Visuals")]
    public UnityEngine.XR.ARFoundation.ARCameraBackground cameraBackground;

    void Awake()
    {
        if (cameraBackground == null && Camera.main != null)
            cameraBackground = Camera.main.GetComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>();
    }

    void Update()
    {
        if (!placementMode) return;
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;

        if (!touch.press.isPressed) return;
        if (touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Began) return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        // require a selected slot from the UI
        if (selectedSlot < 0) return;

        Vector2 pos = touch.position.ReadValue();

        if (raycastManager.Raycast(pos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;

            // if this slot already has a placement, replace it
            if (slotPlacements.TryGetValue(selectedSlot, out var old))
            {
                spawnPoints.Remove(old);
                Destroy(old.gameObject);
                slotPlacements.Remove(selectedSlot);
            }

            GameObject go;
            SpawnPoint sp;

            if (spawnPointPrefab != null)
            {
                go = Instantiate(spawnPointPrefab, pose.position, pose.rotation);
                sp = go.GetComponent<SpawnPoint>();
                if (sp == null) sp = go.AddComponent<SpawnPoint>();
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = pose.position;
                go.transform.rotation = pose.rotation;
                go.transform.localScale = Vector3.one * 0.2f;
                if (placeholderMaterial != null)
                {
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null) mr.material = placeholderMaterial;
                }
                sp = go.AddComponent<SpawnPoint>();
                // remove collider to avoid physics issues
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            go.name = $"SpawnSlot_{selectedSlot}";

            spawnPoints.Add(sp);
            slotPlacements[selectedSlot] = sp;
        }
    }

    public void StartGame()
    {
        placementMode = false;

        planeManager.enabled = false;

        // activate all placed spawn points
        foreach (SpawnPoint sp in spawnPoints)
            sp.Activate();
    }

    // Called from UI to select which slot will be placed on next tap
    public void SelectSlot(int index)
    {
        selectedSlot = index;
    }

    // Called when entering AR mode from the start menu
    public void EnterARMode()
    {
        placementMode = true;
        if (planeManager != null) planeManager.enabled = true;
        // enable camera feed so AR background becomes visible
        if (cameraBackground != null) cameraBackground.enabled = true;

        // ensure main camera clear flags allow the camera background to show
        if (Camera.main != null)
        {
            Camera.main.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
        }
    }

    public void DisableARMode()
    {
        placementMode = false;
        if (planeManager != null) planeManager.enabled = false;
        if (cameraBackground != null) cameraBackground.enabled = false;

        // ensure the main camera shows a solid black background instead of transparent feed
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
        }
    }
}
