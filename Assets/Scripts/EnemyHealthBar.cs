using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider? healthSlider;
    [SerializeField] private bool hideOnDeath = true;

    private Health? health;
    private Camera? mainCamera;

    private void Awake()
    {
        // Find the Health component on the parent zombie object
        health = GetComponentInParent<Health>();
        mainCamera = Camera.main;

        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            // Subscribe to the new events from the refactored Health script
            health.Damaged += OnHealthChanged;
            health.Died += OnDeath;
        }

        // Initialize the slider value immediately
        UpdateVisuals();
    }

    private void OnDisable()
    {
        if (health != null)
        {
            // Unsubscribe to prevent memory leaks (standard C# practice)
            health.Damaged -= OnHealthChanged;
            health.Died -= OnDeath;
        }
    }

    private void LateUpdate()
    {
        // Simple Billboard effect: Ensure the UI always faces the camera
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }

    private void OnHealthChanged(int damageAmount)
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (healthSlider != null && health != null)
        {
            // Use the new public method to get the 0.0 to 1.0 value
            healthSlider.value = health.GetHealthNormalized();
        }
    }

    private void OnDeath(Health h)
    {
        if (hideOnDeath)
        {
            gameObject.SetActive(false);
        }
    }
}