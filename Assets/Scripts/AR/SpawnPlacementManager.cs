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

        Vector2 pos = touch.position.ReadValue();

        if (raycastManager.Raycast(pos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;

            GameObject go = Instantiate(
                spawnPointPrefab,
                pose.position,
                pose.rotation
            );

            spawnPoints.Add(go.GetComponent<SpawnPoint>());
        }
    }

    public void StartGame()
    {
        placementMode = false;

        planeManager.enabled = false;

        foreach (SpawnPoint sp in spawnPoints)
            sp.Activate();
    }
}
