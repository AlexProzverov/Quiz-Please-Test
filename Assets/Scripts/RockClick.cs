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

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnRockClicked);
    }

    private void OnRockClicked()
    {
        GameManager.Instance.HitRock();
        StartCoroutine(AnimateAllPickaxes());
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
}