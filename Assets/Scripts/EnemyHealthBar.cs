using UnityEngine;
using UnityEngine.UI;

public sealed class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.0f, 0f);
    [SerializeField] private Vector2 size = new Vector2(1.6f, 0.2f);

    private Health? health;
    private Image? fillImage;
    private Transform? canvasTransform;

    private void Awake()
    {
        health = GetComponent<Health>();
        CreateUi();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Changed += OnHealthChanged;
            health.Died += OnDied;
        }

        UpdateFill();
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Changed -= OnHealthChanged;
            health.Died -= OnDied;
        }
    }

    private void LateUpdate()
    {
        if (canvasTransform == null)
        {
            return;
        }

        Camera? camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        canvasTransform.position = transform.position + worldOffset;
        canvasTransform.rotation = Quaternion.LookRotation(canvasTransform.position - camera.transform.position);
    }

    private void OnHealthChanged(Health changed)
    {
        UpdateFill();
    }

    private void OnDied(Health died)
    {
        if (canvasTransform != null)
        {
            Destroy(canvasTransform.gameObject);
        }
    }

    private void UpdateFill()
    {
        if (health == null || fillImage == null)
        {
            return;
        }

        fillImage.fillAmount = health.Normalized;
    }

    private void CreateUi()
    {
        GameObject canvasObject = new GameObject("EnemyHealthBar", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size * 100f;
        canvasRect.localScale = Vector3.one * 0.01f;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(canvasObject.transform, false);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0f);
        backgroundRect.anchorMax = new Vector2(1f, 1f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image background = backgroundObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.7f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(backgroundObject.transform, false);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        Image fill = fillObject.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.color = new Color(0.1f, 0.85f, 0.1f, 1f);

        fillImage = fill;
        canvasTransform = canvasObject.transform;
    }
}
