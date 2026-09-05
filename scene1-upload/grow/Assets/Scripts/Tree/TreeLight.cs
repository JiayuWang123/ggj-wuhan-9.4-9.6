using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TreeLight : MonoBehaviour
{
    [SerializeField] private int flowersNeeded = 3;

    public int FlowersNeeded => flowersNeeded;

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }
}
