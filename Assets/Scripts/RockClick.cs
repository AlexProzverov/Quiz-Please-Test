using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RockClick : MonoBehaviour
{
    private Button button;

    [Header("Анимация кирки")]
    [SerializeField] private float rotationAngle = 45f;
    [SerializeField] private float animationDuration = 0.1f;
    [SerializeField] private float waveDelay = 0.05f;

    [Header("Анимация камня")]
    [SerializeField] private Transform rockTransform;        // Transform камня
    [SerializeField] private float rockShakeAmount = 3f;     // Сила тряски
    [SerializeField] private float rockShakeDuration = 0.1f; // Длительность тряски
    [SerializeField] private float rockScaleMultiplier = 0.92f; // Множитель сжатия

    [Header("Эффекты при нажатии")]
    [SerializeField] private GameObject hitEffectPrefab;     // Префаб эффекта (Particle System)
    [SerializeField] private Transform hitEffectSpawnPoint;  // Точка спавна эффекта (обычно позиция камня)
    [SerializeField] private float effectLifetime = 1f;      // Время жизни эффекта
    [SerializeField] private bool randomizeEffectRotation = true; // Случайный поворот эффекта

    private Vector3 rockOriginalScale;
    private Vector3 rockOriginalPosition;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnRockClicked);

        // Сохраняем исходные значения камня
        if (rockTransform != null)
        {
            rockOriginalScale = rockTransform.localScale;
            rockOriginalPosition = rockTransform.localPosition;
        }
        else
        {
            Debug.LogWarning("Rock Transform не назначен! Анимация камня не будет работать.");
        }
    }

    private void OnRockClicked()
    {
        GameManager.Instance.HitRock();
        StartCoroutine(AnimateAllPickaxes());
        StartCoroutine(AnimateRock());
        SpawnHitEffect(); // Запускаем эффект
    }

    private IEnumerator AnimateAllPickaxes()
    {
        List<GameObject> pickaxes = GameManager.Instance.GetActivePickaxes();

        for (int i = 0; i < pickaxes.Count; i++)
        {
            if (pickaxes[i] != null)
            {
                StartCoroutine(AnimateSinglePickaxe(pickaxes[i].transform));
                yield return new WaitForSeconds(waveDelay);
            }
        }
    }

    private IEnumerator AnimateSinglePickaxe(Transform trans)
    {
        if (trans == null) yield break;

        Vector3 startAngles = trans.localEulerAngles;
        Vector3 endAngles = new Vector3(startAngles.x, startAngles.y, startAngles.z - rotationAngle);

        // Удар (вращение)
        float timer = 0;
        while (timer < animationDuration)
        {
            float t = timer / animationDuration;
            trans.localEulerAngles = Vector3.Lerp(startAngles, endAngles, t);
            timer += Time.deltaTime;
            yield return null;
        }

        // Возврат
        timer = 0;
        while (timer < animationDuration)
        {
            float t = timer / animationDuration;
            trans.localEulerAngles = Vector3.Lerp(endAngles, startAngles, t);
            timer += Time.deltaTime;
            yield return null;
        }

        trans.localEulerAngles = startAngles;
    }

    // ==================== АНИМАЦИЯ КАМНЯ ====================

    private IEnumerator AnimateRock()
    {
        if (rockTransform == null) yield break;

        // Сохраняем исходные значения
        Vector3 originalScale = rockOriginalScale;
        Vector3 originalPosition = rockOriginalPosition;

        // Целевые значения (сжатый камень)
        Vector3 targetScale = originalScale * rockScaleMultiplier;

        // Фаза 1: Сжатие и тряска
        float elapsed = 0;
        while (elapsed < rockShakeDuration)
        {
            float t = elapsed / rockShakeDuration;
            float easedT = 1 - Mathf.Pow(1 - t, 2); // Ease out

            // Плавное сжатие
            rockTransform.localScale = Vector3.Lerp(originalScale, targetScale, easedT);

            // Тряска (случайные смещения)
            float shakeX = Random.Range(-rockShakeAmount, rockShakeAmount);
            float shakeY = Random.Range(-rockShakeAmount, rockShakeAmount);
            rockTransform.localPosition = originalPosition + new Vector3(shakeX, shakeY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Фиксируем сжатое состояние
        rockTransform.localScale = targetScale;

        // Фаза 2: Возврат в исходное положение
        elapsed = 0;
        while (elapsed < rockShakeDuration)
        {
            float t = elapsed / rockShakeDuration;
            float easedT = Mathf.Pow(t, 2); // Ease in

            // Плавное возвращение размера
            rockTransform.localScale = Vector3.Lerp(targetScale, originalScale, easedT);

            // Плавное возвращение позиции
            float shakeX = Mathf.Lerp(Random.Range(-rockShakeAmount, rockShakeAmount), 0, easedT);
            float shakeY = Mathf.Lerp(Random.Range(-rockShakeAmount, rockShakeAmount), 0, easedT);
            rockTransform.localPosition = originalPosition + new Vector3(shakeX, shakeY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Точно возвращаем исходные значения
        rockTransform.localScale = originalScale;
        rockTransform.localPosition = originalPosition;
    }

    // ==================== ЭФФЕКТ ПРИ НАЖАТИИ ====================

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
        {
            Debug.LogWarning("Hit Effect Prefab не назначен! Эффект не будет работать.");
            return;
        }

        // Определяем точку спавна
        Vector3 spawnPosition = hitEffectSpawnPoint != null ? hitEffectSpawnPoint.position : rockTransform.position;

        // Создаём эффект
        GameObject effect = Instantiate(hitEffectPrefab, spawnPosition, Quaternion.identity);

        // Случайный поворот эффекта (для разнообразия)
        if (randomizeEffectRotation)
        {
            effect.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        }

        // Автоматическое удаление эффекта через заданное время
        Destroy(effect, effectLifetime);

        // Если у эффекта есть ParticleSystem, можно его сразу запустить
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
        else
        {
            // Проверяем дочерние объекты
            ps = effect.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
        }
    }

    // Публичный метод для вызова эффекта из других скриптов (если нужно)
    public void SpawnCustomEffect(GameObject customEffectPrefab, Vector3 position)
    {
        if (customEffectPrefab == null) return;

        GameObject effect = Instantiate(customEffectPrefab, position, Quaternion.identity);
        Destroy(effect, effectLifetime);
    }
}