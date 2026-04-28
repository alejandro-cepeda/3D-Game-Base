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
        WeaponPistol,
        WeaponShotgun,
        WeaponAssaultRifle,
        LaserSight,
        MoveSpeed,
        Accuracy,
        HealthRegen,
        MaxHealth,
        BulletPierce,
        Radiation,
        RadiationRadius,
        BulletFreeze,
        BulletPoison,
        BulletExplosive
        ,
        BulletGas,
        ProjectileLifetime
    }
    [SerializeField] private string sceneToReload = "MainGameplay";
    [SerializeField] private bool debugEvents;
    [SerializeField] private int upgradeScoreThreshold = 10;
    [SerializeField] private float widerViewZoomOutDistance = 7.5f;
    [SerializeField] private float widerViewOrthoSizeIncrease = 2.5f;
    [SerializeField] private float widerViewDiminishingMultiplier = 0.75f;

    [Header("Upgrade Caps")]
    [SerializeField] private int accuracyCap = 5;
    [SerializeField] private int regenCap = 5;
    [SerializeField] private int maxHealthCap = 6;
    [SerializeField] private int pierceCap = 5;
    [SerializeField] private int radiationCap = 5;
    [SerializeField] private int moveSpeedCap = 10;
    [SerializeField] private int projectileLifetimeCap = 5;

    private int score;
    private int coins;
    private TMP_Text? scoreText;
    private TMP_Text? weaponText;
    private TMP_Text? upgradesText;
    private TMP_Text? upgradeTiersText;
    private TMP_Text? coinsText;
    private TMP_Text? playerHealthText;
    private Image? playerHealthFill;
    private GameObject? gameOverPanel;
    private GameObject? upgradePanel;
    private GameObject? hudRoot;

    private int nextUpgradeScore;
    private int pendingUpgrades;
    private int widerViewPickCount;
    private bool laserSightUnlocked;
    private int accuracyPickCount;
    private int regenPickCount;
    private int maxHealthPickCount;
    private int piercePickCount;
    private int radiationPickCount;
    private int radiationRadiusPickCount;
    private int moveSpeedPickCount;
    private int projectileLifetimePickCount;

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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TryOpenUpgradeChoice();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (upgradePanel != null && upgradePanel.activeSelf)
            {
                HideUpgradeChoice();
                Time.timeScale = 1f;
            }
        }
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
        SetUpgradesText();
        UpdateCoinsText();
        UpdatePlayerHealthUi();
        HideGameOver();
        HideUpgradeChoice();

        if (hudRoot != null)
        {
            hudRoot.SetActive(SceneManager.GetActiveScene().name == sceneToReload);
        }

        if (nextUpgradeScore <= 0)
        {
            nextUpgradeScore = upgradeScoreThreshold;
        }
    }

    public void AddScore(int amount)
    {
        score = Mathf.Max(0, score + amount);
        SetScoreText();

        while (nextUpgradeScore > 0 && score >= nextUpgradeScore)
        {
            pendingUpgrades++;
            nextUpgradeScore += upgradeScoreThreshold;
        }

        SetUpgradesText();
    }

    public void AddCoins(int amount)
    {
        coins = Mathf.Max(0, coins + amount);
        UpdateCoinsText();
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
        if (pendingUpgrades <= 0)
        {
            return;
        }

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

    private void TryOpenUpgradeChoice()
    {
        if (pendingUpgrades <= 0)
        {
            return;
        }

        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            return;
        }

        ShowUpgradeChoice();
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
        else if (choice == UpgradeId.WeaponPistol)
        {
            if (playerCombat != null)
            {
                playerCombat.SetGun(PlayerCombat.WeaponId.Pistol);
            }
        }
        else if (choice == UpgradeId.WeaponShotgun)
        {
            if (playerCombat != null)
            {
                playerCombat.SetGun(PlayerCombat.WeaponId.Shotgun);
            }
        }
        else if (choice == UpgradeId.WeaponAssaultRifle)
        {
            if (playerCombat != null)
            {
                playerCombat.SetGun(PlayerCombat.WeaponId.AssaultRifle);
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
                if (moveSpeedPickCount < moveSpeedCap)
                {
                    playerMovement.AddMoveSpeed(1.5f);
                    moveSpeedPickCount++;
                }
            }
        }
        else if (choice == UpgradeId.BulletFreeze)
        {
            if (playerCombat != null)
            {
                playerCombat.SetBulletType(PlayerCombat.BulletType.Freeze);
            }
        }
        else if (choice == UpgradeId.BulletPoison)
        {
            if (playerCombat != null)
            {
                playerCombat.SetBulletType(PlayerCombat.BulletType.Poison);
            }
        }
        else if (choice == UpgradeId.BulletExplosive)
        {
            if (playerCombat != null)
            {
                playerCombat.SetBulletType(PlayerCombat.BulletType.Explosive);
            }
        }
        else if (choice == UpgradeId.BulletGas)
        {
            if (playerCombat != null)
            {
                playerCombat.SetBulletType(PlayerCombat.BulletType.Gas);
            }
        }
        else if (choice == UpgradeId.ProjectileLifetime)
        {
            if (playerCombat != null && projectileLifetimePickCount < projectileLifetimeCap)
            {
                playerCombat.AddBulletLifetime(0.25f);
                projectileLifetimePickCount++;
            }
        }
        else if (choice == UpgradeId.Accuracy)
        {
            if (playerCombat != null)
            {
                if (accuracyPickCount < accuracyCap)
                {
                    accuracyPickCount++;
                    if (accuracyPickCount >= accuracyCap)
                    {
                        playerCombat.SetPerfectAccuracy();
                    }
                    else
                    {
                        playerCombat.ImproveAccuracy(0.85f);
                    }
                }
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

                if (regenPickCount < regenCap)
                {
                    regen.AddRegen(1.0f);
                    regenPickCount++;
                }
            }
        }
        else if (choice == UpgradeId.MaxHealth)
        {
            if (playerHealth != null)
            {
                if (maxHealthPickCount < maxHealthCap)
                {
                    playerHealth.AddMaxHealth(25, true);
                    maxHealthPickCount++;
                }
            }
        }
        else if (choice == UpgradeId.BulletPierce)
        {
            if (playerCombat != null)
            {
                if (piercePickCount < pierceCap)
                {
                    playerCombat.AddPierce(1);
                    piercePickCount++;
                }
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

                if (radiationPickCount < radiationCap)
                {
                    radiationPickCount++;
                }
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

                if (radiationPickCount < radiationCap)
                {
                    aura.AddRadius(1.5f);
                    radiationRadiusPickCount++;
                    radiationPickCount++;
                }
            }
        }

        HideUpgradeChoice();

        pendingUpgrades = Mathf.Max(0, pendingUpgrades - 1);
        SetUpgradesText();

        if (pendingUpgrades > 0)
        {
            ShowUpgradeChoice();
        }
        else
        {
            Time.timeScale = 1f;
        }
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
            UpgradeId.BulletPierce,
            UpgradeId.WeaponPistol,
            UpgradeId.WeaponShotgun,
            UpgradeId.WeaponAssaultRifle
        };

        if (moveSpeedPickCount >= moveSpeedCap)
        {
            pool.Remove(UpgradeId.MoveSpeed);
        }

        if (projectileLifetimePickCount < projectileLifetimeCap)
        {
            pool.Add(UpgradeId.ProjectileLifetime);
        }

        if (accuracyPickCount >= accuracyCap)
        {
            pool.Remove(UpgradeId.Accuracy);
        }

        if (regenPickCount >= regenCap)
        {
            pool.Remove(UpgradeId.HealthRegen);
        }

        if (maxHealthPickCount >= maxHealthCap)
        {
            pool.Remove(UpgradeId.MaxHealth);
        }

        if (piercePickCount >= pierceCap)
        {
            pool.Remove(UpgradeId.BulletPierce);
        }

        if (!laserSightUnlocked)
        {
            pool.Add(UpgradeId.LaserSight);
        }

        if (playerCombat != null)
        {
            if (playerCombat.CurrentGun == PlayerCombat.WeaponId.Pistol)
            {
                pool.Remove(UpgradeId.WeaponPistol);
            }
            else if (playerCombat.CurrentGun == PlayerCombat.WeaponId.Shotgun)
            {
                pool.Remove(UpgradeId.WeaponShotgun);
            }
            else
            {
                pool.Remove(UpgradeId.WeaponAssaultRifle);
            }

            if (playerCombat.CurrentBulletType == PlayerCombat.BulletType.Freeze)
            {
                pool.Add(UpgradeId.BulletPoison);
                pool.Add(UpgradeId.BulletExplosive);
                pool.Add(UpgradeId.BulletGas);
            }
            else if (playerCombat.CurrentBulletType == PlayerCombat.BulletType.Poison)
            {
                pool.Add(UpgradeId.BulletFreeze);
                pool.Add(UpgradeId.BulletExplosive);
                pool.Add(UpgradeId.BulletGas);
            }
            else if (playerCombat.CurrentBulletType == PlayerCombat.BulletType.Explosive)
            {
                pool.Add(UpgradeId.BulletFreeze);
                pool.Add(UpgradeId.BulletPoison);
                pool.Add(UpgradeId.BulletGas);
            }
            else if (playerCombat.CurrentBulletType == PlayerCombat.BulletType.Gas)
            {
                pool.Add(UpgradeId.BulletFreeze);
                pool.Add(UpgradeId.BulletPoison);
                pool.Add(UpgradeId.BulletExplosive);
            }
            else
            {
                pool.Add(UpgradeId.BulletFreeze);
                pool.Add(UpgradeId.BulletPoison);
                pool.Add(UpgradeId.BulletExplosive);
                pool.Add(UpgradeId.BulletGas);
            }
        }
        else
        {
            pool.Add(UpgradeId.BulletFreeze);
            pool.Add(UpgradeId.BulletPoison);
            pool.Add(UpgradeId.BulletExplosive);
            pool.Add(UpgradeId.BulletGas);
        }

        bool hasRadiation = FindFirstObjectByType<PlayerRadiationAura>() != null || radiationPickCount > 0;
        if (radiationPickCount < radiationCap)
        {
            pool.Add(hasRadiation ? UpgradeId.RadiationRadius : UpgradeId.Radiation);
        }

        return pool;
    }

    private string GetUpgradeLabel(UpgradeId id)
    {
        return id switch
        {
            UpgradeId.WiderView => "Wider View",
            UpgradeId.WeaponPistol => "Pistol",
            UpgradeId.WeaponShotgun => "Shotgun",
            UpgradeId.WeaponAssaultRifle => "Assault Rifle",
            UpgradeId.LaserSight => "Laser Sight",
            UpgradeId.MoveSpeed => "Move Faster",
            UpgradeId.Accuracy => "Accuracy +",
            UpgradeId.HealthRegen => "Health Regen",
            UpgradeId.MaxHealth => "Max Health +",
            UpgradeId.BulletPierce => "Bullet Pierce +",
            UpgradeId.Radiation => "Radiation Aura",
            UpgradeId.RadiationRadius => "Radiation Radius +",
            UpgradeId.BulletFreeze => "Freeze Bullets",
            UpgradeId.BulletPoison => "Poison Bullets",
            UpgradeId.BulletExplosive => "Explosive Bullets",
            UpgradeId.BulletGas => "Gas Bullets",
            UpgradeId.ProjectileLifetime => projectileLifetimePickCount >= projectileLifetimeCap ? "Projectile Lifetime (MAX)" : "Projectile Lifetime +",
            _ => "Upgrade"
        };
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        score = 0;
        coins = 0;
        UpdateCoinsText();
        nextUpgradeScore = upgradeScoreThreshold;
        pendingUpgrades = 0;
        widerViewPickCount = 0;
        laserSightUnlocked = false;
        accuracyPickCount = 0;
        regenPickCount = 0;
        maxHealthPickCount = 0;
        piercePickCount = 0;
        radiationPickCount = 0;
        radiationRadiusPickCount = 0;
        moveSpeedPickCount = 0;
        projectileLifetimePickCount = 0;
        SceneManager.LoadScene(sceneToReload);
    }

    public void SpawnCoinPickup(Vector3 position, int amount)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coin.name = "Coin";
        coin.transform.position = position + new Vector3(0f, 0.05f, 0f);
        coin.transform.localScale = Vector3.one * 0.35f;

        Collider c = coin.GetComponent<Collider>();
        c.isTrigger = true;

        if (c is SphereCollider sphere)
        {
            sphere.radius = 1.0f;
        }

        CoinPickup pickup = coin.AddComponent<CoinPickup>();
        pickup.SetAmount(amount);

        Rigidbody rb = coin.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = coin.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Renderer r = coin.GetComponent<Renderer>();
        Shader? shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.color = new Color(1f, 0.85f, 0.2f, 1f);
            r.material = mat;
        }

        Destroy(coin, 7f);
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

    private void SetUpgradesText()
    {
        if (upgradesText == null)
        {
            return;
        }

        if (pendingUpgrades <= 0)
        {
            upgradesText.text = "Upgrades: 0";
        }
        else
        {
            upgradesText.text = $"Upgrades: {pendingUpgrades} (press Tab)";
        }
    }

    private void UpdateUpgradeTiersText()
    {
        if (upgradeTiersText == null)
        {
            return;
        }

        string acc = accuracyPickCount >= accuracyCap ? "MAX" : $"{accuracyPickCount}/{accuracyCap}";
        string regen = regenPickCount >= regenCap ? "MAX" : $"{regenPickCount}/{regenCap}";
        string hp = maxHealthPickCount >= maxHealthCap ? "MAX" : $"{maxHealthPickCount}/{maxHealthCap}";
        string pierce = piercePickCount >= pierceCap ? "MAX" : $"{piercePickCount}/{pierceCap}";
        string rad = radiationPickCount >= radiationCap ? "MAX" : $"{radiationPickCount}/{radiationCap}";
        string speed = moveSpeedPickCount >= moveSpeedCap ? "MAX" : $"{moveSpeedPickCount}/{moveSpeedCap}";
        string life = projectileLifetimePickCount >= projectileLifetimeCap ? "MAX" : $"{projectileLifetimePickCount}/{projectileLifetimeCap}";

        string bulletType = playerCombat != null ? playerCombat.CurrentBulletType.ToString() : "-";
        upgradeTiersText.text =
            $"Accuracy: {acc}\n" +
            $"Regen: {regen}\n" +
            $"Max HP: {hp}\n" +
            $"Pierce: {pierce}\n" +
            $"Radiation: {rad}\n" +
            $"Speed: {speed}\n" +
            $"Lifetime: {life}\n" +
            $"Bullet: {bulletType}";
    }

    private void UpdateCoinsText()
    {
        if (coinsText == null)
        {
            return;
        }

        coinsText.text = $"Coins: {coins}";
    }

    private void EnsureUi()
    {
        if (scoreText != null && weaponText != null && upgradesText != null && coinsText != null && upgradeTiersText != null && playerHealthFill != null && playerHealthText != null && gameOverPanel != null && upgradePanel != null)
        {
            return;
        }

        Canvas canvas = FindOrCreateHudCanvas();

        EnsureEventSystem();

        if (hudRoot == null)
        {
            hudRoot = new GameObject("HUDRoot", typeof(RectTransform));
            hudRoot.transform.SetParent(canvas.transform, false);
            
            RectTransform hudRect = hudRoot.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;
        }

        if (scoreText == null)
        {
            GameObject scoreObject = new GameObject("ScoreText", typeof(RectTransform), typeof(TextMeshProUGUI));
            scoreObject.transform.SetParent(hudRoot.transform, false);

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
            weaponObject.transform.SetParent(hudRoot.transform, false);

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

        if (coinsText == null)
        {
            GameObject coinsObject = new GameObject("CoinsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            coinsObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = coinsObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -96f);
            rect.sizeDelta = new Vector2(500f, 40f);

            TextMeshProUGUI text = coinsObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 20;
            text.color = new Color(1f, 0.9f, 0.4f, 0.9f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;

            coinsText = text;
            UpdateCoinsText();
        }

        if (upgradesText == null)
        {
            GameObject upgradesObject = new GameObject("UpgradesText", typeof(RectTransform), typeof(TextMeshProUGUI));
            upgradesObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = upgradesObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -72f);
            rect.sizeDelta = new Vector2(700f, 40f);

            TextMeshProUGUI text = upgradesObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 20;
            text.color = new Color(1f, 1f, 1f, 0.8f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            upgradesText = text;
        }

        if (upgradeTiersText == null)
        {
            GameObject tiersObject = new GameObject("UpgradeTiersText", typeof(RectTransform), typeof(TextMeshProUGUI));
            tiersObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = tiersObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = new Vector2(520f, 220f);

            TextMeshProUGUI text = tiersObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 18;
            text.color = new Color(1f, 1f, 1f, 0.85f);
            text.alignment = TextAlignmentOptions.TopRight;
            text.raycastTarget = false;

            upgradeTiersText = text;
            UpdateUpgradeTiersText();
        }

        if (playerHealthFill == null)
        {
            GameObject hpRoot = new GameObject("PlayerHealth", typeof(RectTransform));
            hpRoot.transform.SetParent(hudRoot.transform, false);

            RectTransform rootRect = hpRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -16f);
            rootRect.sizeDelta = new Vector2(420f, 22f);

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

            GameObject hpTextObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            hpTextObject.transform.SetParent(hpRoot.transform, false);

            RectTransform hpTextRect = hpTextObject.GetComponent<RectTransform>();
            hpTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            hpTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            hpTextRect.pivot = new Vector2(0.5f, 0.5f);
            hpTextRect.anchoredPosition = Vector2.zero;
            hpTextRect.sizeDelta = new Vector2(420f, 26f);

            TextMeshProUGUI hpText = hpTextObject.GetComponent<TextMeshProUGUI>();
            hpText.fontSize = 18;
            hpText.color = Color.white;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.raycastTarget = false;
            playerHealthText = hpText;
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

            if (playerHealthText != null)
            {
                playerHealthText.text = "- / -";
            }

            return;
        }

        playerHealthFill.fillAmount = playerHealth.Normalized;

        if (playerHealthText != null)
        {
            playerHealthText.text = $"{playerHealth.CurrentHealth} / {playerHealth.MaxHealth}";
        }

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
