using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Ресурсы
    private int ore = 0;
    private int gold = 0;

    // Прокачка
    private int strengthLevel = 0;
    private int tradeLevel = 0;
    private int pickaxeCount = 1;
    private const int MAX_PICKAXES = 5;

    // Текущая стоимость прокачки
    private int currentStrengthCost;
    private int currentTradeCost;
    private int currentPickaxeCost;

    // Система уровней игрока
    private int playerLevel = 0;
    private int currentExp = 0;
    private int requiredExpForNextLevel = 100;

    // Таблица опыта для уровней
    private int[] expRequirements = new int[]
    {
        0,      // 0 уровень (не используется)
        100,    // 0 -> 1
        500,    // 1 -> 2
        1000,   // 2 -> 3
        2500,   // 3 -> 4
        5000,   // 4 -> 5
        7500,   // 5 -> 6
        10000,  // 6 -> 7
        15000,  // 7 -> 8
        20000,  // 8 -> 9
        50000   // 9 -> 10
    };
    private const int MAX_LEVEL = 10;

    // Туториал
    private enum TutorialStep
    {
        NotStarted,
        FirstHit,
        OreCollected,
        SoldOre,
        Completed
    }

    private TutorialStep currentTutorialStep = TutorialStep.NotStarted;
    private const string TUTORIAL_KEY = "TutorialStep";
    private bool tutorialPanelActive = false;

    // UI ссылки на счётчики
    public TMP_Text oreText;
    public TMP_Text goldText;

    // UI ссылки на кнопки и тексты
    public Button strengthButton;
    public TMP_Text strengthCostText;
    public TMP_Text strengthLevelText;

    public Button tradeButton;
    public TMP_Text tradeCostText;
    public TMP_Text tradeLevelText;

    public Button pickaxeButton;
    public TMP_Text pickaxeCostText;
    public TMP_Text pickaxeCountText;

    // UI для системы уровней
    [Header("Система уровней")]
    public TMP_Text playerLevelText;
    public Slider expProgressBar;
    public TMP_Text expText;
    public Image rockImage;
    public Sprite[] rockSprites; // Спрайты камня для каждого уровня (индекс = уровень)
    public GameObject levelUpEffect; // Эффект при повышении уровня (опционально)

    // Префаб для всплывающих текстов
    public GameObject floatingTextPrefab;
    public Transform floatingTextParent;

    // ==================== ТУТОРИАЛ ====================
    [Header("Туториал")]
    public GameObject tutorialPanel;
    public TMP_Text tutorialText;
    public Button sellButton;

    [Header("Тексты туториала")]
    public string tutorialFirstHitText = "Нажмите на камень,\nчтобы добывать руду!";
    public string tutorialSellText = "У вас 10 руды!\nНажмите на счётчик руды,\nчтобы продать и получить золото!";
    public string tutorialUpgradeText = "Отлично!\nТеперь нажмите на кнопку прокачки,\nчтобы улучшить кирку!";
    public string tutorialCollectMoreText = "Отлично! Добывайте руду,\nпока не накопится 10 штук.";
    public string tutorialWaitForGoldText = "Отлично! Теперь накопите достаточно золота\nдля покупки улучшения!";

    // ==================== НАСТРОЙКИ ВСПЛЫВАЮЩИХ ТЕКСТОВ ====================
    [Header("Настройки всплывающих текстов")]
    [SerializeField] private float floatingTextLifetime = 2f;

    // ==================== СИСТЕМА КИРОК ====================
    [Header("Система кирок")]
    public Transform pickaxeContainer;
    public GameObject pickaxePrefab;
    public Transform[] pickaxePoints;

    private List<GameObject> activePickaxes = new List<GameObject>();

    // ==================== НАСТРОЙКИ АДАПТАЦИИ ====================
    [Header("Настройки адаптации под экран")]
    [SerializeField] private bool adaptToScreen = true;
    [SerializeField] private float referenceWidth = 1080f;
    [SerializeField] private float referenceHeight = 1920f;
    [SerializeField] private float minButtonSize = 120f;

    private CanvasScaler canvasScaler;
    private bool tutorialMessageShown = false;
    private bool upgradeTutorialTriggered = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        AdaptUIForScreen();
        LoadGame();
        LoadTutorialState();
        SetupTutorial();
        UpdateCosts();
        UpdateUI();
        UpdateButtonsInteractable();
        UpdatePickaxesDisplay();
        UpdateExpUI();
        UpdateRockSprite();
    }

    private void Update()
    {
        // Проверяем, хватает ли золота для прокачки во время этапа SoldOre
        if (currentTutorialStep == TutorialStep.SoldOre && !upgradeTutorialTriggered)
        {
            if (gold >= currentStrengthCost || gold >= currentTradeCost || gold >= currentPickaxeCost)
            {
                upgradeTutorialTriggered = true;
                UpdateTutorialText(tutorialUpgradeText);
                HighlightUpgradeButtons(true);
                UpdateButtonsByTutorialStep();
            }
        }
    }

    // ==================== СИСТЕМА УРОВНЕЙ ====================

    private void AddExp(int amount)
    {
        if (playerLevel >= MAX_LEVEL) return;

        currentExp += amount;

        // Проверяем, нужно ли повысить уровень
        while (currentExp >= requiredExpForNextLevel && playerLevel < MAX_LEVEL)
        {
            currentExp -= requiredExpForNextLevel;
            playerLevel++;

            // Обновляем требуемый опыт для следующего уровня
            if (playerLevel < MAX_LEVEL)
            {
                requiredExpForNextLevel = expRequirements[playerLevel + 1];
            }

            // Эффект повышения уровня
            OnLevelUp();
        }

        UpdateExpUI();
        UpdateRockSprite();
        SaveGame();
    }

    private void UpdateExpUI()
    {
        if (playerLevelText != null)
            playerLevelText.text = $"Ур. {playerLevel}";

        if (expProgressBar != null)
        {
            expProgressBar.maxValue = requiredExpForNextLevel;
            expProgressBar.value = currentExp;
        }

        if (expText != null)
            expText.text = $"{currentExp}/{requiredExpForNextLevel}";
    }

    private void UpdateRockSprite()
    {
        if (rockImage != null && rockSprites != null && rockSprites.Length > 0)
        {
            int spriteIndex = Mathf.Min(playerLevel, rockSprites.Length - 1);
            rockImage.sprite = rockSprites[spriteIndex];
        }
    }

    private void OnLevelUp()
    {
        Debug.Log($"Уровень повышен до {playerLevel}!");

        // Показываем всплывающее сообщение
        ShowFloatingText($"УРОВЕНЬ {playerLevel} ДОСТИГНУТ!", Color.yellow);

        // Эффект повышения уровня
        if (levelUpEffect != null)
        {
            GameObject effect = Instantiate(levelUpEffect, rockImage.transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }

        // Дополнительная награда за уровень (опционально)
        // Например, даём золото за каждый уровень
        int levelUpReward = playerLevel * 10;
        gold += levelUpReward;
        ShowFloatingText($"+{levelUpReward} золота за уровень!", Color.green);
        UpdateUI();
    }

    public int GetPlayerLevel()
    {
        return playerLevel;
    }

    // ==================== ТУТОРИАЛ ====================

    private void SetupTutorial()
    {
        if (currentTutorialStep != TutorialStep.Completed)
        {
            ShowTutorialPanel();

            switch (currentTutorialStep)
            {
                case TutorialStep.NotStarted:
                    currentTutorialStep = TutorialStep.FirstHit;
                    UpdateTutorialText(tutorialFirstHitText);
                    break;

                case TutorialStep.FirstHit:
                    UpdateTutorialText(tutorialFirstHitText);
                    break;

                case TutorialStep.OreCollected:
                    UpdateTutorialText(tutorialSellText);
                    UpdateSellButtonState();
                    break;

                case TutorialStep.SoldOre:
                    UpdateTutorialText(tutorialWaitForGoldText);
                    break;
            }

            UpdateButtonsByTutorialStep();
        }
        else
        {
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
            tutorialPanelActive = false;
        }
    }

    private void ShowTutorialPanel()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialPanelActive = true;

            if (Application.isMobilePlatform && tutorialText != null)
            {
                tutorialText.fontSize = Mathf.RoundToInt(tutorialText.fontSize * 1.2f);
            }
        }
    }

    private void UpdateTutorialText(string text)
    {
        if (tutorialText != null)
        {
            tutorialText.text = text;
        }
    }

    private void UpdateSellButtonState()
    {
        if (sellButton != null)
        {
            bool canSell = ore >= 10;
            sellButton.interactable = canSell;

            if (canSell && currentTutorialStep == TutorialStep.OreCollected)
            {
                HighlightSellButton(true);
            }
            else
            {
                HighlightSellButton(false);
            }
        }
    }

    private void HighlightSellButton(bool highlight)
    {
        if (sellButton != null)
        {
            ColorBlock colors = sellButton.colors;
            if (highlight)
            {
                colors.normalColor = Color.yellow;
                colors.selectedColor = Color.yellow;
                StartCoroutine(PulseButton(sellButton));
            }
            else
            {
                colors.normalColor = Color.white;
                colors.selectedColor = Color.white;
                StopCoroutine(PulseButton(sellButton));
            }
            sellButton.colors = colors;
        }
    }

    private void HighlightUpgradeButtons(bool highlight)
    {
        Color highlightColor = new Color(1f, 0.8f, 0.2f);

        if (strengthButton != null)
        {
            ColorBlock colors = strengthButton.colors;
            if (highlight)
            {
                colors.normalColor = highlightColor;
                StartCoroutine(PulseButton(strengthButton));
            }
            else
            {
                colors.normalColor = Color.white;
                StopCoroutine(PulseButton(strengthButton));
            }
            strengthButton.colors = colors;
        }

        if (tradeButton != null)
        {
            ColorBlock colors = tradeButton.colors;
            if (highlight)
            {
                colors.normalColor = highlightColor;
            }
            else
            {
                colors.normalColor = Color.white;
            }
            tradeButton.colors = colors;
        }

        if (pickaxeButton != null)
        {
            ColorBlock colors = pickaxeButton.colors;
            if (highlight)
            {
                colors.normalColor = highlightColor;
            }
            else
            {
                colors.normalColor = Color.white;
            }
            pickaxeButton.colors = colors;
        }
    }

    private System.Collections.IEnumerator PulseButton(Button button)
    {
        if (button == null) yield break;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector3 originalScale = rect.localScale;

        while (tutorialPanelActive)
        {
            float time = 0;
            while (time < 1f)
            {
                if (button == null) yield break;
                float scale = 1 + Mathf.Sin(time * Mathf.PI * 2) * 0.1f;
                rect.localScale = originalScale * scale;
                time += Time.deltaTime * 2f;
                yield return null;
            }
            yield return null;
        }

        rect.localScale = originalScale;
    }

    private void UpdateButtonsByTutorialStep()
    {
        bool upgradesEnabled = (currentTutorialStep == TutorialStep.SoldOre || currentTutorialStep == TutorialStep.Completed);

        if (strengthButton != null)
            strengthButton.interactable = upgradesEnabled && (gold >= currentStrengthCost);

        if (tradeButton != null)
            tradeButton.interactable = upgradesEnabled && (gold >= currentTradeCost);

        if (pickaxeButton != null)
        {
            bool canAddPickaxe = pickaxeCount < MAX_PICKAXES;
            pickaxeButton.interactable = upgradesEnabled && (gold >= currentPickaxeCost) && canAddPickaxe;
        }
    }

    public void OnFirstHit()
    {
        if (currentTutorialStep == TutorialStep.FirstHit)
        {
            currentTutorialStep = TutorialStep.OreCollected;
            SaveTutorialState();

            if (ore >= 10)
            {
                UpdateTutorialText(tutorialSellText);
                UpdateSellButtonState();
            }
            else
            {
                UpdateTutorialText(tutorialCollectMoreText);
            }
        }
    }

    public void CheckOreForTutorial()
    {
        if (currentTutorialStep == TutorialStep.OreCollected && ore >= 10 && !tutorialMessageShown)
        {
            UpdateTutorialText(tutorialSellText);
            tutorialMessageShown = true;
            UpdateSellButtonState();
        }
    }

    public void OnFirstSale()
    {
        if (currentTutorialStep == TutorialStep.OreCollected)
        {
            currentTutorialStep = TutorialStep.SoldOre;
            SaveTutorialState();

            UpdateTutorialText(tutorialWaitForGoldText);
            HighlightSellButton(false);
            UpdateButtonsByTutorialStep();
        }
    }

    public void OnFirstUpgrade()
    {
        if (currentTutorialStep == TutorialStep.SoldOre)
        {
            currentTutorialStep = TutorialStep.Completed;
            SaveTutorialState();

            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
            tutorialPanelActive = false;

            HighlightUpgradeButtons(false);
            UpdateButtonsInteractable();

            Debug.Log("Туториал полностью завершён!");
        }
    }

    private void SaveTutorialState()
    {
        PlayerPrefs.SetInt(TUTORIAL_KEY, (int)currentTutorialStep);
        PlayerPrefs.Save();
    }

    private void LoadTutorialState()
    {
        if (PlayerPrefs.HasKey(TUTORIAL_KEY))
        {
            currentTutorialStep = (TutorialStep)PlayerPrefs.GetInt(TUTORIAL_KEY);
        }
        else
        {
            currentTutorialStep = TutorialStep.NotStarted;
        }
    }

    public void ResetTutorial()
    {
        currentTutorialStep = TutorialStep.NotStarted;
        tutorialMessageShown = false;
        upgradeTutorialTriggered = false;
        SaveTutorialState();
        SetupTutorial();
    }

    // ==================== АДАПТАЦИЯ ПОД ЭКРАНЫ ====================

    private void AdaptUIForScreen()
    {
        if (!adaptToScreen) return;

        canvasScaler = GetComponentInParent<CanvasScaler>();
        if (canvasScaler == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvasScaler = canvas.GetComponent<CanvasScaler>();
        }

        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
        }

        if (tutorialPanel != null)
        {
            RectTransform panelRect = tutorialPanel.GetComponent<RectTransform>();
            if (panelRect != null && Application.isMobilePlatform)
            {
                panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x * 1.2f, panelRect.sizeDelta.y * 1.2f);
            }
        }

        if (Application.isMobilePlatform)
        {
            IncreaseFontSizes();
            IncreaseButtonSizes();
            floatingTextLifetime = 2.5f;
        }

        float aspectRatio = (float)Screen.width / Screen.height;
        AdaptForAspectRatio(aspectRatio);
    }

    private void IncreaseFontSizes()
    {
        if (oreText != null) oreText.fontSize = Mathf.RoundToInt(oreText.fontSize * 1.3f);
        if (goldText != null) goldText.fontSize = Mathf.RoundToInt(goldText.fontSize * 1.3f);
        if (strengthCostText != null) strengthCostText.fontSize = Mathf.RoundToInt(strengthCostText.fontSize * 1.2f);
        if (tradeCostText != null) tradeCostText.fontSize = Mathf.RoundToInt(tradeCostText.fontSize * 1.2f);
        if (pickaxeCostText != null) pickaxeCostText.fontSize = Mathf.RoundToInt(pickaxeCostText.fontSize * 1.2f);
        if (strengthLevelText != null) strengthLevelText.fontSize = Mathf.RoundToInt(strengthLevelText.fontSize * 1.2f);
        if (tradeLevelText != null) tradeLevelText.fontSize = Mathf.RoundToInt(tradeLevelText.fontSize * 1.2f);
        if (pickaxeCountText != null) pickaxeCountText.fontSize = Mathf.RoundToInt(pickaxeCountText.fontSize * 1.2f);
        if (playerLevelText != null) playerLevelText.fontSize = Mathf.RoundToInt(playerLevelText.fontSize * 1.3f);
        if (expText != null) expText.fontSize = Mathf.RoundToInt(expText.fontSize * 1.2f);
    }

    private void IncreaseButtonSizes()
    {
        RectTransform rect;

        if (strengthButton != null)
        {
            rect = strengthButton.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, minButtonSize), Mathf.Max(rect.sizeDelta.y, minButtonSize));
        }

        if (tradeButton != null)
        {
            rect = tradeButton.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, minButtonSize), Mathf.Max(rect.sizeDelta.y, minButtonSize));
        }

        if (pickaxeButton != null)
        {
            rect = pickaxeButton.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, minButtonSize), Mathf.Max(rect.sizeDelta.y, minButtonSize));
        }
    }

    private void AdaptForAspectRatio(float aspectRatio)
    {
        if (aspectRatio > 0.6f && pickaxeContainer != null)
        {
            RectTransform containerRect = pickaxeContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                Vector2 anchoredPos = containerRect.anchoredPosition;
                anchoredPos.y = anchoredPos.y - 50f;
                containerRect.anchoredPosition = anchoredPos;
            }
        }
    }

    private void UpdateCosts()
    {
        float strengthRaw = 10 * Mathf.Pow(1.5f, strengthLevel);
        currentStrengthCost = Mathf.RoundToInt(strengthRaw / 5f) * 5;
        if (currentStrengthCost < 5) currentStrengthCost = 5;

        float tradeRaw = 20 * Mathf.Pow(1.5f, tradeLevel);
        currentTradeCost = Mathf.RoundToInt(tradeRaw / 5f) * 5;
        if (currentTradeCost < 5) currentTradeCost = 5;

        float pickaxeRaw = 50 * Mathf.Pow(1.5f, pickaxeCount - 1);
        currentPickaxeCost = Mathf.RoundToInt(pickaxeRaw / 5f) * 5;
        if (currentPickaxeCost < 5) currentPickaxeCost = 5;
    }

    public void HitRock()
    {
        int gain = 1 + strengthLevel;
        ore += gain;

        // Добавляем опыт за добытую руду
        AddExp(gain);

        UpdateUI();
        ShowFloatingText($"+{gain}", Color.white);

        if (currentTutorialStep == TutorialStep.FirstHit)
        {
            OnFirstHit();
        }

        CheckOreForTutorial();
        UpdateSellButtonState();
        SaveGame();
    }

    public void SellOre()
    {
        if (ore < 10)
        {
            ShowFloatingText("Нужно 10 руды!", Color.red);
            return;
        }

        int goldEarned = 1 + tradeLevel;
        ore -= 10;
        gold += goldEarned;
        UpdateUI();
        UpdateButtonsInteractable();

        ShowFloatingText($"+{goldEarned} золота", Color.yellow);

        if (currentTutorialStep == TutorialStep.OreCollected)
        {
            OnFirstSale();
        }

        UpdateSellButtonState();
        SaveGame();
    }

    public void UpgradeStrength()
    {
        if (gold >= currentStrengthCost)
        {
            gold -= currentStrengthCost;
            strengthLevel++;

            UpdateCosts();
            UpdateUI();
            UpdateButtonsInteractable();

            ShowFloatingText($"Сила удара +1! (теперь +{1 + strengthLevel} руды)", Color.green);

            if (currentTutorialStep == TutorialStep.SoldOre)
            {
                OnFirstUpgrade();
            }

            SaveGame();
        }
        else
        {
            ShowFloatingText($"Нужно {currentStrengthCost} золота!", Color.red);
        }
    }

    public void UpgradeTrade()
    {
        if (gold >= currentTradeCost)
        {
            gold -= currentTradeCost;
            tradeLevel++;

            UpdateCosts();
            UpdateUI();
            UpdateButtonsInteractable();

            ShowFloatingText($"Обмен улучшен! (10 руды = {1 + tradeLevel} золота)", Color.green);

            if (currentTutorialStep == TutorialStep.SoldOre)
            {
                OnFirstUpgrade();
            }

            SaveGame();
        }
        else
        {
            ShowFloatingText($"Нужно {currentTradeCost} золота!", Color.red);
        }
    }

    public void AddPickaxe()
    {
        if (pickaxeCount >= MAX_PICKAXES)
        {
            ShowFloatingText($"Достигнут максимум кирок! (макс. {MAX_PICKAXES})", Color.red);
            return;
        }

        if (gold >= currentPickaxeCost)
        {
            gold -= currentPickaxeCost;
            pickaxeCount++;
            strengthLevel++;

            UpdateCosts();
            UpdateUI();
            UpdateButtonsInteractable();
            UpdatePickaxesDisplay();

            ShowFloatingText($"Новая кирка! +1 к силе удара (теперь +{1 + strengthLevel})", Color.cyan);

            if (currentTutorialStep == TutorialStep.SoldOre)
            {
                OnFirstUpgrade();
            }

            SaveGame();
        }
        else
        {
            ShowFloatingText($"Нужно {currentPickaxeCost} золота!", Color.red);
        }
    }

    private void UpdateButtonsInteractable()
    {
        if (currentTutorialStep != TutorialStep.Completed && currentTutorialStep != TutorialStep.SoldOre)
        {
            if (strengthButton != null) strengthButton.interactable = false;
            if (tradeButton != null) tradeButton.interactable = false;
            if (pickaxeButton != null) pickaxeButton.interactable = false;
            return;
        }

        strengthButton.interactable = gold >= currentStrengthCost;
        tradeButton.interactable = gold >= currentTradeCost;
        bool canAddPickaxe = pickaxeCount < MAX_PICKAXES;
        pickaxeButton.interactable = gold >= currentPickaxeCost && canAddPickaxe;
    }

    private void ShowFloatingText(string message, Color color)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("floatingTextPrefab не назначен в инспекторе!");
            return;
        }

        if (floatingTextParent == null)
        {
            Debug.LogError("floatingTextParent не назначен в инспекторе!");
            return;
        }

        GameObject go = Instantiate(floatingTextPrefab, floatingTextParent);
        TMP_Text text = go.GetComponent<TMP_Text>();

        if (text != null)
        {
            text.text = message;
            text.color = color;

            if (Application.isMobilePlatform)
            {
                text.fontSize = Mathf.RoundToInt(text.fontSize * 1.3f);
            }
        }
        else
        {
            Debug.LogError("В префабе FloatingText нет компонента TMP_Text!");
        }

        Destroy(go, floatingTextLifetime);
    }

    private void UpdateUI()
    {
        if (oreText != null)
            oreText.text = $"{ore} руды";

        if (goldText != null)
            goldText.text = $"{gold} золота";

        if (strengthCostText != null)
            strengthCostText.text = $"{currentStrengthCost} золота";

        if (tradeCostText != null)
            tradeCostText.text = $"{currentTradeCost} золота";

        if (pickaxeCostText != null)
            pickaxeCostText.text = $"{currentPickaxeCost} золота";

        if (strengthLevelText != null)
            strengthLevelText.text = $"Ур. {strengthLevel}";

        if (tradeLevelText != null)
            tradeLevelText.text = $"Ур. {tradeLevel}";

        if (pickaxeCountText != null)
            pickaxeCountText.text = $"{pickaxeCount}/{MAX_PICKAXES}";
    }

    // ==================== ОТОБРАЖЕНИЕ КИРОК ====================

    private void UpdatePickaxesDisplay()
    {
        foreach (GameObject pickaxe in activePickaxes)
        {
            if (pickaxe != null)
                Destroy(pickaxe);
        }
        activePickaxes.Clear();

        for (int i = 0; i < pickaxeCount && i < pickaxePoints.Length; i++)
        {
            if (pickaxePoints[i] != null)
            {
                GameObject newPickaxe = Instantiate(pickaxePrefab, pickaxeContainer);
                newPickaxe.transform.position = pickaxePoints[i].position;
                newPickaxe.transform.rotation = pickaxePoints[i].rotation;
                activePickaxes.Add(newPickaxe);

                if (Application.isMobilePlatform)
                {
                    newPickaxe.transform.localScale = Vector3.one * 1.2f;
                }
            }
            else
            {
                Debug.LogWarning($"Точка {i} не назначена!");
            }
        }
    }

    public List<GameObject> GetActivePickaxes()
    {
        return activePickaxes;
    }

    public int GetPickaxeCount()
    {
        return pickaxeCount;
    }

    // ==================== СОХРАНЕНИЕ И ЗАГРУЗКА ====================

    private void SaveGame()
    {
        PlayerPrefs.SetInt("Ore", ore);
        PlayerPrefs.SetInt("Gold", gold);
        PlayerPrefs.SetInt("StrengthLevel", strengthLevel);
        PlayerPrefs.SetInt("TradeLevel", tradeLevel);
        PlayerPrefs.SetInt("PickaxeCount", pickaxeCount);
        PlayerPrefs.SetInt("PlayerLevel", playerLevel);
        PlayerPrefs.SetInt("CurrentExp", currentExp);
        PlayerPrefs.Save();
    }

    private void LoadGame()
    {
        if (PlayerPrefs.HasKey("Ore"))
        {
            ore = PlayerPrefs.GetInt("Ore");
            gold = PlayerPrefs.GetInt("Gold");
            strengthLevel = PlayerPrefs.GetInt("StrengthLevel");
            tradeLevel = PlayerPrefs.GetInt("TradeLevel");
            pickaxeCount = PlayerPrefs.GetInt("PickaxeCount");
            playerLevel = PlayerPrefs.GetInt("PlayerLevel");
            currentExp = PlayerPrefs.GetInt("CurrentExp");

            if (pickaxeCount > MAX_PICKAXES)
                pickaxeCount = MAX_PICKAXES;

            // Устанавливаем требуемый опыт для следующего уровня
            if (playerLevel < MAX_LEVEL)
            {
                requiredExpForNextLevel = expRequirements[playerLevel + 1];
            }
            else
            {
                requiredExpForNextLevel = expRequirements[MAX_LEVEL];
            }
        }
        else
        {
            Debug.Log("Нет сохранений, начинаем новую игру");
            playerLevel = 0;
            currentExp = 0;
            requiredExpForNextLevel = expRequirements[1];
        }
    }

    public void ResetGame()
    {
        ore = 0;
        gold = 0;
        strengthLevel = 0;
        tradeLevel = 0;
        pickaxeCount = 1;
        playerLevel = 0;
        currentExp = 0;
        requiredExpForNextLevel = expRequirements[1];

        UpdateCosts();
        UpdateUI();
        UpdateButtonsInteractable();
        UpdatePickaxesDisplay();
        UpdateExpUI();
        UpdateRockSprite();
        SaveGame();

        ShowFloatingText("Прогресс сброшен!", Color.red);
    }

    public void ResetGameWithTutorial()
    {
        ResetGame();
        ResetTutorial();
    }

    public void ManualSave()
    {
        SaveGame();
        ShowFloatingText("Игра сохранена!", Color.green);
    }

    public void ManualLoad()
    {
        LoadGame();
        LoadTutorialState();
        SetupTutorial();
        UpdateCosts();
        UpdateUI();
        UpdateButtonsInteractable();
        UpdatePickaxesDisplay();
        UpdateExpUI();
        UpdateRockSprite();
        ShowFloatingText("Игра загружена!", Color.green);
    }
}