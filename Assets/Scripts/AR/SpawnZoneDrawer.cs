using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SpawnZoneDrawer : MonoBehaviour
{
    [Header("AR")]
    public ARRaycastManager raycastManager;

    [Header("Prefabs")]
    public GameObject spawnZonePrefab;

    [Header("Draw Settings")]
    public float minPointDistance = 0.2f; // 20 cm

    private List<Vector3> currentPoints = new();
    private LineRenderer currentLine;
    private SpawnZone currentZone;

    private static List<ARRaycastHit> hits = new();

    private List<SpawnZone> zones = new();


    void Update()
    {
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;
        if (!touch.press.isPressed) return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 touchPos = touch.position.ReadValue();

        if (!raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon))
            return;

        Vector3 worldPos = hits[0].pose.position;

        if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
        {
            StartZone(worldPos);
        }
        else if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            AddPoint(worldPos);
        }
        else if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended)
        {
            FinishZone();
        }
    }

    void StartZone(Vector3 pos)
    {
        GameObject zoneGO = Instantiate(spawnZonePrefab);
        currentZone = zoneGO.GetComponent<SpawnZone>();
        currentLine = zoneGO.GetComponent<LineRenderer>();

        currentPoints.Clear();
        currentPoints.Add(pos);

        currentLine.positionCount = 1;
        currentLine.SetPosition(0, pos);
    }

    void AddPoint(Vector3 pos)
    {
        if (Vector3.Distance(currentPoints[^1], pos) < minPointDistance)
            return;

        currentPoints.Add(pos);
        currentLine.positionCount = currentPoints.Count;
        currentLine.SetPosition(currentPoints.Count - 1, pos);
    }

    void FinishZone()
    {
        if (currentPoints.Count < 3)
            return;

        currentLine.loop = true;
        currentZone.Initialize(currentPoints);
        zones.Add(currentZone);
    }
    public void StartGame()
    {
        foreach (SpawnZone sz in zones)
            sz.Activate();
    }
}
