using UnityEngine;
using System.Collections;

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public float spawnInterval = 2f;
    public int maxEnemies = 5;

    private int spawned;
    private bool active;

    public void Activate()
    {
        if (active) return;
        active = true;
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (spawned < maxEnemies)
        {
            Instantiate(enemyPrefab, transform.position, transform.rotation);
            spawned++;
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
