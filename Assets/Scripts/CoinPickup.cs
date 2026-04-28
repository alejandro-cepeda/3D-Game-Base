using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class CoinPickup : MonoBehaviour
{
    [SerializeField] private int amount = 1;
    [SerializeField] private float rotateDegreesPerSecond = 180f;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobFrequency = 2.0f;

    private Vector3 startPos;
    private Rigidbody rb = null!;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Collider c = GetComponent<Collider>();
        c.isTrigger = true;

        startPos = transform.position;
    }

    private void Update()
    {
        transform.Rotate(0f, rotateDegreesPerSecond * Time.deltaTime, 0f, Space.World);
    }

    private void FixedUpdate()
    {
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        rb.MovePosition(startPos + new Vector3(0f, bob, 0f));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoins(amount);
        }

        Destroy(gameObject);
    }

    public void SetAmount(int newAmount)
    {
        amount = Mathf.Max(0, newAmount);
    }
}
