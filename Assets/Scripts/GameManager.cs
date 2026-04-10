using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Ресурсы
    private int ore = 0;
    private int gold = 15; // Стартовое золото увеличено до 15

    // Прокачка
    private int strengthLevel = 0;
    private int tradeLevel = 0;
    private int pickaxeCount = 1;
    private const int MAX_PICKAXES = 5; // Максимальное количество кирок

    // Текущая стоимость прокачки
    private int currentStrengthCost;
    private int currentTradeCost;
    private int currentPickaxeCost;

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

    // Префаб для всплывающих текстов
    public GameObject floatingTextPrefab;
    public Transform floatingTextParent;

    // ==================== НАСТРОЙКИ ВСПЛЫВАЮЩИХ ТЕКСТОВ ====================
    [Header("Настройки всплывающих текстов")]
    [SerializeField] private float floatingTextLifetime = 2f; // Время жизни текста (секунды)
    [SerializeField] private float floatingTextSpeed = 50f;   // Скорость подъёма

    // ==================== СИСТЕМА КИРОК ====================
    [Header("Система кирок")]
    public Transform pickaxeContainer;     // Контейнер для кирок
    public GameObject pickaxePrefab;       // Префаб кирки
    public Transform[] pickaxePoints;      // Точки для расположения кирок (настраиваешь вручную)

    private List<GameObject> activePickaxes = new List<GameObject>(); // Активные кирки

    // ==================== НАСТРОЙКИ АДАПТАЦИИ ====================
    [Header("Настройки адаптации под экран")]
    [SerializeField] private bool adaptToScreen = true;
    [SerializeField] private float referenceWidth = 1080f; // Эталонная ширина (1080p)
    [SerializeField] private float referenceHeight = 1920f; // Эталонная высота
    [SerializeField] private float minButtonSize = 120f; // Минимальный размер кнопки на мобилках

    private CanvasScaler canvasScaler;
    private float currentScale = 1f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Адаптируем интерфейс под экран
        AdaptUIForScreen();

        LoadGame();
        UpdateCosts();
        UpdateUI();
        UpdateButtonsInteractable();
        UpdatePickaxesDisplay();
    }

    // ==================== АДАПТАЦИЯ ПОД РАЗНЫЕ ЭКРАНЫ ====================

    private void AdaptUIForScreen()
    {
        if (!adaptToScreen) return;

        // Находим Canvas Scaler
        canvasScaler = GetComponentInParent<CanvasScaler>();
        if (canvasScaler == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvasScaler = canvas.GetComponent<CanvasScaler>();
        }

        // Настройка Canvas Scaler для мобильных устройств
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f; // Баланс между шириной и высотой
        }

        // Проверяем, мобильное ли устройство
        if (Application.isMobilePlatform)
        {
            // Увеличиваем шрифты на мобильных устройствах
            IncreaseFontSizes();

            // Увеличиваем размеры кнопок
            IncreaseButtonSizes();

            // Увеличиваем время жизни текста
            floatingTextLifetime = 2.5f;

            Debug.Log("Адаптация под мобильное устройство выполнена");
        }

        // Дополнительная адаптация под соотношение сторон
        float aspectRatio = (float)Screen.width / Screen.height;
        AdaptForAspectRatio(aspectRatio);
    }

    private void IncreaseFontSizes()
    {
        // Увеличиваем шрифты для лучшей читаемости на телефонах
        if (oreText != null)
            oreText.fontSize = Mathf.RoundToInt(oreText.fontSize * 1.3f);

        if (goldText != null)
            goldText.fontSize = Mathf.RoundToInt(goldText.fontSize * 1.3f);

        if (strengthCostText != null)
            strengthCostText.fontSize = Mathf.RoundToInt(strengthCostText.fontSize * 1.2f);

        if (tradeCostText != null)
            tradeCostText.fontSize = Mathf.RoundToInt(tradeCostText.fontSize * 1.2f);

        if (pickaxeCostText != null)
            pickaxeCostText.fontSize = Mathf.RoundToInt(pickaxeCostText.fontSize * 1.2f);

        if (strengthLevelText != null)
            strengthLevelText.fontSize = Mathf.RoundToInt(strengthLevelText.fontSize * 1.2f);

        if (tradeLevelText != null)
            tradeLevelText.fontSize = Mathf.RoundToInt(tradeLevelText.fontSize * 1.2f);

        if (pickaxeCountText != null)
            pickaxeCountText.fontSize = Mathf.RoundToInt(pickaxeCountText.fontSize * 1.2f);
    }

    private void IncreaseButtonSizes()
    {
        // Увеличиваем размеры кнопок для удобного нажатия пальцем
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
        // Адаптация под разные соотношения сторон
        if (aspectRatio > 0.6f) // Широкий экран (например, 19.5:9)
        {
            // Немного увеличиваем отступы для широких экранов
            if (pickaxeContainer != null)
            {
                RectTransform containerRect = pickaxeContainer.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    Vector2 anchoredPos = containerRect.anchoredPosition;
                    anchoredPos.y = anchoredPos.y - 50f; // Смещаем кирки ниже
                    containerRect.anchoredPosition = anchoredPos;
                }
            }
        }
    }

    // Публичный метод для получения масштаба (для других скриптов)
    public float GetScreenScale()
    {
        if (canvasScaler != null)
        {
            return canvasScaler.transform.localScale.x;
        }
        return 1f;
    }

    // Метод для адаптации позиций кирок под экран
    public void RepositionPickaxes()
    {
        if (!Application.isMobilePlatform) return;

        for (int i = 0; i < activePickaxes.Count; i++)
        {
            if (activePickaxes[i] != null && i < pickaxePoints.Length && pickaxePoints[i] != null)
            {
                activePickaxes[i].transform.position = pickaxePoints[i].position;
                activePickaxes[i].transform.rotation = pickaxePoints[i].rotation;
            }
        }
    }

    private void UpdateCosts()
    {
        // НОВЫЙ БАЛАНС: старт 10, 20, 50 с множителем 1.5
        // Сила удара: старт 10, множитель 1.5
        float strengthRaw = 10 * Mathf.Pow(1.5f, strengthLevel);
        currentStrengthCost = Mathf.RoundToInt(strengthRaw / 5f) * 5;
        if (currentStrengthCost < 5) currentStrengthCost = 5;

        // Обмен: старт 20, множитель 1.5
        float tradeRaw = 20 * Mathf.Pow(1.5f, tradeLevel);
        currentTradeCost = Mathf.RoundToInt(tradeRaw / 5f) * 5;
        if (currentTradeCost < 5) currentTradeCost = 5;

        // Кирка: старт 50, множитель 1.5
        float pickaxeRaw = 50 * Mathf.Pow(1.5f, pickaxeCount - 1);
        currentPickaxeCost = Mathf.RoundToInt(pickaxeRaw / 5f) * 5;
        if (currentPickaxeCost < 5) currentPickaxeCost = 5;
    }

    public void HitRock()
    {
        int gain = 1 + strengthLevel;
        ore += gain;
        UpdateUI();
        ShowFloatingText($"+{gain}", Color.white);
        SaveGame();
        Debug.Log($"УДАР: Сила уровня {strengthLevel}, Добыто +{gain}, Всего руды: {ore}");
    }

    public void SellOre()
    {
        if (ore < 20)
        {
            ShowFloatingText("Нужно 20 руды!", Color.red);
            return;
        }

        int goldEarned = 1 + tradeLevel;
        ore -= 20;
        gold += goldEarned;
        UpdateUI();
        UpdateButtonsInteractable();

        ShowFloatingText($"+{goldEarned} золота", Color.yellow);
        SaveGame();
    }

    public void UpgradeStrength()
    {
        if (gold >= currentStrengthCost)
        {
            gold -= currentStrengthCost;
            strengthLevel++;

            Debug.Log($"ПРОКАЧКА: Сила удара теперь уровень {strengthLevel} (добыча +{1 + strengthLevel})");

            UpdateCosts();
            UpdateUI();
            UpdateButtonsInteractable();

            ShowFloatingText($"Сила удара +1! (теперь +{1 + strengthLevel} руды)", Color.green);
            SaveGame();
        }
        else
        {
            ShowFloatingText($"Не хватает золота! Нужно {currentStrengthCost}", Color.red);
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

            ShowFloatingText($"Обмен улучшен! (20 руды = {1 + tradeLevel} золота)", Color.green);
            SaveGame();
        }
        else
        {
            ShowFloatingText($"Не хватает золота! Нужно {currentTradeCost}", Color.red);
        }
    }

    public void AddPickaxe()
    {
        // Проверяем, не достигнут ли максимум
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
            SaveGame();
        }
        else
        {
            ShowFloatingText($"Не хватает золота! Нужно {currentPickaxeCost}", Color.red);
        }
    }

    private void UpdateButtonsInteractable()
    {
        strengthButton.interactable = gold >= currentStrengthCost;
        tradeButton.interactable = gold >= currentTradeCost;

        // Кнопка кирки неактивна, если достигнут максимум
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

            // На мобильных устройствах делаем текст крупнее
            if (Application.isMobilePlatform)
            {
                text.fontSize = Mathf.RoundToInt(text.fontSize * 1.3f);
            }
        }
        else
        {
            Debug.LogError("В префабе FloatingText нет компонента TMP_Text!");
        }

        // Увеличиваем время жизни текста (используем переменную)
        Destroy(go, floatingTextLifetime);
    }

    private void UpdateUI()
    {
        if (oreText != null)
            oreText.text = $"{ore} руды";

        if (goldText != null)
            goldText.text = $"{gold} золота";

        if (strengthCostText != null)
            strengthCostText.text = $"{currentStrengthCost}";

        if (tradeCostText != null)
            tradeCostText.text = $"{currentTradeCost}";

        if (pickaxeCostText != null)
            pickaxeCostText.text = $"{currentPickaxeCost}";

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
        // Удаляем старые кирки
        foreach (GameObject pickaxe in activePickaxes)
        {
            if (pickaxe != null)
                Destroy(pickaxe);
        }
        activePickaxes.Clear();

        // Создаём новые кирки по точкам
        for (int i = 0; i < pickaxeCount && i < pickaxePoints.Length; i++)
        {
            if (pickaxePoints[i] != null)
            {
                GameObject newPickaxe = Instantiate(pickaxePrefab, pickaxeContainer);
                newPickaxe.transform.position = pickaxePoints[i].position;
                newPickaxe.transform.rotation = pickaxePoints[i].rotation;
                activePickaxes.Add(newPickaxe);

                // На мобильных устройствах увеличиваем размер кирок
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

        Debug.Log($"Отображение кирок обновлено: {activePickaxes.Count} кирок на экране");
    }

    // Геттеры для RockClick
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
        PlayerPrefs.Save();
        Debug.Log("Игра сохранена!");
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

            // Ограничиваем количество кирок максимумом
            if (pickaxeCount > MAX_PICKAXES)
                pickaxeCount = MAX_PICKAXES;

            Debug.Log($"Игра загружена: Руда={ore}, Золото={gold}, Сила={strengthLevel}, Обмен={tradeLevel}, Кирки={pickaxeCount}");
        }
        else
        {
            Debug.Log("Нет сохранений, начинаем новую игру");
        }
    }

    public void ResetGame()
    {
        ore = 0;
        gold = 15; // Стартовое золото 15
        strengthLevel = 0;
        tradeLevel = 0;
        pickaxeCount = 1;

        UpdateCosts();
        UpdateUI();
        UpdateButtonsInteractable();
        UpdatePickaxesDisplay();
        SaveGame();

        ShowFloatingText("Прогресс сброшен!", Color.red);
        Debug.Log("Прогресс игры сброшен!");
    }

    public void ManualSave()
    {
        SaveGame();
        ShowFloatingText("Игра сохранена!", Color.green);
    }

    public void ManualLoad()
    {
        LoadGame();
        UpdateCosts();
        UpdateUI();
        UpdateButtonsInteractable();
        UpdatePickaxesDisplay();
        ShowFloatingText("Игра загружена!", Color.green);
    }
}