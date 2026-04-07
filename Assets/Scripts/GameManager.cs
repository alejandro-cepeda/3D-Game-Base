using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public sealed class GameManager : MonoBehaviour
{
    private enum UpgradeId
    {
        WiderView,
        WeaponUpgrade,
        LaserSight,
        MoveSpeed,
        Accuracy,
        HealthRegen,
        MaxHealth,
        BulletPierce,
        Radiation,
        RadiationRadius
    }
    [SerializeField] private string sceneToReload = "MainGameplay";
    [SerializeField] private bool debugEvents;
    [SerializeField] private int upgradeScoreThreshold = 10;
    [SerializeField] private float widerViewZoomOutDistance = 7.5f;
    [SerializeField] private float widerViewOrthoSizeIncrease = 2.5f;
    [SerializeField] private float widerViewDiminishingMultiplier = 0.75f;

    private int score;
    private TMP_Text? scoreText;
    private TMP_Text? weaponText;
    private Image? playerHealthFill;
    private GameObject? gameOverPanel;
    private GameObject? upgradePanel;

    private int nextUpgradeScore;
    private int widerViewPickCount;
    private bool laserSightUnlocked;
    private int accuracyPickCount;
    private int regenPickCount;
    private int maxHealthPickCount;
    private int piercePickCount;
    private int radiationPickCount;
    private int radiationRadiusPickCount;

    private UpgradeId[] currentUpgradeChoices = new UpgradeId[3];

    private Health? playerHealth;
    private PlayerCombat? playerCombat;

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
        WirePlayerCombat();
        SetScoreText();
        UpdatePlayerHealthUi();
        HideGameOver();
        HideUpgradeChoice();

        if (nextUpgradeScore <= 0)
        {
            nextUpgradeScore = upgradeScoreThreshold;
        }
    }

    public void AddScore(int amount)
    {
        score = Mathf.Max(0, score + amount);
        SetScoreText();

        if (nextUpgradeScore > 0 && score >= nextUpgradeScore)
        {
            ShowUpgradeChoice();
        }
    }

    public void ResetScore()
    {
        score = 0;
        SetScoreText();
    }

    public void ShowGameOver()
    {
        HideUpgradeChoice();
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
    }

    private void ShowUpgradeChoice()
    {
        nextUpgradeScore += upgradeScoreThreshold;

        RollUpgradeChoices();
        UpdateUpgradeLabels();

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    private void HideUpgradeChoice()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }

    private void ApplyUpgrade(int index)
    {
        Debug.Log($"[GameManager] ApplyUpgrade({index}) clicked.", this);

        PlayerMovement? playerMovement = FindFirstObjectByType<PlayerMovement>();
        PlayerCombat? playerCombat = playerMovement != null ? playerMovement.GetComponent<PlayerCombat>() : null;
        Camera? mainCamera = Camera.main;
        CameraFollow? cameraFollow = mainCamera != null ? mainCamera.GetComponent<CameraFollow>() : null;
        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }

        UpgradeId choice = currentUpgradeChoices[Mathf.Clamp(index, 0, currentUpgradeChoices.Length - 1)];

        if (choice == UpgradeId.WiderView)
        {
            float multiplier = Mathf.Pow(widerViewDiminishingMultiplier, widerViewPickCount);
            widerViewPickCount++;
            float zoomDistance = widerViewZoomOutDistance * multiplier;
            float orthoIncrease = widerViewOrthoSizeIncrease * multiplier;

            if (cameraFollow == null && mainCamera != null)
            {
                cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();

                if (playerMovement != null)
                {
                    cameraFollow.SetTarget(playerMovement.transform);
                }
            }

            if (cameraFollow != null)
            {
                cameraFollow.ZoomOutTopDown(zoomDistance);

                if (playerMovement != null)
                {
                    cameraFollow.SetTarget(playerMovement.transform);
                }
            }

            if (mainCamera != null && mainCamera.orthographic)
            {
                mainCamera.orthographicSize = Mathf.Max(0.01f, mainCamera.orthographicSize + orthoIncrease);
            }
        }
        else if (choice == UpgradeId.WeaponUpgrade)
        {
            if (playerCombat != null)
            {
                if (playerCombat.CurrentGun == PlayerCombat.WeaponId.Pistol)
                {
                    playerCombat.SetGun(PlayerCombat.WeaponId.Shotgun);
                }
                else if (playerCombat.CurrentGun == PlayerCombat.WeaponId.Shotgun)
                {
                    playerCombat.SetGun(PlayerCombat.WeaponId.AssaultRifle);
                }
            }
        }
        else if (choice == UpgradeId.LaserSight)
        {
            if (playerMovement != null)
            {
                PlayerLaserSight? sight = playerMovement.GetComponent<PlayerLaserSight>();
                if (sight == null)
                {
                    sight = playerMovement.gameObject.AddComponent<PlayerLaserSight>();
                }

                sight.SetEnabled(true);
                laserSightUnlocked = true;
            }
        }
        else if (choice == UpgradeId.MoveSpeed)
        {
            if (playerMovement != null)
            {
                playerMovement.AddMoveSpeed(1.5f);
            }
        }
        else if (choice == UpgradeId.Accuracy)
        {
            if (playerCombat != null)
            {
                accuracyPickCount++;
                playerCombat.ImproveAccuracy(0.85f);
            }
        }
        else if (choice == UpgradeId.HealthRegen)
        {
            if (playerMovement != null)
            {
                PlayerHealthRegen? regen = playerMovement.GetComponent<PlayerHealthRegen>();
                if (regen == null)
                {
                    regen = playerMovement.gameObject.AddComponent<PlayerHealthRegen>();
                }

                regen.AddRegen(1.0f);
                regenPickCount++;
            }
        }
        else if (choice == UpgradeId.MaxHealth)
        {
            if (playerHealth != null)
            {
                playerHealth.AddMaxHealth(25, true);
                maxHealthPickCount++;
            }
        }
        else if (choice == UpgradeId.BulletPierce)
        {
            if (playerCombat != null)
            {
                playerCombat.AddPierce(1);
                piercePickCount++;
            }
        }
        else if (choice == UpgradeId.Radiation)
        {
            if (playerMovement != null)
            {
                PlayerRadiationAura? aura = playerMovement.GetComponent<PlayerRadiationAura>();
                if (aura == null)
                {
                    aura = playerMovement.gameObject.AddComponent<PlayerRadiationAura>();
                }

                radiationPickCount++;
            }
        }
        else if (choice == UpgradeId.RadiationRadius)
        {
            if (playerMovement != null)
            {
                PlayerRadiationAura? aura = playerMovement.GetComponent<PlayerRadiationAura>();
                if (aura == null)
                {
                    aura = playerMovement.gameObject.AddComponent<PlayerRadiationAura>();
                }

                aura.AddRadius(1.5f);
                radiationRadiusPickCount++;
            }
        }

        HideUpgradeChoice();
        Time.timeScale = 1f;
    }

    private void UpdateUpgradeLabels()
    {
        if (upgradePanel == null)
        {
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            Transform? buttonText = upgradePanel.transform.Find($"Upgrade{i}/Text");
            if (buttonText == null)
            {
                continue;
            }

            TMP_Text? text = buttonText.GetComponent<TMP_Text>();
            if (text == null)
            {
                continue;
            }

            text.text = GetUpgradeLabel(currentUpgradeChoices[i]);
        }
    }

    private void RollUpgradeChoices()
    {
        var pool = GetUpgradePool();
        for (int i = 0; i < 3; i++)
        {
            if (pool.Count == 0)
            {
                currentUpgradeChoices[i] = UpgradeId.MoveSpeed;
                continue;
            }

            int idx = UnityEngine.Random.Range(0, pool.Count);
            currentUpgradeChoices[i] = pool[idx];
            pool.RemoveAt(idx);
        }
    }

    private System.Collections.Generic.List<UpgradeId> GetUpgradePool()
    {
        var pool = new System.Collections.Generic.List<UpgradeId>
        {
            UpgradeId.WiderView,
            UpgradeId.MoveSpeed,
            UpgradeId.Accuracy,
            UpgradeId.HealthRegen,
            UpgradeId.MaxHealth,
            UpgradeId.BulletPierce
        };

        if (!laserSightUnlocked)
        {
            pool.Add(UpgradeId.LaserSight);
        }

        if (playerCombat != null && playerCombat.CurrentGun != PlayerCombat.WeaponId.AssaultRifle)
        {
            pool.Add(UpgradeId.WeaponUpgrade);
        }

        bool hasRadiation = FindFirstObjectByType<PlayerRadiationAura>() != null || radiationPickCount > 0;
        if (!hasRadiation)
        {
            pool.Add(UpgradeId.Radiation);
        }
        else
        {
            pool.Add(UpgradeId.RadiationRadius);
        }

        return pool;
    }

    private string GetUpgradeLabel(UpgradeId id)
    {
        return id switch
        {
            UpgradeId.WiderView => "Wider View",
            UpgradeId.WeaponUpgrade => playerCombat == null ? "Weapon Upgrade" : playerCombat.CurrentGun switch
            {
                PlayerCombat.WeaponId.Pistol => "Upgrade: Shotgun",
                PlayerCombat.WeaponId.Shotgun => "Upgrade: Assault Rifle",
                _ => "Weapon Upgrade"
            },
            UpgradeId.LaserSight => "Laser Sight",
            UpgradeId.MoveSpeed => "Move Faster",
            UpgradeId.Accuracy => "Accuracy +",
            UpgradeId.HealthRegen => "Health Regen",
            UpgradeId.MaxHealth => "Max Health +",
            UpgradeId.BulletPierce => "Bullet Pierce +",
            UpgradeId.Radiation => "Radiation Aura",
            UpgradeId.RadiationRadius => "Radiation Radius +",
            _ => "Upgrade"
        };
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        score = 0;
        nextUpgradeScore = upgradeScoreThreshold;
        widerViewPickCount = 0;
        laserSightUnlocked = false;
        accuracyPickCount = 0;
        regenPickCount = 0;
        maxHealthPickCount = 0;
        piercePickCount = 0;
        radiationPickCount = 0;
        radiationRadiusPickCount = 0;
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

    private void WirePlayerCombat()
    {
        PlayerMovement? player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        PlayerCombat? combat = player.GetComponent<PlayerCombat>();
        if (combat == null)
        {
            return;
        }

        if (playerCombat != null)
        {
            playerCombat.GunChanged -= OnGunChanged;
        }

        playerCombat = combat;
        playerCombat.GunChanged += OnGunChanged;
        UpdateWeaponText();
    }

    private void OnGunChanged(PlayerCombat.WeaponId weapon)
    {
        UpdateWeaponText();
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
        if (scoreText != null && weaponText != null && playerHealthFill != null && gameOverPanel != null && upgradePanel != null)
        {
            return;
        }

        Canvas canvas = FindOrCreateHudCanvas();

        EnsureEventSystem();

        if (scoreText == null)
        {
            GameObject scoreObject = new GameObject("ScoreText", typeof(RectTransform), typeof(TextMeshProUGUI));
            scoreObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = scoreObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(400f, 60f);

            TextMeshProUGUI text = scoreObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 32;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;

            scoreText = text;
        }

        if (weaponText == null)
        {
            GameObject weaponObject = new GameObject("WeaponText", typeof(RectTransform), typeof(TextMeshProUGUI));
            weaponObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = weaponObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -48f);
            rect.sizeDelta = new Vector2(500f, 40f);

            TextMeshProUGUI text = weaponObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 22;
            text.color = new Color(1f, 1f, 1f, 0.9f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;

            weaponText = text;
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

            GameObject labelObject = new GameObject("GameOverText", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(panelObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.65f);
            labelRect.anchorMax = new Vector2(0.5f, 0.65f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(900f, 120f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 56;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
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

            GameObject buttonTextObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            buttonTextObject.transform.SetParent(buttonObject.transform, false);

            RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonTextRect.pivot = new Vector2(0.5f, 0.5f);
            buttonTextRect.anchoredPosition = Vector2.zero;
            buttonTextRect.sizeDelta = new Vector2(320f, 80f);

            TextMeshProUGUI buttonText = buttonTextObject.GetComponent<TextMeshProUGUI>();
            buttonText.fontSize = 32;
            buttonText.color = Color.black;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.raycastTarget = false;
            buttonText.text = "Restart";

            gameOverPanel = panelObject;
            gameOverPanel.SetActive(false);
        }

        if (upgradePanel == null)
        {
            GameObject panelObject = new GameObject("UpgradePanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.6f);

            GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(panelObject.transform, false);

            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.75f);
            titleRect.anchorMax = new Vector2(0.5f, 0.75f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(900f, 100f);

            TextMeshProUGUI titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 44;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.raycastTarget = false;
            titleText.text = "Choose an Upgrade";

            CreateUpgradeButton(panelObject.transform, new Vector2(-360f, 0f), "Wider View", 0);
            CreateUpgradeButton(panelObject.transform, new Vector2(0f, 0f), "Weapon Upgrade", 1);
            CreateUpgradeButton(panelObject.transform, new Vector2(360f, 0f), "Move Faster", 2);

            upgradePanel = panelObject;
            upgradePanel.SetActive(false);
        }
    }

    private void CreateUpgradeButton(Transform parent, Vector2 anchoredPosition, string label, int index)
    {
        GameObject buttonObject = new GameObject($"Upgrade{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(300f, 120f);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = GetUiSprite();
        image.color = new Color(1f, 1f, 1f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => ApplyUpgrade(index));

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(280f, 110f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 26;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = label;
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

    private void UpdateWeaponText()
    {
        if (weaponText == null)
        {
            return;
        }

        if (playerCombat == null)
        {
            weaponText.text = "Weapon: -";
            return;
        }

        weaponText.text = $"Weapon: {playerCombat.CurrentGun}";
    }

    private void EnsureEventSystem()
    {
        EventSystem? existing = FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule? inputModule = existing.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.enabled = true;
#else
            StandaloneInputModule? inputModule = existing.GetComponent<StandaloneInputModule>();
            if (inputModule == null)
            {
                inputModule = existing.gameObject.AddComponent<StandaloneInputModule>();
            }

            inputModule.enabled = true;
#endif

            return;
        }

        GameObject obj = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        obj.AddComponent<InputSystemUIInputModule>();
#else
        obj.AddComponent<StandaloneInputModule>();
#endif
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
