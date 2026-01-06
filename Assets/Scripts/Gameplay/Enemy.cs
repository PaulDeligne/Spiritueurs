using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 0.5f;
    private Transform target;

    void Start()
    {
        target = Camera.main.transform;
    }

    void Update()
    {
        if (!target) return;

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.LookAt(target);
    }
}
