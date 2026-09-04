using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TreeHazard : MonoBehaviour
{
    [SerializeField] private bool pruneOnTouch = true;

    public bool PruneOnTouch => pruneOnTouch;

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }
}
