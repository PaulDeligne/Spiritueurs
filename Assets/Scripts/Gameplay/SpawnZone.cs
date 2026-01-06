using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnZone : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemiesPerWave = 5;
    public float spawnInterval = 1f;

    private List<Vector3> polygon;

    public void Initialize(List<Vector3> points)
    {
        polygon = new List<Vector3>(points);
    }

    public void Activate()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            Vector3 spawnPos = GetRandomPointInPolygon();
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    Vector3 GetRandomPointInPolygon()
    {
        Bounds bounds = GetBounds();

        while (true)
        {
            Vector3 random = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (IsPointInsidePolygon(random))
                return random;
        }
    }

    Bounds GetBounds()
    {
        Bounds b = new Bounds(polygon[0], Vector3.zero);
        foreach (var p in polygon)
            b.Encapsulate(p);
        return b;
    }

    bool IsPointInsidePolygon(Vector3 point)
    {
        int j = polygon.Count - 1;
        bool inside = false;

        for (int i = 0; i < polygon.Count; j = i++)
        {
            Vector3 pi = polygon[i];
            Vector3 pj = polygon[j];

            if (((pi.z > point.z) != (pj.z > point.z)) &&
                (point.x < (pj.x - pi.x) * (point.z - pi.z) / (pj.z - pi.z) + pi.x))
            {
                inside = !inside;
            }
        }
        return inside;
    }
}
