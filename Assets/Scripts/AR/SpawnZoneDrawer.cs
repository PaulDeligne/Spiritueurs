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
    public ARPlaneManager planeManager;

    [Header("Prefabs")]
    public GameObject spawnZonePrefab;

    [Header("Settings")]
    public float minPointDistance = 0.25f;

    private static readonly List<ARRaycastHit> hits = new();

    private readonly List<Vector3> points = new();
    private LineRenderer line;
    private SpawnZone currentZone;
    private ARPlane lockedPlane;

    private readonly List<SpawnZone> zones = new();

    void Update()
    {
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;
        if (!touch.press.isPressed) return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 screenPos = touch.position.ReadValue();
        if (!raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            return;

        ARPlane plane = planeManager.GetPlane(hits[0].trackableId);
        if (!IsPlaneUsable(plane)) return;

        Vector3 worldPos = hits[0].pose.position;

        if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
        {
            StartZone(worldPos, plane);
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

    bool IsPlaneUsable(ARPlane plane)
    {
        if (plane == null) return false;
        if (plane.alignment != PlaneAlignment.HorizontalUp) return false;
        if (plane.trackingState != TrackingState.Tracking) return false;
        if (plane.size.x < 2f || plane.size.y < 2f) return false;
        return true;
    }

    void StartZone(Vector3 pos, ARPlane plane)
    {
        lockedPlane = plane;

        GameObject go = Instantiate(spawnZonePrefab);
        line = go.GetComponent<LineRenderer>();
        currentZone = go.GetComponent<SpawnZone>();

        points.Clear();
        AddPoint(ProjectOnPlane(pos));

        line.positionCount = 1;
        line.SetPosition(0, points[0]);
    }

    void AddPoint(Vector3 pos)
    {
        if (lockedPlane == null) return;

        Vector3 projected = ProjectOnPlane(pos);
        if (points.Count > 0 &&
            Vector3.Distance(points[^1], projected) < minPointDistance)
            return;

        points.Add(projected);
        line.positionCount = points.Count;
        line.SetPosition(points.Count - 1, projected);
    }

    void FinishZone()
    {
        if (points.Count < 3) return;

        line.loop = true;
        currentZone.Initialize(points);
        zones.Add(currentZone);

        lockedPlane = null;
        points.Clear();
    }

    Vector3 ProjectOnPlane(Vector3 pos)
    {
        Vector3 local = lockedPlane.transform.InverseTransformPoint(pos);
        local.y = 0;
        return lockedPlane.transform.TransformPoint(local);
    }

    public void StartGame()
    {
        foreach (var z in zones)
            z.Activate();
    }
}
