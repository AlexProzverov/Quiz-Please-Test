using UnityEngine;
using TMPro;

public class FloatingTextAnimation : MonoBehaviour
{
    private TMP_Text text;
    private float lifetime = 0.8f;
    private float moveSpeed = 50f;

    void Start()
    {
        text = GetComponent<TMP_Text>();
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Движение вверх
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Плавное исчезновение
        if (text != null)
        {
            Color color = text.color;
            color.a -= Time.deltaTime / lifetime;
            text.color = color;
        }
    }
}