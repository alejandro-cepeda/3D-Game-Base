using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private string sceneToReload = "MainGameplay";
    [SerializeField] private bool debugEvents;

    private int score;
    private Text? scoreText;
    private Image? playerHealthFill;
    private GameObject? gameOverPanel;

    private Health? playerHealth;

    public static GameManager? Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureUi();
        InitializeForCurrentScene();
    }

    private void Start()
    {
        InitializeForCurrentScene();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeForCurrentScene();
    }

    private void InitializeForCurrentScene()
    {
        EnsureUi();
        WirePlayerDeath();
        SetScoreText();
        UpdatePlayerHealthUi();
        HideGameOver();
    }

    public void AddScore(int amount)
    {
        score = Mathf.Max(0, score + amount);
        SetScoreText();
    }

    public void ResetScore()
    {
        score = 0;
        SetScoreText();
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        score = 0;
        SceneManager.LoadScene(sceneToReload);
    }

    private void WirePlayerDeath()
    {
        PlayerMovement? player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        Health? health = player.GetComponent<Health>();
        if (health == null)
        {
            return;
        }

        if (playerHealth != null)
        {
            playerHealth.Changed -= OnPlayerHealthChanged;
        }

        playerHealth = health;
        playerHealth.Changed += OnPlayerHealthChanged;

        health.Died -= OnPlayerDied;
        health.Died += OnPlayerDied;

        UpdatePlayerHealthUi();
    }

    private void OnPlayerHealthChanged(Health health)
    {
        UpdatePlayerHealthUi();
    }

    private void OnPlayerDied(Health health)
    {
        if (debugEvents)
        {
            Debug.Log("[GameManager] Player died. Showing Game Over.", this);
        }

        ShowGameOver();
    }

    private void SetScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void EnsureUi()
    {
        if (scoreText != null && playerHealthFill != null && gameOverPanel != null)
        {
            return;
        }

        Canvas canvas = FindOrCreateHudCanvas();

        EnsureEventSystem();

        if (scoreText == null)
        {
            GameObject scoreObject = new GameObject("ScoreText", typeof(RectTransform), typeof(Text));
            scoreObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = scoreObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(400f, 60f);

            Text text = scoreObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;

            scoreText = text;
        }

        if (playerHealthFill == null)
        {
            GameObject hpRoot = new GameObject("PlayerHealth", typeof(RectTransform));
            hpRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = hpRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(20f, -70f);
            rootRect.sizeDelta = new Vector2(320f, 18f);

            GameObject borderObject = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderObject.transform.SetParent(hpRoot.transform, false);

            RectTransform borderRect = borderObject.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 0f);
            borderRect.anchorMax = new Vector2(1f, 1f);
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            Image border = borderObject.GetComponent<Image>();
            border.sprite = GetUiSprite();
            border.color = Color.white;

            GameObject bgObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObject.transform.SetParent(borderObject.transform, false);

            RectTransform bgRect = bgObject.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.offsetMin = new Vector2(2f, 2f);
            bgRect.offsetMax = new Vector2(-2f, -2f);

            Image bg = bgObject.GetComponent<Image>();
            bg.sprite = GetUiSprite();
            bg.color = new Color(0f, 0f, 0f, 0.65f);

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(bgObject.transform, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);

            Image fill = fillObject.GetComponent<Image>();
            fill.sprite = GetUiSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.color = new Color(0.85f, 0.15f, 0.15f, 1f);

            playerHealthFill = fill;
        }

        if (gameOverPanel == null)
        {
            GameObject panelObject = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.6f);

            GameObject labelObject = new GameObject("GameOverText", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(panelObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.65f);
            labelRect.anchorMax = new Vector2(0.5f, 0.65f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(900f, 120f);

            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 56;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = "You Died";

            GameObject buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panelObject.transform, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.45f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.45f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(320f, 80f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0.9f);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(Restart);

            GameObject buttonTextObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            buttonTextObject.transform.SetParent(buttonObject.transform, false);

            RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonTextRect.pivot = new Vector2(0.5f, 0.5f);
            buttonTextRect.anchoredPosition = Vector2.zero;
            buttonTextRect.sizeDelta = new Vector2(320f, 80f);

            Text buttonText = buttonTextObject.GetComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 32;
            buttonText.color = Color.black;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.text = "Restart";

            gameOverPanel = panelObject;
            gameOverPanel.SetActive(false);
        }
    }

    private Canvas FindOrCreateHudCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }
        }

        GameObject canvasObject = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        return canvas;
    }

    private void UpdatePlayerHealthUi()
    {
        if (playerHealthFill == null)
        {
            return;
        }

        if (playerHealth == null)
        {
            playerHealthFill.fillAmount = 0f;
            return;
        }

        playerHealthFill.fillAmount = playerHealth.Normalized;

        if (debugEvents)
        {
            Debug.Log($"[GameManager] Player HP UI: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth} ({playerHealthFill.fillAmount:0.00})", this);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(obj);
    }

    private Sprite GetUiSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }
}
