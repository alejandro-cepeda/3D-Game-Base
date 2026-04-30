using UnityEngine;

public sealed class ShopZone : MonoBehaviour
{
    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            c.isTrigger = true;
        }
    }
}
