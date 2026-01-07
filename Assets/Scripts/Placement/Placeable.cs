using UnityEngine;

/// <summary>
/// Simple component to identify a placed entry and its type.
/// Attach this to entry prefabs if needed.
/// </summary>
public class Placeable : MonoBehaviour
{
    public PlaceableType type;

    public void SetType(PlaceableType t)
    {
        type = t;
    }
}
