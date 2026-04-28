using UnityEngine;

public sealed class PierceCost : MonoBehaviour
{
    [SerializeField] private int cost = 1;

    public int Cost => Mathf.Max(1, cost);
}
